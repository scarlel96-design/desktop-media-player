namespace DesktopMediaPlayer.Contracts;

/// <summary>
/// Platform/Facade health check for native runtime (KI-010).
/// Shell must call this instead of Engine types.
/// </summary>
public interface INativeRuntimeProbe
{
    bool TryProbe(out string? detail);
}
