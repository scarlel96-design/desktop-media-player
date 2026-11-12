using System;
using System.Collections.Generic;
using System.IO;

namespace DesktopMediaPlayer.Playback;

/// <summary>Pure command-line parsing for "open this file on startup". Testable; no IO except the injected probe.</summary>
public static class StartupArgs
{
    /// <summary>
    /// Returns the full path of the first existing file argument, or null.
    /// Outer quotes are trimmed, "-" switches are skipped ("/" args are checked via fileExists), relative paths are made absolute.
    /// </summary>
    public static string? FirstMediaPath(IReadOnlyList<string> args, Func<string, bool> fileExists)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(fileExists);

        foreach (var raw in args)
        {
            var arg = raw?.Trim().Trim('"').Trim();
            if (string.IsNullOrEmpty(arg) || arg[0] == '-')
            {
                continue;
            }

            string full;
            try
            {
                full = Path.GetFullPath(arg);
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
            {
                continue;
            }

            if (fileExists(full))
            {
                return full;
            }
        }

        return null;
    }
}
