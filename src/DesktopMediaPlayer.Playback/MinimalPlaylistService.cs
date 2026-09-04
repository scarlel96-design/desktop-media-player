using DesktopMediaPlayer.Contracts;

namespace DesktopMediaPlayer.Playback;

/// <summary>Phase A minimal playlist — Open only through <see cref="IPlaybackEngine"/>. Ends stop (no wrap).</summary>
public sealed class MinimalPlaylistService : IPlaylistService
{
    private readonly IPlaybackEngine _engine;
    private readonly List<string> _items = new();
    private int _currentIndex = -1;

    public MinimalPlaylistService(IPlaybackEngine engine)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
    }

    public IReadOnlyList<string> Items
    {
        get
        {
            lock (_items)
            {
                return _items.ToArray();
            }
        }
    }

    public int CurrentIndex
    {
        get
        {
            lock (_items)
            {
                return _currentIndex;
            }
        }
    }

    public bool CanPlayPrevious
    {
        get
        {
            lock (_items)
            {
                return _currentIndex > 0;
            }
        }
    }

    public bool CanPlayNext
    {
        get
        {
            lock (_items)
            {
                return _currentIndex >= 0 && _currentIndex < _items.Count - 1;
            }
        }
    }

    public void Add(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        lock (_items)
        {
            _items.Add(path);
        }
    }

    public void Clear()
    {
        lock (_items)
        {
            _items.Clear();
            _currentIndex = -1;
        }
    }

    public void PlayAt(int index)
    {
        string path;
        lock (_items)
        {
            if (index < 0 || index >= _items.Count)
            {
                return;
            }

            _currentIndex = index;
            path = _items[index];
        }

        _engine.Open(path);
    }

    public void PlayNext()
    {
        lock (_items)
        {
            if (_items.Count == 0)
            {
                _engine.Stop();
                return;
            }

            var next = _currentIndex + 1;
            if (next >= _items.Count)
            {
                // Queue end: stop (no wrap).
                _engine.Stop();
                return;
            }

            _currentIndex = next;
            _engine.Open(_items[_currentIndex]);
        }
    }

    public void PlayPrevious()
    {
        lock (_items)
        {
            if (_items.Count == 0 || _currentIndex <= 0)
            {
                // Queue start: stop (no wrap).
                _engine.Stop();
                return;
            }

            _currentIndex -= 1;
            _engine.Open(_items[_currentIndex]);
        }
    }
}
