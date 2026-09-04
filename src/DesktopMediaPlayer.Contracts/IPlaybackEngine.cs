namespace DesktopMediaPlayer.Contracts;

/// <summary>Core playback control surface. Implementations must be safe to call from the UI thread.</summary>
public interface IPlaybackEngine : IDisposable
{
    void Open(string path);
    void Close();
    void Play();
    void Pause();
    void Stop();
    void Seek(double seconds);
    void SetVolume(int volume); // 0-100
    PlaybackState GetState();
}
