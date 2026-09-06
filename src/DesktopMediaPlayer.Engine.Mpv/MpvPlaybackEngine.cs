using System.Collections.Concurrent;
using System.Globalization;
using System.Runtime.InteropServices;
using DesktopMediaPlayer.Contracts;
using DesktopMediaPlayer.Engine.Mpv.Native;
using DesktopMediaPlayer.Platform;

namespace DesktopMediaPlayer.Engine.Mpv;

/// <summary>
/// libmpv engine host. UI thread posts commands; a dedicated engine thread owns the mpv handle.
/// </summary>
public sealed class MpvPlaybackEngine : IPlaybackEngine, INativeRuntimeProbe
{
    private readonly SpikeLogger _logger;
    private readonly ConcurrentQueue<Action> _queue = new();
    private readonly AutoResetEvent _wake = new(false);
    private readonly CancellationTokenSource _cts = new();
    private readonly object _stateGate = new();
    private readonly SynchronizationContext? _sync;

    private Thread? _thread;
    private nint _mpv;
    private nint _wid;
    private PlaybackState _state = PlaybackState.Idle;
    private bool _firstFrameRaised;
    private bool _disposed;
    private string? _pendingOpen;
    private int _volume = 100;
    private DateTime _lastMetricsUtc = DateTime.MinValue;
    private static readonly TimeSpan MetricsInterval = TimeSpan.FromSeconds(2);

    // KI-014 Soft: absolute drop counters + Δrate between samples (≠ perceived stutter).
    private long? _prevFrameDrop;
    private long? _prevDecoderDrop;
    private long? _prevVoDrop;
    private DateTime _prevDropSampleUtc = DateTime.MinValue;

    public MpvPlaybackEngine(SpikeLogger logger, IPlaybackObserver? observer = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        Observer = observer;
        _sync = SynchronizationContext.Current;
        RenderHost = new MpvRenderHost(this);
    }

    public IPlaybackObserver? Observer { get; set; }

    public IRenderHost RenderHost { get; }

    /// <summary>True if libmpv-2.dll can be resolved (does not require initialize).</summary>
    public bool TryProbe(out string? detail)
    {
        if (MpvNative.TryProbeLibrary(out var loaded, out var err))
        {
            detail = loaded;
            return true;
        }

        detail = err;
        return false;
    }

    public void Open(string path) => Post(() =>
    {
        _pendingOpen = path;
        _firstFrameRaised = false;
        ResetDropBaselines();
        SetState(PlaybackState.Opening);
        _logger.Open(path);
        if (_mpv == nint.Zero)
        {
            if (_wid == nint.Zero)
            {
                return;
            }

            if (!CreateAndInitialize())
            {
                return;
            }
        }

        LoadFile(path);
    });

    public void Close() => Post(() =>
    {
        _pendingOpen = null;
        if (_mpv != nint.Zero)
        {
            Check(MpvNative.mpv_command_string(_mpv, "stop"), "stop");
        }

        SetState(PlaybackState.Idle);
    });

    public void Play() => Post(() =>
    {
        if (_mpv == nint.Zero)
        {
            return;
        }

        Check(MpvNative.mpv_set_property_string(_mpv, "pause", "no"), "pause=no");
        SetState(PlaybackState.Playing);
    });

    public void Pause() => Post(() =>
    {
        if (_mpv == nint.Zero)
        {
            return;
        }

        Check(MpvNative.mpv_set_property_string(_mpv, "pause", "yes"), "pause=yes");
        SetState(PlaybackState.Paused);
    });

    public void Stop() => Post(() =>
    {
        if (_mpv == nint.Zero)
        {
            return;
        }

        Check(MpvNative.mpv_command_string(_mpv, "stop"), "stop");
        SetState(PlaybackState.Stopped);
    });

    public void Seek(double seconds) => Post(() =>
    {
        if (_mpv == nint.Zero)
        {
            return;
        }

        var cmd = string.Create(CultureInfo.InvariantCulture, $"seek {seconds} absolute");
        Check(MpvNative.mpv_command_string(_mpv, cmd), "seek");
    });

    public void SetVolume(int volume) => Post(() =>
    {
        _volume = Math.Clamp(volume, 0, 100);
        if (_mpv == nint.Zero)
        {
            return;
        }

        Check(
            MpvNative.mpv_set_property_string(_mpv, "volume", _volume.ToString(CultureInfo.InvariantCulture)),
            "volume");
    });

    public PlaybackState GetState()
    {
        lock (_stateGate)
        {
            return _state;
        }
    }

    internal void AttachWindow(nint hwnd)
    {
        Post(() =>
        {
            _wid = hwnd;
            if (hwnd == nint.Zero)
            {
                DestroyMpv();
                return;
            }

            if (_mpv == nint.Zero)
            {
                if (!CreateAndInitialize())
                {
                    return;
                }

                if (_pendingOpen is not null)
                {
                    LoadFile(_pendingOpen);
                }
            }
            else
            {
                ApplyWid();
            }
        });
    }

    internal void ResizeWindow(int width, int height)
    {
        Post(() =>
        {
            if (_wid == nint.Zero || width <= 0 || height <= 0)
            {
                return;
            }

            const uint swpNoZOrder = 0x0004;
            const uint swpNoMove = 0x0002;
            const uint swpNoActivate = 0x0010;
            NativeMethods.SetWindowPos(_wid, nint.Zero, 0, 0, width, height, swpNoZOrder | swpNoMove | swpNoActivate);
        });
    }


    public void FrameStep(int steps) => Post(() =>
    {
        if (_mpv == nint.Zero || steps == 0)
        {
            return;
        }

        // Positive = forward, negative = backward (mpv frame-back-step / frame-step).
        for (var i = 0; i < Math.Abs(steps); i++)
        {
            var cmd = steps > 0 ? "frame-step" : "frame-back-step";
            Check(MpvNative.mpv_command_string(_mpv, cmd), cmd);
        }
    });

    public void SetMute(bool mute) => Post(() =>
    {
        if (_mpv == nint.Zero)
        {
            return;
        }

        Check(MpvNative.mpv_set_property_string(_mpv, "mute", mute ? "yes" : "no"), "mute");
    });

    public bool GetMute()
    {
        if (_mpv == nint.Zero)
        {
            return false;
        }

        // Best-effort sync read; may be slightly stale vs engine thread.
        var v = MpvNative.GetPropertyAndFree(_mpv, "mute");
        return string.Equals(v, "yes", StringComparison.OrdinalIgnoreCase);
    }

    public double GetPosition()
    {
        if (_mpv == nint.Zero)
        {
            return 0;
        }

        var v = MpvNative.GetPropertyAndFree(_mpv, "time-pos");
        return double.TryParse(v, System.Globalization.NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : 0;
    }

    public double GetDuration()
    {
        if (_mpv == nint.Zero)
        {
            return 0;
        }

        var v = MpvNative.GetPropertyAndFree(_mpv, "duration");
        return double.TryParse(v, System.Globalization.NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : 0;
    }

    public double GetBufferedEndSeconds()
    {
        if (_mpv == nint.Zero)
        {
            return 0;
        }

        static double Parse(string? s) =>
            double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : 0;

        var pos = Parse(MpvNative.GetPropertyAndFree(_mpv, "time-pos"));
        var cache = Parse(MpvNative.GetPropertyAndFree(_mpv, "demuxer-cache-duration"));
        if (cache <= 0)
        {
            cache = Parse(MpvNative.GetPropertyAndFree(_mpv, "cache-duration"));
        }

        // Soft (수석): cache=0 → empty (real property). Do not fake buffer from position alone.
        if (cache <= 0)
        {
            return 0;
        }

        var dur = Parse(MpvNative.GetPropertyAndFree(_mpv, "duration"));
        var end = pos + cache;
        if (dur > 0)
        {
            end = Math.Min(end, dur);
        }

        return end;
    }


    public IReadOnlyList<MediaTrackInfo> ListTracks()
    {
        if (_mpv == nint.Zero)
        {
            return Array.Empty<MediaTrackInfo>();
        }

        var countRaw = MpvNative.GetPropertyAndFree(_mpv, "track-list/count");
        if (!int.TryParse(countRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var count) || count <= 0)
        {
            return Array.Empty<MediaTrackInfo>();
        }

        var list = new List<MediaTrackInfo>(count);
        for (var i = 0; i < count; i++)
        {
            var type = MpvNative.GetPropertyAndFree(_mpv, $"track-list/{i}/type") ?? string.Empty;
            var kind = type.ToLowerInvariant() switch
            {
                "audio" => MediaTrackKind.Audio,
                "sub" or "subtitle" => MediaTrackKind.Subtitle,
                "video" => MediaTrackKind.Video,
                _ => (MediaTrackKind?)null
            };
            if (kind is null)
            {
                continue;
            }

            var idRaw = MpvNative.GetPropertyAndFree(_mpv, $"track-list/{i}/id");
            if (!int.TryParse(idRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
            {
                continue;
            }

            var title = MpvNative.GetPropertyAndFree(_mpv, $"track-list/{i}/title");
            var lang = MpvNative.GetPropertyAndFree(_mpv, $"track-list/{i}/lang");
            var selectedRaw = MpvNative.GetPropertyAndFree(_mpv, $"track-list/{i}/selected");
            var selected = string.Equals(selectedRaw, "yes", StringComparison.OrdinalIgnoreCase)
                           || string.Equals(selectedRaw, "true", StringComparison.OrdinalIgnoreCase);
            list.Add(new MediaTrackInfo(kind.Value, id, title, lang, selected));
        }

        return list;
    }

    public void SelectTrack(MediaTrackKind kind, int id) => Post(() =>
    {
        if (_mpv == nint.Zero)
        {
            return;
        }

        var prop = kind switch
        {
            MediaTrackKind.Audio => "aid",
            MediaTrackKind.Subtitle => "sid",
            MediaTrackKind.Video => "vid",
            _ => null
        };
        if (prop is null)
        {
            return;
        }

        var value = id <= 0 ? "no" : id.ToString(CultureInfo.InvariantCulture);
        Check(MpvNative.mpv_set_property_string(_mpv, prop, value), prop);
    });

    public void LoadExternalSubtitle(string path) => Post(() =>
    {
        if (_mpv == nint.Zero)
        {
            return;
        }

        var escaped = path.Replace("\\", "/", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);
        Check(MpvNative.mpv_command_string(_mpv, $"sub-add \"{escaped}\""), "sub-add");
    });

    public void SetSubtitleOffset(double seconds) => Post(() =>
    {
        if (_mpv == nint.Zero)
        {
            return;
        }

        Check(
            MpvNative.mpv_set_property_string(_mpv, "sub-delay", seconds.ToString(CultureInfo.InvariantCulture)),
            "sub-delay");
    });

    public void Screenshot(string path) => Post(() =>
    {
        if (_mpv == nint.Zero)
        {
            return;
        }

        var escaped = path.Replace("\\", "/", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);
        Check(MpvNative.mpv_command_string(_mpv, $"screenshot-to-file \"{escaped}\""), "screenshot-to-file");
    });


    public MediaInfoBasics GetMediaInfo()
    {
        if (_mpv == nint.Zero)
        {
            return new MediaInfoBasics(null, null, null, null, null, null, null, 0);
        }

        static int? ParseInt(string? s) =>
            int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : null;

        static double ParseDouble(string? s) =>
            double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : 0;

        var path = MpvNative.GetPropertyAndFree(_mpv, "path");
        var title = MpvNative.GetPropertyAndFree(_mpv, "media-title");
        var format = MpvNative.GetPropertyAndFree(_mpv, "file-format");
        var vcodec = MpvNative.GetPropertyAndFree(_mpv, "video-codec");
        var acodec = MpvNative.GetPropertyAndFree(_mpv, "audio-codec");
        var w = ParseInt(MpvNative.GetPropertyAndFree(_mpv, "width"));
        var h = ParseInt(MpvNative.GetPropertyAndFree(_mpv, "height"));
        var dur = ParseDouble(MpvNative.GetPropertyAndFree(_mpv, "duration"));
        return new MediaInfoBasics(path, title, format, vcodec, acodec, w, h, dur);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _cts.Cancel();
        _wake.Set();
        if (_thread is { IsAlive: true } && Thread.CurrentThread != _thread)
        {
            _thread.Join(TimeSpan.FromSeconds(3));
        }

        DestroyMpv();
        _wake.Dispose();
        _cts.Dispose();
    }

    private void EnsureThread()
    {
        if (_thread is { IsAlive: true })
        {
            return;
        }

        _thread = new Thread(EngineLoop)
        {
            IsBackground = true,
            Name = "mpv-engine"
        };
        _thread.Start();
    }

    private void Post(Action action)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        EnsureThread();
        _queue.Enqueue(action);
        _wake.Set();
    }

    private void EngineLoop()
    {
        try
        {
            while (!_cts.IsCancellationRequested)
            {
                while (_queue.TryDequeue(out var work))
                {
                    try
                    {
                        work();
                    }
                    catch (Exception ex)
                    {
                        RaiseError("engine.command", ex.Message, recoverable: true);
                    }
                }

                PumpEvents();
                MaybeLogPeriodicMetrics();
                _wake.WaitOne(50);
            }
        }
        catch (Exception ex)
        {
            RaiseError("engine.loop", ex.Message, recoverable: false);
        }
        finally
        {
            DestroyMpv();
        }
    }

    private bool CreateAndInitialize()
    {
        if (!MpvNative.TryProbeLibrary(out var loaded, out var probeError))
        {
            RaiseError("native.missing", probeError ?? "libmpv-2.dll missing", recoverable: false);
            SetState(PlaybackState.Error);
            return false;
        }

        _logger.Log("info", "native_loaded", loaded ?? "");

        try
        {
            _mpv = MpvNative.mpv_create();
        }
        catch (DllNotFoundException ex)
        {
            RaiseError("native.missing", ex.Message, recoverable: false);
            SetState(PlaybackState.Error);
            return false;
        }
        catch (Exception ex)
        {
            RaiseError("mpv.create", ex.Message, recoverable: false);
            SetState(PlaybackState.Error);
            return false;
        }

        if (_mpv == nint.Zero)
        {
            RaiseError("mpv.create", "mpv_create returned null", recoverable: false);
            SetState(PlaybackState.Error);
            return false;
        }

        ApplyWid();
        MpvNative.mpv_set_option_string(_mpv, "hwdec", "d3d11va,d3d11va-copy,no");
        MpvNative.mpv_set_option_string(_mpv, "vo", "gpu-next,gpu");
        MpvNative.mpv_set_option_string(_mpv, "gpu-context", HardwareAccelPolicy.GpuContext);
        MpvNative.mpv_set_option_string(_mpv, "keep-open", "yes");
        MpvNative.mpv_set_option_string(_mpv, "osc", "no");
        MpvNative.mpv_set_option_string(_mpv, "input-default-bindings", "no");
        MpvNative.mpv_set_option_string(_mpv, "input-vo-keyboard", "no");
        MpvNative.mpv_set_option_string(_mpv, "terminal", "no");
        MpvNative.mpv_request_log_messages(_mpv, "info");

        var init = MpvNative.mpv_initialize(_mpv);
        if (init < 0)
        {
            RaiseError("mpv.init", MpvNative.GetErrorString(init), recoverable: false);
            DestroyMpv();
            SetState(PlaybackState.Error);
            return false;
        }

        MpvNative.mpv_observe_property(_mpv, 1, "pause", MpvFormat.Flag);
        MpvNative.mpv_observe_property(_mpv, 2, "hwdec-current", MpvFormat.String);
        MpvNative.mpv_set_property_string(_mpv, "volume", _volume.ToString(CultureInfo.InvariantCulture));
        LogRenderPath("after_init");
        return true;
    }

    private void ApplyWid()
    {
        if (_mpv == nint.Zero || _wid == nint.Zero)
        {
            return;
        }

        var wid = _wid.ToInt64().ToString(CultureInfo.InvariantCulture);
        MpvNative.mpv_set_option_string(_mpv, "wid", wid);
        MpvNative.mpv_set_property_string(_mpv, "wid", wid);
    }

    private void LoadFile(string path)
    {
        if (_mpv == nint.Zero)
        {
            return;
        }

        var escaped = path.Replace("\\", "/", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);
        var cmd = $"loadfile \"{escaped}\" replace";
        var rc = MpvNative.mpv_command_string(_mpv, cmd);
        if (rc < 0)
        {
            RaiseError("open.failed", MpvNative.GetErrorString(rc), recoverable: true);
            SetState(PlaybackState.Error);
        }
    }

    private void PumpEvents()
    {
        if (_mpv == nint.Zero)
        {
            return;
        }

        for (var i = 0; i < 32; i++)
        {
            var ptr = MpvNative.mpv_wait_event(_mpv, 0);
            if (ptr == nint.Zero)
            {
                break;
            }

            var evt = Marshal.PtrToStructure<MpvEvent>(ptr);
            if (evt.event_id == MpvEventIds.None)
            {
                break;
            }

            HandleEvent(evt);
        }
    }

    private void HandleEvent(MpvEvent evt)
    {
        switch (evt.event_id)
        {
            case MpvEventIds.Shutdown:
                DestroyMpv();
                SetState(PlaybackState.Idle);
                break;
            case MpvEventIds.FileLoaded:
                SetState(PlaybackState.Playing);
                break;
            case MpvEventIds.EndFile:
                SetState(PlaybackState.Ended);
                break;
            case MpvEventIds.VideoReconfig:
            case MpvEventIds.PlaybackRestart:
                MaybeFirstFrame();
                break;
            case MpvEventIds.LogMessage:
                break;
            case MpvEventIds.PropertyChange:
                HandlePropertyChange(evt.data);
                break;
        }
    }

    private void HandlePropertyChange(nint data)
    {
        if (data == nint.Zero)
        {
            return;
        }

        var prop = Marshal.PtrToStructure<MpvEventProperty>(data);
        var name = MpvNative.PtrToUtf8(prop.name);
        if (string.Equals(name, "hwdec-current", StringComparison.Ordinal))
        {
            ReportHwdec();
        }
        else if (string.Equals(name, "pause", StringComparison.Ordinal) && _state is PlaybackState.Playing or PlaybackState.Paused)
        {
            var paused = MpvNative.GetPropertyAndFree(_mpv, "pause");
            if (string.Equals(paused, "yes", StringComparison.OrdinalIgnoreCase))
            {
                SetState(PlaybackState.Paused);
            }
            else
            {
                SetState(PlaybackState.Playing);
            }
        }
    }

    private void MaybeFirstFrame()
    {
        if (_firstFrameRaised)
        {
            return;
        }

        _firstFrameRaised = true;
        _logger.FirstFrame();
        Raise(o => o.OnFirstFrame());
        LogRenderPath("first_frame");
        ReportHwdec();
    }

    /// <summary>Logs vo / gpu-context / hwdec-current / frame drops for Windows spike evidence.</summary>
    private void LogRenderPath(string phase) => LogPlaybackMetrics(phase);

    private void MaybeLogPeriodicMetrics()
    {
        PlaybackState state;
        lock (_stateGate)
        {
            state = _state;
        }

        if (state != PlaybackState.Playing || _mpv == nint.Zero)
        {
            return;
        }

        var now = DateTime.UtcNow;
        if (now - _lastMetricsUtc < MetricsInterval)
        {
            return;
        }

        LogPlaybackMetrics("periodic");
    }

    private void LogPlaybackMetrics(string phase)
    {
        if (_mpv == nint.Zero)
        {
            return;
        }

        var now = DateTime.UtcNow;
        _lastMetricsUtc = now;

        var vo = MpvNative.GetPropertyAndFree(_mpv, "current-vo")
                 ?? MpvNative.GetPropertyAndFree(_mpv, "vo")
                 ?? "(unknown)";
        var gpuContext = MpvNative.GetPropertyAndFree(_mpv, "gpu-context") ?? "(unknown)";
        var hwdec = MpvNative.GetPropertyAndFree(_mpv, "hwdec-current") ?? "(unknown)";
        var frameDropRaw = MpvNative.GetPropertyAndFree(_mpv, "frame-drop-count")
                           ?? MpvNative.GetPropertyAndFree(_mpv, "drop-frame-count");
        var decoderDropRaw = MpvNative.GetPropertyAndFree(_mpv, "decoder-frame-drop-count");
        var voDropRaw = MpvNative.GetPropertyAndFree(_mpv, "vo-delayed-frame-count");

        var frameDrop = frameDropRaw ?? "(n/a)";
        var decoderDrop = decoderDropRaw ?? "(n/a)";
        var voDrop = voDropRaw ?? "(n/a)";
        var deltaPart = FormatDropDeltas(now, frameDropRaw, decoderDropRaw, voDropRaw);

        _logger.Log(
            "info",
            "render_path",
            $"phase={phase} vo={vo} gpu-context={gpuContext} hwdec-current={hwdec} "
            + $"frame-drop={frameDrop} decoder-drop={decoderDrop} vo-drop={voDrop}{deltaPart}");
    }

    private void ResetDropBaselines()
    {
        _prevFrameDrop = null;
        _prevDecoderDrop = null;
        _prevVoDrop = null;
        _prevDropSampleUtc = DateTime.MinValue;
    }

    /// <summary>
    /// KI-014 Soft: log Δ and /s rate between samples. Cumulative frame-drop-count is not stutter.
    /// </summary>
    private string FormatDropDeltas(DateTime now, string? frameRaw, string? decoderRaw, string? voRaw)
    {
        static bool TryParseLong(string? s, out long v) =>
            long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out v);

        var hasPrev = _prevDropSampleUtc != DateTime.MinValue
                      && _prevFrameDrop.HasValue;
        var dt = hasPrev ? (now - _prevDropSampleUtc).TotalSeconds : 0;

        string FormatOne(string name, string? raw, long? prev)
        {
            if (!TryParseLong(raw, out var cur) || !hasPrev || prev is null || dt <= 0)
            {
                return $" {name}-delta=(n/a) {name}-rate=(n/a)";
            }

            var d = cur - prev.Value;
            var rate = d / dt;
            return $" {name}-delta={d.ToString(CultureInfo.InvariantCulture)}"
                   + $" {name}-rate={rate.ToString("0.###", CultureInfo.InvariantCulture)}/s";
        }

        var part = FormatOne("frame-drop", frameRaw, _prevFrameDrop)
                   + FormatOne("decoder-drop", decoderRaw, _prevDecoderDrop)
                   + FormatOne("vo-drop", voRaw, _prevVoDrop);

        if (TryParseLong(frameRaw, out var fd))
        {
            _prevFrameDrop = fd;
        }

        if (TryParseLong(decoderRaw, out var dd))
        {
            _prevDecoderDrop = dd;
        }

        if (TryParseLong(voRaw, out var vd))
        {
            _prevVoDrop = vd;
        }

        _prevDropSampleUtc = now;
        return part;
    }

    private void ReportHwdec()
    {
        if (_mpv == nint.Zero)
        {
            return;
        }

        var current = MpvNative.GetPropertyAndFree(_mpv, "hwdec-current") ?? "unknown";
        var active = !string.Equals(current, "no", StringComparison.OrdinalIgnoreCase)
                     && !string.Equals(current, "none", StringComparison.OrdinalIgnoreCase)
                     && !string.IsNullOrWhiteSpace(current)
                     && !string.Equals(current, "unknown", StringComparison.OrdinalIgnoreCase);
        var reason = active
            ? $"hwdec-current={current}"
            : $"hwdec-current={current} (software/inactive)";
        Raise(o => o.OnHardwareAccelChanged(active, reason));
        if (active)
        {
            _logger.HwdecActive(reason);
        }
        else
        {
            _logger.HwdecFallback(reason);
        }
    }

    private void SetState(PlaybackState state)
    {
        lock (_stateGate)
        {
            if (_state == state)
            {
                return;
            }

            _state = state;
        }

        _logger.State(state.ToString());
        Raise(o => o.OnStateChanged(state));
        if (_mpv != nint.Zero)
        {
            LogPlaybackMetrics($"state:{state}");
        }
    }

    private void RaiseError(string code, string message, bool recoverable)
    {
        _logger.Error(code, message, recoverable);
        Raise(o => o.OnError(code, message, recoverable));
    }

    private void Raise(Action<IPlaybackObserver> call)
    {
        var observer = Observer;
        if (observer is null)
        {
            return;
        }

        void Invoke()
        {
            try
            {
                call(observer);
            }
            catch
            {
                // Observer must not kill the engine thread.
            }
        }

        if (_sync is not null)
        {
            _sync.Post(_ => Invoke(), null);
        }
        else
        {
            Invoke();
        }
    }

    private void Check(int rc, string what)
    {
        if (rc < 0)
        {
            RaiseError($"mpv.{what}", MpvNative.GetErrorString(rc), recoverable: true);
        }
    }

    private void DestroyMpv()
    {
        if (_mpv == nint.Zero)
        {
            return;
        }

        try
        {
            MpvNative.mpv_terminate_destroy(_mpv);
        }
        catch
        {
            // Best-effort shutdown.
        }

        _mpv = nint.Zero;
    }
}

internal static class NativeMethods
{
    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool SetWindowPos(nint hwnd, nint hwndInsertAfter, int x, int y, int cx, int cy, uint flags);
}
