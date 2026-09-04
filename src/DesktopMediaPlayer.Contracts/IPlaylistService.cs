namespace DesktopMediaPlayer.Contracts;

/// <summary>
/// Minimal playlist. Opens items only via <see cref="IPlaybackEngine.Open"/> — never Engine Host.
/// </summary>
public interface IPlaylistService
{
    IReadOnlyList<string> Items { get; }
    int CurrentIndex { get; }

    void Add(string path);
    void Clear();
    void PlayAt(int index);
    void PlayNext();
    void PlayPrevious();
}
