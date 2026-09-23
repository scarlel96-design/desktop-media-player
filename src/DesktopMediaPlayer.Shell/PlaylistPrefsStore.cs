using System.IO;
using System.Text.Json;

namespace DesktopMediaPlayer.Shell;

/// <summary>S20 Soft: persist/restore playlist paths (+ CurrentIndex Soft) under LocalAppData (Shell-only).</summary>
internal static class PlaylistPrefsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    private sealed record PrefsDto(string[] Paths, int CurrentIndex);

    private static string FilePath
    {
        get
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "desktop-media-player");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "playlist-prefs.json");
        }
    }

    public static bool TryLoad(out IReadOnlyList<string> paths, out int currentIndex)
    {
        paths = Array.Empty<string>();
        currentIndex = -1;
        try
        {
            var path = FilePath;
            if (!File.Exists(path))
            {
                return false;
            }

            var dto = JsonSerializer.Deserialize<PrefsDto>(File.ReadAllText(path), JsonOptions);
            if (dto is null)
            {
                return false;
            }

            paths = dto.Paths ?? Array.Empty<string>();
            currentIndex = dto.CurrentIndex;
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static void Persist(IReadOnlyList<string> paths, int currentIndex)
    {
        try
        {
            var list = paths?.ToArray() ?? Array.Empty<string>();
            var idx = list.Length == 0
                ? -1
                : Math.Clamp(currentIndex, -1, list.Length - 1);
            var dto = new PrefsDto(list, idx);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(dto, JsonOptions));
        }
        catch
        {
            // Ignore IO failures.
        }
    }
}
