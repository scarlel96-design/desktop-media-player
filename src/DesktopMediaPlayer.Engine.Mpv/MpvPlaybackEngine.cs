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
public sealed class MpvPlaybackEngine : IPlaybackEngine
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
    public static bool ProbeNativeLibrary(out string? pathOrError)
    {
        if (MpvNative.TryProbeLibrary(out var loaded, out var err))
        {
            pathOrError = loaded;
            return true;
        }

        pathOrError = err;
        return false;
    }

    public void Open(string path) => Post(() =>
    {
        _pendingOpen = path;
        _firstFrameRaised = false;
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
        ReportHwdec();
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
