namespace DesktopMediaPlayer.Contracts;

/// <summary>Basic media info from engine properties only (no separate decoder).</summary>
public sealed record MediaInfoBasics(
    string? Path,
    string? Title,
    string? Format,
    string? VideoCodec,
    string? AudioCodec,
    int? Width,
    int? Height,
    double DurationSeconds);
