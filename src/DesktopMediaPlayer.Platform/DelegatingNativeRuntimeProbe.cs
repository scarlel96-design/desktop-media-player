namespace DesktopMediaPlayer.Platform;

using DesktopMediaPlayer.Contracts;

/// <summary>Composition-friendly probe (KI-010). Shell never sees Engine types.</summary>
public sealed class DelegatingNativeRuntimeProbe : INativeRuntimeProbe
{
    private readonly Func<(bool Ok, string? Detail)> _probe;

    public DelegatingNativeRuntimeProbe(Func<(bool Ok, string? Detail)> probe)
    {
        _probe = probe ?? throw new ArgumentNullException(nameof(probe));
    }

    public bool TryProbe(out string? detail)
    {
        var (ok, d) = _probe();
        detail = d;
        return ok;
    }
}
