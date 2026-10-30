using DesktopMediaPlayer.Contracts;

namespace DesktopMediaPlayer.Engine.Mpv;

/// <summary>A5-a: d3d11-adapter value to set pre-init (null = leave unset) and why.</summary>
public sealed record GpuAdapterDecision(string? Adapter, string Reason);

/// <summary>A5-a: pure adapter choice. Reasons: default | selected | no_hp_adapter | invalid_pref | probe_failed.</summary>
public static class GpuAdapterPolicy
{
    public const string PrefsKey = "gpuPreference";

    /// <param name="pref">null = invalid stored value (behaves as Auto).</param>
    /// <param name="adapters">Descriptions in DXGI high-performance order, [0] = highest performance.</param>
    /// <param name="defaultAdapter">Description of EnumAdapters1(0), the adapter mpv uses when d3d11-adapter is unset.</param>
    public static GpuAdapterDecision Decide(GpuPreference? pref, IReadOnlyList<string> adapters, string? defaultAdapter, bool probeFailed = false)
    {
        if (pref is null)
        {
            return new(null, "invalid_pref");
        }

        if (pref != GpuPreference.HighPerformance)
        {
            return new(null, "default");
        }

        if (probeFailed)
        {
            return new(null, "probe_failed");
        }

        var hp = adapters.Count > 0 ? adapters[0] : null;
        if (string.IsNullOrWhiteSpace(hp))
        {
            return new(null, "no_hp_adapter");
        }

        return string.Equals(hp, defaultAdapter, StringComparison.Ordinal) ? new(null, "default") : new(hp, "selected");
    }

    /// <summary>Missing/empty -> Auto; unknown value -> null (invalid_pref).</summary>
    public static GpuPreference? ParsePreference(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return GpuPreference.Auto;
        return Enum.TryParse<GpuPreference>(value.Trim(), ignoreCase: true, out var p) && Enum.IsDefined(p) ? p : null;
    }
}
