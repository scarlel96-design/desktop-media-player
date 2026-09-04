namespace DesktopMediaPlayer.Contracts;

/// <summary>Observer for playback lifecycle, errors, and hardware acceleration changes.</summary>
public interface IPlaybackObserver
{
    void OnFirstFrame();
    void OnStateChanged(PlaybackState state);
    void OnError(string code, string message, bool recoverable);
    void OnHardwareAccelChanged(bool active, string reason);
}
