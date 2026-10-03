using System.IO;
using System.Text.Json;

namespace DesktopMediaPlayer.Shell;

/// <summary>S45 Soft: persist/restore PlaylistPanel open Soft under LocalAppData (Shell-only).</summary>
internal static class PlaylistPanelPrefsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    private sealed record PrefsDto(bool Open);

    private static string FilePath
    {
        get
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "desktop-media-player");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "playlist-panel-prefs.json");
        }
    }

    public static bool TryLoad(out bool open)
    {
        open = false;
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

            open = dto.Open;
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static void Persist(bool open)
    {
        try
        {
            var dto = new PrefsDto(open);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(dto, JsonOptions));
        }
        catch
        {
            // Ignore IO failures.
        }
    }
}
