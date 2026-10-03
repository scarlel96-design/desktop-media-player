using System.IO;
using System.Text.Json;

namespace DesktopMediaPlayer.Shell;

/// <summary>S40 Soft: persist/restore last Open media directory Soft under LocalAppData (Shell-only).</summary>
internal static class LastOpenDirectoryPrefsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    private sealed record PrefsDto(string? Directory);

    private static string FilePath
    {
        get
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "desktop-media-player");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "last-open-directory-prefs.json");
        }
    }

    public static bool TryLoad(out string directory)
    {
        directory = string.Empty;
        try
        {
            var path = FilePath;
            if (!File.Exists(path))
            {
                return false;
            }

            var dto = JsonSerializer.Deserialize<PrefsDto>(File.ReadAllText(path), JsonOptions);
            if (dto is null || string.IsNullOrWhiteSpace(dto.Directory))
            {
                return false;
            }

            directory = dto.Directory;
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static void Persist(string directory)
    {
        if (string.IsNullOrWhiteSpace(directory))
        {
            return;
        }

        try
        {
            var dto = new PrefsDto(directory);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(dto, JsonOptions));
        }
        catch
        {
            // Ignore IO failures.
        }
    }
}
