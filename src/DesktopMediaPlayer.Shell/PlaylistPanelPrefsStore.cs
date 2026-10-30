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
            var dir = DesktopMediaPlayer.Platform.AppDataPaths.Current.Settings;
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
        catch (Exception ex)
        {
            (System.Windows.Application.Current as App)?.Logger?.Log("warn", "prefs_load_failed", $"PlaylistPanelPrefsStore: {ex.GetType().Name}: {ex.Message}");
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
        catch (Exception ex)
        {
            (System.Windows.Application.Current as App)?.Logger?.Log("warn", "prefs_persist_failed", $"PlaylistPanelPrefsStore: {ex.GetType().Name}: {ex.Message}");
            // Ignore IO failures.
        }
    }
}
