namespace DesktopMediaPlayer.Contracts;

/// <summary>Persists last playback position per local media path (Phase A Resume).</summary>
public interface IResumeStore
{
    void Save(string path, double positionSeconds, double durationSeconds);
    bool TryLoad(string path, out double positionSeconds);
    void Clear(string path);
}
