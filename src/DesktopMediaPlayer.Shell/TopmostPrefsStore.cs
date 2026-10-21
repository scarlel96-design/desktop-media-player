using System.IO;
using System.Text.Json;

namespace DesktopMediaPlayer.Shell;

/// <summary>S38 Soft: persist/restore Window.Topmost Soft under LocalAppData (Shell-only).</summary>
internal static class TopmostPrefsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    private sealed record PrefsDto(bool Topmost);

    private static string FilePath
    {
        get
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "desktop-media-player");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "topmost-prefs.json");
        }
    }

    public static bool TryLoad(out bool topmost)
    {
        topmost = false;
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

            topmost = dto.Topmost;
            return true;
        }
        catch (Exception ex)
        {
            (System.Windows.Application.Current as App)?.Logger?.Log("warn", "prefs_load_failed", $"TopmostPrefsStore: {ex.GetType().Name}: {ex.Message}");
            return false;
        }
    }

    public static void Persist(bool topmost)
    {
        try
        {
            var dto = new PrefsDto(topmost);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(dto, JsonOptions));
        }
        catch (Exception ex)
        {
            (System.Windows.Application.Current as App)?.Logger?.Log("warn", "prefs_persist_failed", $"TopmostPrefsStore: {ex.GetType().Name}: {ex.Message}");
            // Ignore IO failures.
        }
    }
}
