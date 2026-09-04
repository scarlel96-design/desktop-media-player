using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace DesktopMediaPlayer.Shell;

/// <summary>
/// HwndHost creating a child STATIC hwnd (WS_CHILD|WS_VISIBLE) for libmpv wid embedding.
/// WM_MOUSEACTIVATE returns MA_NOACTIVATE so focus is not trapped.
/// </summary>
public sealed class VideoHwndHost : HwndHost
{
    private const int WsChild = 0x40000000;
    private const int WsVisible = 0x10000000;
    private const int WmMouseActivate = 0x0021;
    private const int MaNoActivate = 3;

    /// <summary>Child STATIC HWND handle (same as Host handle after build).</summary>
    public nint Handle { get; private set; }

    protected override HandleRef BuildWindowCore(HandleRef hwndParent)
    {
        Handle = CreateWindowEx(
            0,
            "STATIC",
            string.Empty,
            WsChild | WsVisible,
            0,
            0,
            64,
            64,
            hwndParent.Handle,
            nint.Zero,
            nint.Zero,
            nint.Zero);

        return new HandleRef(this, Handle);
    }

    protected override void DestroyWindowCore(HandleRef hwnd)
    {
        if (hwnd.Handle != nint.Zero)
        {
            DestroyWindow(hwnd.Handle);
        }

        Handle = nint.Zero;
    }

    protected override nint WndProc(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
    {
        if (msg == WmMouseActivate)
        {
            handled = true;
            return MaNoActivate;
        }

        return base.WndProc(hwnd, msg, wParam, lParam, ref handled);
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint CreateWindowEx(
        int dwExStyle,
        string lpClassName,
        string lpWindowName,
        int dwStyle,
        int x,
        int y,
        int nWidth,
        int nHeight,
        nint hWndParent,
        nint hMenu,
        nint hInstance,
        nint lpParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyWindow(nint hWnd);
}
