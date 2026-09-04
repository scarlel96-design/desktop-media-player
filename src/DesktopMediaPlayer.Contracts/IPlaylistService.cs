namespace DesktopMediaPlayer.Contracts;

/// <summary>
/// Minimal playlist. Opens items only via <see cref="IPlaybackEngine.Open"/> — never Engine Host.
/// Prev/Next at ends: <b>stop</b> (no wrap in Phase A).
/// </summary>
public interface IPlaylistService
{
    IReadOnlyList<string> Items { get; }
    int CurrentIndex { get; }
    bool CanPlayPrevious { get; }
    bool CanPlayNext { get; }

    void Add(string path);
    void Clear();
    void PlayAt(int index);
    void PlayNext();
    void PlayPrevious();
}
