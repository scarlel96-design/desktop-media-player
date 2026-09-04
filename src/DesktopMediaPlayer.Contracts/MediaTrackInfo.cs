namespace DesktopMediaPlayer.Contracts;

/// <summary>Immutable track descriptor returned by <see cref="IPlaybackEngine.ListTracks"/>.</summary>
public sealed record MediaTrackInfo(
    MediaTrackKind Kind,
    int Id,
    string? Title,
    string? Language,
    bool IsSelected);
