using System.IO;
using System.Text.Json;

namespace DesktopMediaPlayer.Shell;

/// <summary>S19 Soft: persist/restore AppThemeMode under LocalAppData (Shell-only).</summary>
internal static class ThemePrefsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    private sealed record PrefsDto(string Theme);

    private static string FilePath
    {
        get
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "desktop-media-player");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "theme-prefs.json");
        }
    }

    public static bool TryLoad(out AppThemeMode theme)
    {
        theme = AppThemeMode.Dark;
        try
        {
            var path = FilePath;
            if (!File.Exists(path))
            {
                return false;
            }

            var dto = JsonSerializer.Deserialize<PrefsDto>(File.ReadAllText(path), JsonOptions);
            if (dto is null || string.IsNullOrWhiteSpace(dto.Theme))
            {
                return false;
            }

            if (!Enum.TryParse<AppThemeMode>(dto.Theme, ignoreCase: true, out theme))
            {
                theme = AppThemeMode.Dark;
                return false;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    public static void Persist(AppThemeMode theme)
    {
        try
        {
            var dto = new PrefsDto(theme.ToString());
            File.WriteAllText(FilePath, JsonSerializer.Serialize(dto, JsonOptions));
        }
        catch
        {
            // Ignore IO failures.
        }
    }
}
