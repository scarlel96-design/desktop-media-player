namespace DesktopMediaPlayer.Contracts;

/// <summary>Native window embedding surface for video output (HWND on Windows).</summary>
public interface IRenderHost
{
    void Attach(nint hwndOrParent);
    void Detach();
    void Resize(int width, int height); // physical pixels
}
