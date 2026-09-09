using System.IO;
using System.Text.Json;

namespace DesktopMediaPlayer.Shell;

/// <summary>S18 Soft: persist/restore volume (+ mute) under LocalAppData (Shell-only).</summary>
internal static class VolumePrefsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    private sealed record PrefsDto(int Volume, bool Mute);

    private static string FilePath
    {
        get
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "desktop-media-player");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "volume-prefs.json");
        }
    }

    public static bool TryLoad(out int volume, out bool mute)
    {
        volume = 80;
        mute = false;
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

            volume = Math.Clamp(dto.Volume, 0, 100);
            mute = dto.Mute;
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static void Persist(int volume, bool mute)
    {
        try
        {
            var dto = new PrefsDto(Math.Clamp(volume, 0, 100), mute);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(dto, JsonOptions));
        }
        catch
        {
            // Ignore IO failures.
        }
    }
}
