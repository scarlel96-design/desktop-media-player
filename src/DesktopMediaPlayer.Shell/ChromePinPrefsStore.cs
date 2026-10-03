using System.IO;
using System.Text.Json;

namespace DesktopMediaPlayer.Shell;

/// <summary>S36 Soft: persist/restore chrome pin Soft under LocalAppData (Shell-only).</summary>
internal static class ChromePinPrefsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    private sealed record PrefsDto(bool Pinned);

    private static string FilePath
    {
        get
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "desktop-media-player");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "chrome-pin-prefs.json");
        }
    }

    public static bool TryLoad(out bool pinned)
    {
        pinned = false;
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

            pinned = dto.Pinned;
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static void Persist(bool pinned)
    {
        try
        {
            var dto = new PrefsDto(pinned);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(dto, JsonOptions));
        }
        catch
        {
            // Ignore IO failures.
        }
    }
}
