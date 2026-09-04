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

    // Phase A extensions (Architecture S0 — approved)
    void FrameStep(int steps);
    void SetMute(bool mute);
    bool GetMute();
    double GetPosition();
    double GetDuration();
    IReadOnlyList<MediaTrackInfo> ListTracks();
    void SelectTrack(MediaTrackKind kind, int id);
    void LoadExternalSubtitle(string path);
    void SetSubtitleOffset(double seconds);
    void Screenshot(string path);
}
