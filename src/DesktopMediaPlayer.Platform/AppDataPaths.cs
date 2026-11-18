using System.Security.Cryptography;
using System.Text;

namespace DesktopMediaPlayer.Platform;

/// <summary>DATADIR: single source of app-data paths (default %LocalAppData%/desktop-media-player, or --data-dir).</summary>
public sealed class AppDataPaths
{
    public const string AppFolderName = "desktop-media-player";
    public const string DataDirSwitch = "--data-dir";

    public AppDataPaths(string root)
    {
        Root = root;
    }

    /// <summary>Set once at startup (before any store/logger); defaults to the per-user folder.</summary>
    public static AppDataPaths Current { get; set; } = new(DefaultRoot());

    public string Root { get; }
    public string Settings => Root;
    public string Logs => Root;
    public string Resume => Path.Combine(Root, "resume.json");
    public string Cache => Path.Combine(Root, "cache");

    public static string DefaultRoot() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppFolderName);

    /// <summary>
    /// Strips "--data-dir &lt;path&gt;" from args. Returns false when the switch is present but the path is
    /// invalid or cannot be created (caller must fail startup, never fall back). paths is null when unspecified.
    /// </summary>
    public static bool TryParseArgs(IReadOnlyList<string> args, out AppDataPaths? paths, out List<string> remaining)
    {
        ArgumentNullException.ThrowIfNull(args);
        paths = null;
        remaining = new List<string>(args.Count);
        for (var i = 0; i < args.Count; i++)
        {
            if (!string.Equals(args[i]?.Trim(), DataDirSwitch, StringComparison.OrdinalIgnoreCase))
            {
                remaining.Add(args[i]);
                continue;
            }

            if (paths is not null || i + 1 >= args.Count || !TryCreate(args[++i], out paths))
            {
                paths = null;
                return false;
            }
        }

        return true;
    }

    public static bool TryCreate(string? raw, out AppDataPaths? paths)
    {
        paths = null;
        var value = raw?.Trim().Trim('"').Trim();
        if (string.IsNullOrEmpty(value) || value[0] == '-' || !Path.IsPathFullyQualified(value))
        {
            return false;
        }

        try
        {
            var full = Path.GetFullPath(value);
            if (File.Exists(full))
            {
                return false;
            }

            Directory.CreateDirectory(full);
            paths = new AppDataPaths(full);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return false;
        }
    }
}

/// <summary>DATADIR: log-safe path text — "&lt;data&gt;/relative" under the data root, otherwise a file-name hash.</summary>
public static class PathMask
{
    /// <summary>Free text (e.g. ex.Message): data root -> "&lt;data&gt;", user profile -> "&lt;user&gt;" (case-insensitive).</summary>
    public static string MaskText(string? text, AppDataPaths? paths = null, string? userProfile = null, string? appBaseDir = null)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        var root = Path.TrimEndingDirectorySeparator((paths ?? AppDataPaths.Current).Root);
        var app = Path.TrimEndingDirectorySeparator(appBaseDir ?? AppContext.BaseDirectory);
        var user = Path.TrimEndingDirectorySeparator(userProfile ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
        // Most specific first: data root and app dir (longer prefix wins if nested), then user profile.
        var first = root.Length >= app.Length ? (root, "<data>") : (app, "<app>");
        var second = root.Length >= app.Length ? (app, "<app>") : (root, "<data>");
        var result = text;
        foreach (var (prefix, token) in new[] { first, second, (user, "<user>") })
        {
            if (prefix.Length > 0)
            {
                result = result.Replace(prefix, token, StringComparison.OrdinalIgnoreCase);
            }
        }

        return result;
    }

    public static string Mask(string? path, AppDataPaths? paths = null)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        var root = Path.TrimEndingDirectorySeparator((paths ?? AppDataPaths.Current).Root);
        string full;
        try
        {
            full = Path.GetFullPath(path);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            full = path;
        }

        if (full.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            return "<data>/" + full[(root.Length + 1)..].Replace('\\', '/');
        }

        var name = Path.GetFileName(full);
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(name)))[..8].ToLowerInvariant();
        return "f-" + hash + Path.GetExtension(name);
    }
}
