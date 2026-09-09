using System.IO;
using System.Text.Json;
using System.Windows;

namespace DesktopMediaPlayer.Shell;

/// <summary>S17 Soft: persist/restore/clamp main window bounds to LocalAppData (Shell-only).</summary>
internal static class WindowBoundsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    private sealed record BoundsDto(double Left, double Top, double Width, double Height, string State);

    private static string FilePath
    {
        get
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "desktop-media-player");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "window-bounds.json");
        }
    }

    public static void TryRestore(Window window)
    {
        try
        {
            var path = FilePath;
            if (!File.Exists(path))
            {
                return;
            }

            var dto = JsonSerializer.Deserialize<BoundsDto>(File.ReadAllText(path), JsonOptions);
            if (dto is null || dto.Width < window.MinWidth || dto.Height < window.MinHeight)
            {
                return;
            }

            var width = Math.Clamp(dto.Width, window.MinWidth, SystemParameters.VirtualScreenWidth);
            var height = Math.Clamp(dto.Height, window.MinHeight, SystemParameters.VirtualScreenHeight);
            var left = dto.Left;
            var top = dto.Top;
            ClampToVirtualScreen(ref left, ref top, width, height);

            window.WindowStartupLocation = WindowStartupLocation.Manual;
            window.Left = left;
            window.Top = top;
            window.Width = width;
            window.Height = height;

            if (Enum.TryParse<WindowState>(dto.State, ignoreCase: true, out var state)
                && state is WindowState.Normal or WindowState.Maximized)
            {
                window.WindowState = state;
            }
        }
        catch
        {
            // Corrupt bounds file — keep defaults.
        }
    }

    public static void Persist(Window window)
    {
        try
        {
            // Prefer restore bounds when maximized so next Normal launch is sane.
            var left = window.Left;
            var top = window.Top;
            var width = window.Width;
            var height = window.Height;
            if (window.WindowState == WindowState.Maximized && window.RestoreBounds.Width > 0)
            {
                left = window.RestoreBounds.Left;
                top = window.RestoreBounds.Top;
                width = window.RestoreBounds.Width;
                height = window.RestoreBounds.Height;
            }

            width = Math.Max(window.MinWidth, width);
            height = Math.Max(window.MinHeight, height);
            ClampToVirtualScreen(ref left, ref top, width, height);

            var dto = new BoundsDto(left, top, width, height, window.WindowState.ToString());
            File.WriteAllText(FilePath, JsonSerializer.Serialize(dto, JsonOptions));
        }
        catch
        {
            // Ignore IO failures.
        }
    }

    private static void ClampToVirtualScreen(ref double left, ref double top, double width, double height)
    {
        var screenLeft = SystemParameters.VirtualScreenLeft;
        var screenTop = SystemParameters.VirtualScreenTop;
        var screenRight = screenLeft + SystemParameters.VirtualScreenWidth;
        var screenBottom = screenTop + SystemParameters.VirtualScreenHeight;

        // Keep at least 80px of title/chrome visible.
        const double margin = 80;
        if (left + width < screenLeft + margin)
        {
            left = screenLeft;
        }

        if (top + height < screenTop + margin)
        {
            top = screenTop;
        }

        if (left > screenRight - margin)
        {
            left = Math.Max(screenLeft, screenRight - width);
        }

        if (top > screenBottom - margin)
        {
            top = Math.Max(screenTop, screenBottom - height);
        }

        if (width > SystemParameters.VirtualScreenWidth)
        {
            width = SystemParameters.VirtualScreenWidth;
        }

        if (height > SystemParameters.VirtualScreenHeight)
        {
            height = SystemParameters.VirtualScreenHeight;
        }

        // Silence unused if only left/top adjusted — width/height are by-value here.
        _ = width;
        _ = height;
    }
}
