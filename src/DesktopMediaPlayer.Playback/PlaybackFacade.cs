using System.Threading;
using DesktopMediaPlayer.Contracts;

namespace DesktopMediaPlayer.Playback;

/// <summary>
/// Facade over an inner <see cref="IPlaybackEngine"/> with volume clamping,
/// path validation, observer fan-out, and native health probe (KI-010).
/// </summary>
public sealed class PlaybackFacade : IPlaybackEngine, IPlaybackObserver, INativeRuntimeProbe
{
    private readonly IPlaybackEngine _inner;
    private INativeRuntimeProbe? _probe;
    private readonly object _observersGate = new();
    private readonly List<IPlaybackObserver> _observers = new();
    private readonly object _positionGate = new();
    private static readonly TimeSpan PositionThrottle = TimeSpan.FromMilliseconds(100);
    private readonly Timer _positionTimer;
    private DateTime _lastPositionFanOutUtc = DateTime.MinValue;
    private double _pendingPosition;
    private double _pendingDuration;
    private double _lastDeliveredPosition = double.NaN;
    private double _lastDeliveredDuration = double.NaN;
    private bool _positionPending;
    private bool _positionTimerArmed;
    private bool _disposed;

    public PlaybackFacade(IPlaybackEngine inner, params IPlaybackObserver[] observers)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _probe = inner as INativeRuntimeProbe;
        _positionTimer = new Timer(PositionTimerCallback, null, Timeout.Infinite, Timeout.Infinite);
        if (observers is { Length: > 0 })
        {
            foreach (var o in observers)
            {
                if (o is not null)
                {
                    _observers.Add(o);
                }
            }
        }
    }

    /// <summary>Optional render host exposed by the inner engine.</summary>
    public IRenderHost? RenderHost { get; set; }

    /// <summary>Override native probe (KI-010). Defaults to inner if it implements <see cref="INativeRuntimeProbe"/>.</summary>
    public INativeRuntimeProbe? NativeProbe
    {
        get => _probe;
        set => _probe = value;
    }

    public void AddObserver(IPlaybackObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        lock (_observersGate)
        {
            if (!_observers.Contains(observer))
            {
                _observers.Add(observer);
            }
        }
    }

    public void RemoveObserver(IPlaybackObserver observer)
    {
        if (observer is null)
        {
            return;
        }

        lock (_observersGate)
        {
            _observers.Remove(observer);
        }
    }

    public bool TryProbe(out string? detail)
    {
        ThrowIfDisposed();
        if (_probe is null)
        {
            detail = "No native runtime probe registered.";
            return false;
        }

        return _probe.TryProbe(out detail);
    }

    public void Open(string path)
    {
        ThrowIfDisposed();
        if (string.IsNullOrWhiteSpace(path))
        {
            OnError("invalid_path", "Open path is null or empty.", recoverable: true);
            return;
        }

        _inner.Open(path);
    }

    public void Close()
    {
        ThrowIfDisposed();
        _inner.Close();
    }

    public void Play()
    {
        ThrowIfDisposed();
        _inner.Play();
    }

    public void Pause()
    {
        ThrowIfDisposed();
        _inner.Pause();
    }

    public void Stop()
    {
        ThrowIfDisposed();
        _inner.Stop();
    }

    public void Seek(double seconds)
    {
        ThrowIfDisposed();
        _inner.Seek(seconds);
    }

    public void SetVolume(int volume)
    {
        ThrowIfDisposed();
        _inner.SetVolume(Math.Clamp(volume, 0, 100));
    }

    public PlaybackState GetState()
    {
        ThrowIfDisposed();
        return _inner.GetState();
    }

    public void FrameStep(int steps)
    {
        ThrowIfDisposed();
        _inner.FrameStep(steps);
    }

    public void SetMute(bool mute)
    {
        ThrowIfDisposed();
        _inner.SetMute(mute);
    }

    public bool GetMute()
    {
        ThrowIfDisposed();
        return _inner.GetMute();
    }

    public double GetPosition()
    {
        ThrowIfDisposed();
        return _inner.GetPosition();
    }

    public double GetDuration()
    {
        ThrowIfDisposed();
        return _inner.GetDuration();
    }

    public IReadOnlyList<MediaTrackInfo> ListTracks()
    {
        ThrowIfDisposed();
        return _inner.ListTracks();
    }

    public void SelectTrack(MediaTrackKind kind, int id)
    {
        ThrowIfDisposed();
        _inner.SelectTrack(kind, id);
    }

    public void LoadExternalSubtitle(string path)
    {
        ThrowIfDisposed();
        if (string.IsNullOrWhiteSpace(path))
        {
            OnError("invalid_path", "Subtitle path is null or empty.", recoverable: true);
            return;
        }

        _inner.LoadExternalSubtitle(path);
    }

    public void SetSubtitleOffset(double seconds)
    {
        ThrowIfDisposed();
        _inner.SetSubtitleOffset(seconds);
    }

    public void Screenshot(string path)
    {
        ThrowIfDisposed();
        if (string.IsNullOrWhiteSpace(path))
        {
            OnError("invalid_path", "Screenshot path is null or empty.", recoverable: true);
            return;
        }

        _inner.Screenshot(path);
    }

    public MediaInfoBasics GetMediaInfo()
    {
        ThrowIfDisposed();
        return _inner.GetMediaInfo();
    }

    public double GetBufferedEndSeconds()
    {
        ThrowIfDisposed();
        return _inner.GetBufferedEndSeconds();
    }

    public void OnFirstFrame() => FanOut(o => o.OnFirstFrame());

    public void OnStateChanged(PlaybackState state) => FanOut(o => o.OnStateChanged(state));

    public void OnError(string code, string message, bool recoverable) =>
        FanOut(o => o.OnError(code, message, recoverable));

    public void OnHardwareAccelChanged(bool active, string reason) =>
        FanOut(o => o.OnHardwareAccelChanged(active, reason));

    /// <summary>S13 Soft: fan-out ≤100ms; suppress identical (pos,dur); always deliver latest pending.</summary>
    public void OnPositionChanged(double positionSeconds, double durationSeconds)
    {
        lock (_positionGate)
        {
            if (_disposed)
            {
                return;
            }

            // Identical to pending or last delivered → suppress.
            if ((_positionPending
                 && NearlyEqual(_pendingPosition, positionSeconds)
                 && NearlyEqual(_pendingDuration, durationSeconds))
                || (NearlyEqual(_lastDeliveredPosition, positionSeconds)
                    && NearlyEqual(_lastDeliveredDuration, durationSeconds)))
            {
                return;
            }

            _pendingPosition = positionSeconds;
            _pendingDuration = durationSeconds;
            _positionPending = true;

            var elapsed = DateTime.UtcNow - _lastPositionFanOutUtc;
            if (elapsed >= PositionThrottle)
            {
                EmitPendingPositionLocked();
                return;
            }

            if (_positionTimerArmed)
            {
                return;
            }

            var delay = PositionThrottle - elapsed;
            if (delay < TimeSpan.Zero)
            {
                delay = TimeSpan.Zero;
            }

            _positionTimerArmed = true;
            _positionTimer.Change(delay, Timeout.InfiniteTimeSpan);
        }
    }

    private void PositionTimerCallback(object? state)
    {
        lock (_positionGate)
        {
            _positionTimerArmed = false;
            if (_disposed || !_positionPending)
            {
                return;
            }

            EmitPendingPositionLocked();
        }
    }

    private void EmitPendingPositionLocked()
    {
        var pos = _pendingPosition;
        var dur = _pendingDuration;
        _positionPending = false;
        _lastDeliveredPosition = pos;
        _lastDeliveredDuration = dur;
        _lastPositionFanOutUtc = DateTime.UtcNow;
        // Fan-out outside nested observer risks: still under lock briefly — copy then release.
        // Call FanOut while holding lock is OK (observers shouldn't re-enter position).
        FanOut(o => o.OnPositionChanged(pos, dur));
    }

    private static bool NearlyEqual(double a, double b) =>
        (double.IsNaN(a) && double.IsNaN(b))
        || Math.Abs(a - b) < 1e-9;

    private void FanOut(Action<IPlaybackObserver> action)
    {
        IPlaybackObserver[] snapshot;
        lock (_observersGate)
        {
            snapshot = _observers.ToArray();
        }

        foreach (var observer in snapshot)
        {
            try
            {
                action(observer);
            }
            catch
            {
                // Observer exceptions must not break playback.
            }
        }
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        lock (_positionGate)
        {
            _disposed = true;
            _positionTimerArmed = false;
            _positionTimer.Change(Timeout.Infinite, Timeout.Infinite);
            if (_positionPending)
            {
                EmitPendingPositionLocked();
            }

            _positionTimer.Dispose();
        }

        _inner.Dispose();
        lock (_observersGate)
        {
            _observers.Clear();
        }
    }
}
