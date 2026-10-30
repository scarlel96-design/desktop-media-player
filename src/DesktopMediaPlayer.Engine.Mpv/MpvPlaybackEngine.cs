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
    // S104 C2: serializes UI-thread handle reads with DestroyMpv handle release.
    private readonly object _handleGate = new();
    private readonly SynchronizationContext? _sync;

    private Thread? _thread;
    private nint _mpv;
    private nint _wid;
    private PlaybackState _state = PlaybackState.Idle;
    private bool _firstFrameRaised;
    private long _seekRequestTs; // RV-01 Soft: Stopwatch ticks of pending seek request; 0 = none.
    private int _seekCount; // K1: seek commands issued within the open _seekRequestTs measurement.
    private volatile bool _disposed;
    private int _disposeFlag; // S105: atomic Dispose entry guard
    private string? _pendingOpen;
    private int _volume = 100;
    private string _cscale = CscalePolicy.Lanczos;
    private readonly GpuPreference? _gpuPreference; // A5-a: null = invalid stored value (behaves as Auto)
    private readonly IGpuAdapterProbe? _gpuProbe;
    private bool _gpuAdapterApplied;
    private bool _gpuRetried;
    private bool _gpuDiagOverride; // only ever set under DMP_DIAG
    private long _loadGeneration; // A5-a: bumped by Open/Stop/Close/Dispose/retry; stale vo-configured timeouts are ignored
    private long _voDeadlineGeneration;
    private DateTime _voDeadlineUtc = DateTime.MaxValue;
    private DateTime _lastMetricsUtc = DateTime.MinValue;
    private static readonly TimeSpan MetricsInterval = TimeSpan.FromSeconds(2);
    private const int SeekSlowMs = 500;

    // KI-014 Soft: absolute drop counters + Δrate between samples (≠ perceived stutter).
    private long? _prevFrameDrop;
    private long? _prevDecoderDrop;
    private long? _prevVoDrop;
    private DateTime _prevDropSampleUtc = DateTime.MinValue;
#if DMP_DIAG
    // DIAG only: keys injected from diag-mpv.txt, and the 10 s summary window.
    private readonly List<string> _diagAppliedKeys = new();
    private static readonly TimeSpan DiagSummaryInterval = TimeSpan.FromSeconds(10);
    private DateTime _diagSummaryUtc = DateTime.MinValue;
    private long? _diagSummaryDrop;
    private TimeSpan _diagSummaryCpu;
    private string? _diagPolicyAdapter; // gpu-policy-adapter-override (policy only, not an mpv option)
#endif

    public MpvPlaybackEngine(SpikeLogger logger, IPlaybackObserver? observer = null,
        GpuPreference? gpuPreference = GpuPreference.Auto, IGpuAdapterProbe? gpuProbe = null, string? cacheDir = null)
    {
        _cacheDir = cacheDir;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _gpuPreference = gpuPreference;
        _gpuProbe = gpuProbe;
        Observer = observer;
        _sync = SynchronizationContext.Current;
        RenderHost = new MpvRenderHost(this);
    }

    private readonly string? _cacheDir;

    /// <summary>DATADIR: mpv cache options rooted at the injected cache dir (none when null).</summary>
    public static IReadOnlyList<KeyValuePair<string, string>> CacheOptions(string? cacheDir) =>
        string.IsNullOrEmpty(cacheDir)
            ? Array.Empty<KeyValuePair<string, string>>()
            : new[]
            {
                new KeyValuePair<string, string>("gpu-shader-cache-dir", Path.Combine(cacheDir, "shaders")),
                new KeyValuePair<string, string>("icc-cache-dir", Path.Combine(cacheDir, "icc")),
            };

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
        CancelVoTimer();
        _seekRequestTs = 0;
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
        CancelVoTimer();
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
        _seekRequestTs = 0;
        CancelVoTimer();
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
        var rc = MpvNative.mpv_command_string(_mpv, cmd);
        Check(rc, "seek");
        if (rc >= 0 && (_state is PlaybackState.Playing or PlaybackState.Paused) && _seekRequestTs == 0)
        {
            _seekRequestTs = System.Diagnostics.Stopwatch.GetTimestamp();
            _seekCount = 0;
        }

        _seekCount += rc >= 0 && _seekRequestTs != 0 ? 1 : 0;
    });

    public void SeekRelative(double deltaSeconds, SeekPrecision precision) => Post(() =>
    {
        if (_mpv == nint.Zero)
        {
            return;
        }

        var rc = MpvNative.mpv_command_string(_mpv, string.Create(CultureInfo.InvariantCulture, $"seek {deltaSeconds} {(precision == SeekPrecision.Keyframe ? "relative+keyframes" : "relative+exact")}"));
        Check(rc, "seek");
        if (rc >= 0 && (_state is PlaybackState.Playing or PlaybackState.Paused) && _seekRequestTs == 0)
        {
            _seekRequestTs = System.Diagnostics.Stopwatch.GetTimestamp();
            _seekCount = 0;
        }

        _seekCount += rc >= 0 && _seekRequestTs != 0 ? 1 : 0;
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

    public bool GetMute() { lock (_handleGate) { return GetMuteCore(); } }

    private bool GetMuteCore()
    {
        if (_mpv == nint.Zero)
        {
            return false;
        }

        // Best-effort sync read; may be slightly stale vs engine thread.
        var v = MpvNative.GetPropertyAndFree(_mpv, "mute");
        return string.Equals(v, "yes", StringComparison.OrdinalIgnoreCase);
    }

    public double GetPosition() { lock (_handleGate) { return GetPositionCore(); } }

    private double GetPositionCore()
    {
        if (_mpv == nint.Zero)
        {
            return 0;
        }

        var v = MpvNative.GetPropertyAndFree(_mpv, "time-pos");
        return double.TryParse(v, System.Globalization.NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : 0;
    }

    public double GetDuration() { lock (_handleGate) { return GetDurationCore(); } }

    private double GetDurationCore()
    {
        if (_mpv == nint.Zero)
        {
            return 0;
        }

        var v = MpvNative.GetPropertyAndFree(_mpv, "duration");
        return double.TryParse(v, System.Globalization.NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : 0;
    }

    public double GetBufferedEndSeconds() { lock (_handleGate) { return GetBufferedEndSecondsCore(); } }

    private double GetBufferedEndSecondsCore()
    {
        if (_mpv == nint.Zero)
        {
            return 0;
        }

        // S98 KI-030: local file (demuxer-via-network=no) has no meaningful network buffer.
        // Query failure (null/unknown) keeps existing behavior; never assume local.
        var viaNetwork = MpvNative.GetPropertyAndFree(_mpv, "demuxer-via-network");
        if (string.Equals(viaNetwork, "no", StringComparison.OrdinalIgnoreCase))
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


    public IReadOnlyList<MediaTrackInfo> ListTracks() { lock (_handleGate) { return ListTracksCore(); } }

    private IReadOnlyList<MediaTrackInfo> ListTracksCore()
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


    public MediaInfoBasics GetMediaInfo() { lock (_handleGate) { return GetMediaInfoCore(); } }

    private MediaInfoBasics GetMediaInfoCore()
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
        if (Interlocked.Exchange(ref _disposeFlag, 1) != 0)
        {
            return;
        }

        _disposed = true;
        Interlocked.Increment(ref _loadGeneration);
        _cts.Cancel();
        _wake.Set();
        if (_thread is { IsAlive: true } && Thread.CurrentThread != _thread)
        {
            if (!_thread.Join(TimeSpan.FromSeconds(3)))
            {
                // S102: event thread still alive; skip native destroy and handle disposal to avoid use-after-free.
                _logger.Log("warn", "dispose_join_timeout", "event thread did not exit within 3s; skipped DestroyMpv");
                return;
            }
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
        try
        {
            _wake.Set();
        }
        catch (ObjectDisposedException)
        {
            // S106: Dispose raced after ThrowIf; engine is shutting down, drop the wake.
        }
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
                CheckVoConfiguredTimeout();
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
            _cscale = CscalePolicy.Lanczos;
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
        // S100 frame-drop: cheaper downscale filter for oversized video (timing/queue unchanged).
        MpvNative.mpv_set_option_string(_mpv, "dscale", "bilinear");
        MpvNative.mpv_set_option_string(_mpv, "gpu-context", HardwareAccelPolicy.GpuContext);
        MpvNative.mpv_set_option_string(_mpv, "keep-open", "yes");
        MpvNative.mpv_set_option_string(_mpv, "osc", "no");
        MpvNative.mpv_set_option_string(_mpv, "input-default-bindings", "no");
        MpvNative.mpv_set_option_string(_mpv, "input-vo-keyboard", "no");
        MpvNative.mpv_set_option_string(_mpv, "terminal", "no");
        MpvNative.mpv_request_log_messages(_mpv, "info");
        foreach (var opt in CacheOptions(_cacheDir))
        {
            // mpv creates these dirs on first write; the app does not pre-create them.
            MpvNative.mpv_set_option_string(_mpv, opt.Key, opt.Value);
        }
#if DMP_DIAG
        var diag = ApplyDiagOptions(); // after the hard-coded options so injected values win
        _gpuDiagOverride = diag.Accepted.Any(p => p.Key == "d3d11-adapter");
        _diagPolicyAdapter = diag.PolicyAdapterOverride;
#endif
        ApplyGpuPolicy();

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
        MpvNative.mpv_observe_property(_mpv, 3, "eof-reached", MpvFormat.Flag);
        MpvNative.mpv_observe_property(_mpv, 4, "osd-dimensions", MpvFormat.None);
        MpvNative.mpv_set_property_string(_mpv, "volume", _volume.ToString(CultureInfo.InvariantCulture));
#if DMP_DIAG
        LogDiagOptions(diag);
#endif
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
                ArmVoConfiguredTimeout();
                break;
            case MpvEventIds.EndFile:
                // S103: map END_FILE by reason; error -> Error, eof/unknown -> Ended, stop/quit/redirect keep state.
                var end = LogEndFile(evt.data);
                if (end is { reason: 4 } failed)
                {
                    if (TryGpuFallback(GpuFallbackPolicy.StageForEndFileError(failed.error)))
                    {
                        break;
                    }

                    RaiseError("playback.end_error", MpvNative.GetErrorString(failed.error), recoverable: true);
                    SetState(PlaybackState.Error);
                }
                else if (end is null || end.Value.reason is not (2 or 3 or 5))
                {
                    SetState(PlaybackState.Ended);
                }
                break;
            case MpvEventIds.VideoReconfig:
                LogUnhandledEvent(evt.event_id);
                ApplyCscalePolicy(logDecision: true);
                break;
            case MpvEventIds.PlaybackRestart:
                LogSeekLatency();
                MaybeFirstFrame();
                break;
            case MpvEventIds.LogMessage:
                break;
            case MpvEventIds.PropertyChange:
                HandlePropertyChange(evt.data);
                break;
            default:
                LogUnhandledEvent(evt.event_id);
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
            ApplyCscalePolicy(); // hw-pixelformat may be filled late, after VideoReconfig
        }
        else if (string.Equals(name, "osd-dimensions", StringComparison.Ordinal))
        {
            ApplyCscalePolicy();
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
        else if (string.Equals(name, "eof-reached", StringComparison.Ordinal)
                 && string.Equals(MpvNative.GetPropertyAndFree(_mpv, "eof-reached"), "yes", StringComparison.OrdinalIgnoreCase))
        {
            _logger.Log("info", "eof_reached", $"pos={MpvNative.GetPropertyAndFree(_mpv, "time-pos") ?? "(n/a)"} state={_state}");
        }
    }

    // A5-b: the only place cscale is set. Re-evaluated on VideoReconfig, window resize (osd-dimensions) and hwdec-current change.
    private void ApplyCscalePolicy(bool logDecision = false)
    {
        int P(string n) => int.TryParse(MpvNative.GetPropertyAndFree(_mpv, n), out var v) ? v : 0;
        var dispW = P("osd-dimensions/w") - P("osd-dimensions/ml") - P("osd-dimensions/mr");
        var dispH = P("osd-dimensions/h") - P("osd-dimensions/mt") - P("osd-dimensions/mb");
        int srcW = P("video-params/w"), srcH = P("video-params/h");
        var swFmt = MpvNative.GetPropertyAndFree(_mpv, "video-params/pixelformat");
        var hwFmt = MpvNative.GetPropertyAndFree(_mpv, "video-params/hw-pixelformat");
        var shift = CscalePolicy.ChromaShift(CscalePolicy.EffectivePixelFormat(hwFmt, swFmt));
        var next = CscalePolicy.Decide(dispW, dispH, srcW, srcH, shift);
        var chroma = $"{CscalePolicy.ChromaSize(srcW, shift)}x{CscalePolicy.ChromaSize(srcH, shift)}";
#if DMP_DIAG
        if (DiagMpvOptions.OverridesCscale(_diagAppliedKeys))
        {
            if (logDecision)
            {
                _logger.Log("info", "cscale_decision", $"decision=diag_override policy={next} cscale={MpvNative.GetPropertyAndFree(_mpv, "cscale") ?? "(n/a)"} override=1");
            }

            return;
        }
#endif
        if (logDecision)
        {
            string G(string n) => MpvNative.GetPropertyAndFree(_mpv, n) ?? "(n/a)";
            _logger.Log("info", "cscale_decision", $"decision={next} cur={_cscale} disp={dispW}x{dispH} chroma={chroma} pixelformat={swFmt ?? "(n/a)"} "
                + $"hw-pixelformat={hwFmt ?? "(n/a)"} scale={G("scale")} cscale={G("cscale")} dscale={G("dscale")} hwdec-current={G("hwdec-current")}");
        }

        if (next == _cscale || MpvNative.mpv_set_property_string(_mpv, "cscale", next) < 0)
        {
            return;
        }

        _logger.Log("info", "cscale_policy", $"from={_cscale} to={next} disp={dispW}x{dispH} chroma={chroma}");
        _cscale = next;
    }

    // A5-a: the only place d3d11-adapter is set by policy (pre-init). A DIAG d3d11-adapter disables the policy.
    private void ApplyGpuPolicy()
    {
        var pref = _gpuPreference?.ToString() ?? "invalid";
#if DMP_DIAG
        if (_gpuDiagOverride)
        {
            _logger.Log("info", "gpu_policy", $"pref={pref} adapter=none reason=diag_override");
            return;
        }
#endif

        var probe = _gpuPreference == GpuPreference.HighPerformance ? _gpuProbe?.Probe() ?? GpuAdapterList.ProbeFailed : GpuAdapterList.ProbeFailed;
        var decision = GpuAdapterPolicy.Decide(_gpuPreference, probe.HighPerformanceOrder, probe.DefaultAdapter, probe.Failed);
#if DMP_DIAG
        // HighPerformance only (the only pref ShouldRetry accepts): adapter replaced, fallback/retry path unchanged.
        if (_diagPolicyAdapter is not null && _gpuPreference == GpuPreference.HighPerformance)
        {
            decision = new GpuAdapterDecision(_diagPolicyAdapter, "diag_policy_override");
        }
#endif
        if (decision.Adapter is not null)
        {
            _gpuAdapterApplied = MpvNative.mpv_set_option_string(_mpv, "d3d11-adapter", decision.Adapter) >= 0;
        }

        _logger.Log("info", "gpu_policy", $"pref={pref} adapter={decision.Adapter ?? "none"} reason={decision.Reason}");
    }

    private void CancelVoTimer()
    {
        Interlocked.Increment(ref _loadGeneration);
        _voDeadlineUtc = DateTime.MaxValue;
    }

    private void ArmVoConfiguredTimeout()
    {
        if (!_gpuAdapterApplied || _mpv == nint.Zero)
        {
            return;
        }

        var vid = MpvNative.GetPropertyAndFree(_mpv, "vid");
        if (string.IsNullOrEmpty(vid) || string.Equals(vid, "no", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _voDeadlineGeneration = Interlocked.Read(ref _loadGeneration);
        _voDeadlineUtc = DateTime.UtcNow + GpuFallbackPolicy.VoConfiguredTimeout;
    }

    private void CheckVoConfiguredTimeout()
    {
        if (_voDeadlineUtc == DateTime.MaxValue || DateTime.UtcNow < _voDeadlineUtc)
        {
            return;
        }

        _voDeadlineUtc = DateTime.MaxValue;
        if (_mpv == nint.Zero || !GpuFallbackPolicy.IsTimeoutCurrent(_voDeadlineGeneration, Interlocked.Read(ref _loadGeneration))
            || string.Equals(MpvNative.GetPropertyAndFree(_mpv, "vo-configured"), "yes", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        // Diagnostic only: a timeout never retries (only end_file error -15 does). Deadline is cleared above, so this logs once.
        _logger.Log("warn", "gpu_policy", "vo_timeout=1 retry=0");
    }

    private bool TryGpuFallback(GpuFailureStage stage)
    {
        if (!_gpuAdapterApplied || _pendingOpen is null || _mpv == nint.Zero
            || !GpuFallbackPolicy.ShouldRetry(_gpuPreference, stage, _gpuRetried, _firstFrameRaised, _gpuDiagOverride))
        {
            return false;
        }

        _gpuRetried = true;
        _gpuAdapterApplied = false;
        _logger.Log("warn", "gpu_policy", "fallback=1 reason=vo_init_failed");
        MpvNative.mpv_set_property_string(_mpv, "d3d11-adapter", "");
        CancelVoTimer();
        SetState(PlaybackState.Opening);
        LoadFile(_pendingOpen);
        return true;
    }

    private void MaybeFirstFrame()
    {
        if (_firstFrameRaised)
        {
            return;
        }

        _firstFrameRaised = true;
#if DMP_DIAG
        // Summary window baseline restarts at first frame, the same signal that starts the seek-script runner.
        _diagSummaryUtc = DateTime.MinValue;
#endif
        _logger.FirstFrame();
        Raise(o => o.OnFirstFrame());
        LogRenderPath("first_frame");
        ReportHwdec();
    }

    private void LogUnhandledEvent(int id)
    {
        var name = id switch
        {
            6 => "start_file",
            11 => "idle",
            17 => "video_reconfig",
            18 => "audio_reconfig",
            20 => "seek",
            _ => "other",
        };
        _logger.Log("info", "mpv_event", $"id={id} name={name} state={_state}");
    }

    private MpvEventEndFile? LogEndFile(nint data)
    {
        if (data == nint.Zero)
        {
            return null;
        }
        var end = Marshal.PtrToStructure<MpvEventEndFile>(data);
        var name = end.reason switch
        {
            0 => "eof",
            2 => "stop",
            3 => "quit",
            4 => "error",
            5 => "redirect",
            _ => "unknown",
        };
        _logger.Log(end.reason == 4 ? "warn" : "info", "end_file", $"reason={name}({end.reason}) error={end.error} state={_state}");
        return end;
    }

    private void LogSeekLatency()
    {
        if (_seekRequestTs == 0)
        {
            return;
        }

        var ms = (System.Diagnostics.Stopwatch.GetTimestamp() - _seekRequestTs) * 1000L / System.Diagnostics.Stopwatch.Frequency;
        _seekRequestTs = 0;
        var slow = ms >= SeekSlowMs;
        var hwdec = MpvNative.GetPropertyAndFree(_mpv, "hwdec-current") ?? "(unknown)";
        _logger.Log(slow ? "warn" : "info", "seek_latency", $"ms={ms} slow={slow} seeks={_seekCount} hwdec-current={hwdec}");
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
#if DMP_DIAG
        MaybeLogDiagSummary(now);
#endif
    }
#if DMP_DIAG

    private DiagMpvOptions.Result ApplyDiagOptions()
    {
        _diagAppliedKeys.Clear();
        var (path, source) = DiagMpvOptions.ResolveFile(AppDataPaths.Current.Root, AppContext.BaseDirectory, File.Exists);
        _logger.Log("info", "diag_opts", $"source={source} path={PathMask.Mask(path)}");
        string[] lines;
        try
        {
            lines = File.Exists(path) ? File.ReadAllLines(path) : Array.Empty<string>();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.Log("warn", "diag_opts", $"read_error={ex.GetType().Name}");
            lines = Array.Empty<string>();
        }

        var result = DiagMpvOptions.Parse(lines);
        foreach (var (key, value) in result.Accepted)
        {
            if (MpvNative.mpv_set_option_string(_mpv, key, value) >= 0)
            {
                _diagAppliedKeys.Add(key);
            }
        }

        return result;
    }

    private void LogDiagOptions(DiagMpvOptions.Result result)
    {
        var parts = new List<string>();
        foreach (var (key, value) in result.Accepted)
        {
            var applied = _diagAppliedKeys.Contains(key);
            parts.Add($"{key}=accepted value={value} set={(applied ? "ok" : "failed")} readback={MpvNative.GetPropertyAndFree(_mpv, key) ?? "(n/a)"}");
        }

        foreach (var (key, reason) in result.Rejected)
        {
            parts.Add($"{key}=rejected reason={reason}");
        }

        _logger.Log("info", "diag_opts", parts.Count == 0 ? "none" : string.Join(" | ", parts));
    }

    private void MaybeLogDiagSummary(DateTime now)
    {
        var wall = now - _diagSummaryUtc;
        if (_diagSummaryUtc != DateTime.MinValue && wall < DiagSummaryInterval)
        {
            return;
        }

        TimeSpan cpu;
        using (var proc = System.Diagnostics.Process.GetCurrentProcess())
        {
            cpu = proc.TotalProcessorTime;
        }

        long? drop = long.TryParse(MpvNative.GetPropertyAndFree(_mpv, "frame-drop-count"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var d) ? d : null;
        if (_diagSummaryUtc == DateTime.MinValue)
        {
            (_diagSummaryUtc, _diagSummaryDrop, _diagSummaryCpu) = (now, drop, cpu);
            return;
        }

        double? dropRate = drop is long cur && _diagSummaryDrop is long prev && cur >= prev ? (cur - prev) / wall.TotalSeconds : null;
        var voPasses = MpvNative.GetPropertyAndFree(_mpv, "vo-passes");
        var freshMs = DiagMpvOptions.SumFreshAvgMs(voPasses);
        var cpuPct = DiagMpvOptions.CpuPercent(cpu - _diagSummaryCpu, wall, Environment.ProcessorCount);
        // vo-drop-rate counts frame-drop-count (VO drops only; decoder drops are not included).
        // vo-passes-fmt=json|other|none is an L2 marker: if fresh-avg-ms stays (n/a) with fmt=other, switch to the node API.
        _logger.Log("info", "diag_summary", $"window={DiagMpvOptions.Fmt(wall.TotalSeconds)}s vo-drop-rate={DiagMpvOptions.Fmt(dropRate)}/s fresh-avg-ms={DiagMpvOptions.Fmt(freshMs)} vo-passes-fmt={DiagMpvOptions.VoPassesFormat(voPasses)} process-cpu={DiagMpvOptions.Fmt(cpuPct)}%");
        (_diagSummaryUtc, _diagSummaryDrop, _diagSummaryCpu) = (now, drop, cpu);
    }
#endif

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

        // S72 diagnostics: extra render_path items, omitted when mpv cannot read them (vo-delayed-frame-count is already vo-drop).
        foreach (var key in new[] { "container-fps", "estimated-vf-fps", "display-fps", "video-params/w", "video-params/h", "video-params/pixelformat", "video-sync", "avsync", "mistimed-frame-count",
                                  "video-codec", "estimated-display-fps", "vsync-jitter", "display-sync-active", "dwidth", "dheight", "osd-dimensions/w", "osd-dimensions/h", "hwdec-interop",
                                  "video-params/hw-pixelformat", "scale", "cscale", "dscale" })
        {
            var val = MpvNative.GetPropertyAndFree(_mpv, key);
            if (val != null)
            {
                deltaPart += $" {key}={val}";
            }
        }

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
            var reset = d < 0; // K1: mpv resets drop counters on seek; report current value instead.
            d = reset ? cur : d;
            var rate = d / dt;
            return $" {name}-delta={d.ToString(CultureInfo.InvariantCulture)}"
                   + $" {name}-rate={rate.ToString("0.###", CultureInfo.InvariantCulture)}/s{(reset ? $" {name}-reset=1" : "")}";
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
            catch (Exception ex)
            {
                // Observer must not kill the engine thread.
                _logger.Log("warn", "observer_exception", $"{ex.GetType().Name}: {ex.Message}");
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
        // S102: atomically take ownership so only one caller destroys the handle.
        // S104: take the handle under _handleGate so no getter is mid-call; destroy outside the lock (no UI-thread wait).
        nint h;
        lock (_handleGate)
        {
            h = Interlocked.Exchange(ref _mpv, nint.Zero);
        }
        if (h == nint.Zero)
        {
            return;
        }

        try
        {
            MpvNative.mpv_terminate_destroy(h);
        }
        catch (Exception ex)
        {
            _logger.Log("warn", "mpv_destroy_failed", $"{ex.GetType().Name}: {ex.Message}");
            // Best-effort shutdown.
        }
    }
}

internal static class NativeMethods
{
    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool SetWindowPos(nint hwnd, nint hwndInsertAfter, int x, int y, int cx, int cy, uint flags);
}
