#if DMP_DIAG
using System.Globalization;
using System.Text.Json;

namespace DesktopMediaPlayer.Engine.Mpv;

/// <summary>
/// DIAG-only (DMP_DIAG): mpv option injection from diag-mpv.txt next to the exe, one key=value per line.
/// Pure helpers; the engine applies accepted options pre-init in one place. Never compiled into Release.
/// </summary>
public static class DiagMpvOptions
{
    public const string FileName = "diag-mpv.txt";
    public const int MaxValueLength = 64;

    public static readonly IReadOnlyList<string> AllowedKeys =
        new[] { "dither-depth", "dither", "d3d11-adapter", "scale", "cscale", "dscale", "hwdec" };

    public sealed record Result(
        IReadOnlyList<KeyValuePair<string, string>> Accepted,
        IReadOnlyList<KeyValuePair<string, string>> Rejected);

    /// <summary>Rejected reasons: format (no key=value), not_allowed (key outside allowlist), bad_value. Duplicate key: last wins.</summary>
    public static Result Parse(IEnumerable<string>? lines)
    {
        var accepted = new List<KeyValuePair<string, string>>();
        var rejected = new List<KeyValuePair<string, string>>();
        foreach (var raw in lines ?? Array.Empty<string>())
        {
            var line = raw?.Trim() ?? "";
            if (line.Length == 0)
            {
                continue;
            }

            var eq = line.IndexOf('=');
            if (eq <= 0)
            {
                rejected.Add(new(line.Length > MaxValueLength ? line[..MaxValueLength] : line, "format"));
                continue;
            }

            var key = line[..eq].Trim();
            var value = line[(eq + 1)..].Trim();
            if (!AllowedKeys.Contains(key, StringComparer.Ordinal))
            {
                rejected.Add(new(key.Length > MaxValueLength ? key[..MaxValueLength] : key, "not_allowed"));
                continue;
            }

            if (!IsValidValue(value))
            {
                rejected.Add(new(key, "bad_value"));
                continue;
            }

            accepted.RemoveAll(p => p.Key == key);
            accepted.Add(new(key, value));
        }

        return new Result(accepted, rejected);
    }

    public static bool IsValidValue(string? value) =>
        !string.IsNullOrEmpty(value)
        && value.Length <= MaxValueLength
        && value.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.');

    /// <summary>True when cscale was injected; the A5-b policy must then not overwrite it.</summary>
    public static bool OverridesCscale(IEnumerable<string> appliedKeys) =>
        appliedKeys.Contains("cscale", StringComparer.Ordinal);

    /// <summary>Sum of vo-passes fresh[].avg (ns, mpv JSON) in ms; null when unreadable.</summary>
    public static double? SumFreshAvgMs(string? voPassesJson)
    {
        if (string.IsNullOrWhiteSpace(voPassesJson))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(voPassesJson);
            if (!doc.RootElement.TryGetProperty("fresh", out var fresh) || fresh.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            double ns = 0;
            foreach (var pass in fresh.EnumerateArray())
            {
                if (pass.TryGetProperty("avg", out var avg) && avg.TryGetDouble(out var v))
                {
                    ns += v;
                }
            }

            return ns / 1_000_000.0;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>Process CPU % of all cores over a window; null when the window is empty.</summary>
    public static double? CpuPercent(TimeSpan cpuDelta, TimeSpan wallDelta, int processorCount)
    {
        if (wallDelta <= TimeSpan.Zero || processorCount <= 0 || cpuDelta < TimeSpan.Zero)
        {
            return null;
        }

        return cpuDelta.TotalMilliseconds / (wallDelta.TotalMilliseconds * processorCount) * 100.0;
    }

    public static string Fmt(double? v) => v?.ToString("0.###", CultureInfo.InvariantCulture) ?? "(n/a)";
}
#endif
