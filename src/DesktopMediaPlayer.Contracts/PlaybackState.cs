namespace DesktopMediaPlayer.Contracts;

/// <summary>Playback lifecycle state exposed by <see cref="IPlaybackEngine"/>.</summary>
public enum PlaybackState
{
    Idle,
    Opening,
    Playing,
    Paused,
    Stopped,
    Ended,
    Error
}
