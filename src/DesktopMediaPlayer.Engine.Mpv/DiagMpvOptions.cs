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

    /// <summary>Policy-only key: replaces the GPU policy's adapter; never passed to mpv, never a d3d11-adapter override.</summary>
    public const string PolicyAdapterKey = "gpu-policy-adapter-override";
    public const int MaxAdapterLength = 256;

    /// <summary>App-only key: gpu-policy-fault=vo injects one VO failure into the real fallback path.</summary>
    public const string PolicyFaultKey = "gpu-policy-fault";

    public sealed record Result(
        IReadOnlyList<KeyValuePair<string, string>> Accepted,
        IReadOnlyList<KeyValuePair<string, string>> Rejected,
        string? PolicyAdapterOverride = null,
        bool PolicyFaultVo = false);

    /// <summary>First existing diag-mpv.txt in dirs (no merge). Source: data (index 0) | app (index 1) | none.</summary>
    public static (string? Path, string Source) ResolveFile(IReadOnlyList<string>? dirs, Func<string, bool> exists)
    {
        for (var i = 0; i < Math.Min(dirs?.Count ?? 0, 2); i++)
        {
            if (!string.IsNullOrEmpty(dirs![i]) && exists(System.IO.Path.Combine(dirs[i], FileName)))
            {
                return (System.IO.Path.Combine(dirs[i], FileName), i == 0 ? "data" : "app");
            }
        }

        return (null, "none");
    }

    /// <summary>Override replaces the adapter only when the policy set one; Ignored = override present but no policy adapter.</summary>
    public static (GpuAdapterDecision Decision, bool Ignored) ApplyAdapterOverride(GpuAdapterDecision decision, string? adapterOverride)
    {
        if (adapterOverride is null)
        {
            return (decision, false);
        }

        return decision.Adapter is null ? (decision, true) : (new GpuAdapterDecision(adapterOverride, "diag_policy_override"), false);
    }

    /// <summary>Rejected reasons: format (no key=value), not_allowed (key outside allowlist), bad_value. Duplicate key: last wins.</summary>
    public static Result Parse(IEnumerable<string>? lines)
    {
        var accepted = new List<KeyValuePair<string, string>>();
        var rejected = new List<KeyValuePair<string, string>>();
        string? policyAdapter = null;
        var policyFaultVo = false;
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
            if (key == PolicyAdapterKey)
            {
                if (value.Length > 0 && value.Length <= MaxAdapterLength && value.IndexOfAny(new[] { '\r', '\n' }) < 0)
                {
                    policyAdapter = value;
                }
                else
                {
                    rejected.Add(new(key, "bad_value"));
                }

                continue;
            }

            if (key == PolicyFaultKey)
            {
                if (value == "vo")
                {
                    policyFaultVo = true;
                }
                else
                {
                    rejected.Add(new(key, "bad_value"));
                }

                continue;
            }

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

        return new Result(accepted, rejected, policyAdapter, policyFaultVo);
    }

    public static bool IsValidValue(string? value) =>
        !string.IsNullOrEmpty(value)
        && value.Length <= MaxValueLength
        && value.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.');

    /// <summary>True when cscale was injected; the A5-b policy must then not overwrite it.</summary>
    public static bool OverridesCscale(IEnumerable<string> appliedKeys) =>
        appliedKeys.Contains("cscale", StringComparer.Ordinal);

    /// <summary>L2 marker for the vo-passes string form: json (object), other (non-JSON text), none (empty).</summary>
    public static string VoPassesFormat(string? voPasses) =>
        string.IsNullOrWhiteSpace(voPasses) ? "none" : voPasses.TrimStart().StartsWith('{') ? "json" : "other";

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
