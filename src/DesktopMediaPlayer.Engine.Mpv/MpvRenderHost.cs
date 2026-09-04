using DesktopMediaPlayer.Contracts;

namespace DesktopMediaPlayer.Engine.Mpv;

/// <summary>
/// HWND binder for libmpv <c>wid</c>. Shell creates the child HWND; this host only attaches mpv to it.
/// </summary>
public sealed class MpvRenderHost : IRenderHost
{
    private readonly MpvPlaybackEngine _engine;

    internal MpvRenderHost(MpvPlaybackEngine engine)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
    }

    public void Attach(nint hwndOrParent) => _engine.AttachWindow(hwndOrParent);

    public void Detach() => _engine.AttachWindow(nint.Zero);

    public void Resize(int width, int height) => _engine.ResizeWindow(width, height);
}
