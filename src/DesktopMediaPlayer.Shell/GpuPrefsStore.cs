using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using DesktopMediaPlayer.Contracts;
using DesktopMediaPlayer.Playback;

namespace DesktopMediaPlayer.Shell;

/// <summary>A5-a: read-only GPU preference ("gpuPreference") under LocalAppData (Shell-only, no UI writes it).</summary>
internal static class GpuPrefsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    private sealed record PrefsDto([property: JsonPropertyName(GpuAdapterPolicy.PrefsKey)] string? Value);

    private static string FilePath
    {
        get
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "desktop-media-player");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "gpu-prefs.json");
        }
    }

    /// <summary>Missing file/key -> Auto; unknown value -> null (engine logs reason=invalid_pref and behaves as Auto).</summary>
    public static GpuPreference? Load()
    {
        try
        {
            var path = FilePath;
            if (!File.Exists(path))
            {
                return GpuPreference.Auto;
            }

            var dto = JsonSerializer.Deserialize<PrefsDto>(File.ReadAllText(path), JsonOptions);
            return GpuAdapterPolicy.ParsePreference(dto?.Value);
        }
        catch (Exception ex)
        {
            (System.Windows.Application.Current as App)?.Logger?.Log("warn", "prefs_load_failed", $"GpuPrefsStore: {ex.GetType().Name}: {ex.Message}");
            return GpuPreference.Auto;
        }
    }
}
