namespace DesktopMediaPlayer.Contracts;

/// <summary>A5-a: which GPU mpv should render on. Auto leaves the choice to mpv/DXGI.</summary>
public enum GpuPreference
{
    Auto,
    HighPerformance
}
