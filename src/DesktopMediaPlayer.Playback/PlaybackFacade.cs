using DesktopMediaPlayer.Contracts;

namespace DesktopMediaPlayer.Playback;

/// <summary>
/// Facade over an inner <see cref="IPlaybackEngine"/> with volume clamping,
/// null/empty path validation, and thread-safe observer fan-out.
/// </summary>
public sealed class PlaybackFacade : IPlaybackEngine, IPlaybackObserver
{
    private readonly IPlaybackEngine _inner;
    private readonly object _observersGate = new();
    private readonly List<IPlaybackObserver> _observers = new();
    private bool _disposed;

    public PlaybackFacade(IPlaybackEngine inner, params IPlaybackObserver[] observers)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
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

    /// <summary>Optional render host exposed by the inner engine (e.g. MpvPlaybackEngine.RenderHost).</summary>
    public IRenderHost? RenderHost { get; set; }

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
        var clamped = Math.Clamp(volume, 0, 100);
        _inner.SetVolume(clamped);
    }

    public PlaybackState GetState()
    {
        ThrowIfDisposed();
        return _inner.GetState();
    }

    public void OnFirstFrame() => FanOut(o => o.OnFirstFrame());

    public void OnStateChanged(PlaybackState state) => FanOut(o => o.OnStateChanged(state));

    public void OnError(string code, string message, bool recoverable) =>
        FanOut(o => o.OnError(code, message, recoverable));

    public void OnHardwareAccelChanged(bool active, string reason) =>
        FanOut(o => o.OnHardwareAccelChanged(active, reason));

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

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _inner.Dispose();
        lock (_observersGate)
        {
            _observers.Clear();
        }
    }
}
