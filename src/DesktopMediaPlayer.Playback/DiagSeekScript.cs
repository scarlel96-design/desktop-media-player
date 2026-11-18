#if DMP_DIAG
using System;
using System.Collections.Generic;
using System.Globalization;

namespace DesktopMediaPlayer.Playback;

/// <summary>DMP_DIAG only: parses a measurement seek script ("&lt;wait_ms&gt; &lt;cmd&gt; [value]" per line). Pure; no IO.</summary>
public static class DiagSeekScript
{
    public const string Switch = "--diag-seek-script";
    public const int MaxLines = 200;

    public enum Command { Abs, Rel, Pause, Play }

    public readonly record struct Step(int WaitMs, Command Cmd, double Value);

    /// <summary>Steps when valid; otherwise Steps is empty and RejectedLine is the 1-based offending line.</summary>
    public readonly record struct Result(IReadOnlyList<Step> Steps, int? RejectedLine);

    /// <summary>Removes "--diag-seek-script &lt;path&gt;" from args so the path is never treated as media.</summary>
    public static string? ExtractPath(IReadOnlyList<string> args, out List<string> remaining)
    {
        ArgumentNullException.ThrowIfNull(args);
        remaining = new List<string>(args.Count);
        string? path = null;
        for (var i = 0; i < args.Count; i++)
        {
            if (string.Equals(args[i], Switch, StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 < args.Count)
                {
                    path ??= args[i + 1].Trim().Trim('"');
                    i++;
                }

                continue;
            }

            remaining.Add(args[i]);
        }

        return string.IsNullOrEmpty(path) ? null : path;
    }

    public static Result Parse(IReadOnlyList<string> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);
        if (lines.Count > MaxLines)
        {
            return new Result(Array.Empty<Step>(), MaxLines + 1);
        }

        var steps = new List<Step>();
        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i]?.Trim() ?? string.Empty;
            if (line.Length == 0 || line[0] == '#')
            {
                continue;
            }

            if (!TryParseLine(line, out var step))
            {
                return new Result(Array.Empty<Step>(), i + 1);
            }

            steps.Add(step);
        }

        return new Result(steps, null);
    }

    private static bool TryParseLine(string line, out Step step)
    {
        step = default;
        var parts = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2 || !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var wait))
        {
            return false;
        }

        var cmd = parts[1].ToLowerInvariant();
        if (cmd is "pause" or "play")
        {
            if (parts.Length != 2)
            {
                return false;
            }

            step = new Step(wait, cmd == "pause" ? Command.Pause : Command.Play, 0);
            return true;
        }

        if (parts.Length != 3
            || !double.TryParse(parts[2], NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value)
            || !double.IsFinite(value))
        {
            return false;
        }

        switch (cmd)
        {
            case "abs" when value >= 0:
                step = new Step(wait, Command.Abs, value);
                return true;
            case "rel":
                step = new Step(wait, Command.Rel, value);
                return true;
            default:
                return false;
        }
    }
}
#endif
