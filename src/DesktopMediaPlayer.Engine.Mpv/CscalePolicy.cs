namespace DesktopMediaPlayer.Engine.Mpv;

/// <summary>
/// A5-b: pure cscale decision. When the displayed rect is no larger than the chroma plane
/// in both axes, chroma is only ever downscaled, so bilinear is used; otherwise lanczos.
/// Only 4:2:0 sources (chromaShift = 1) are subject to the rule; 4:4:4/RGB (0) keep lanczos.
/// </summary>
public static class CscalePolicy
{
    public const string Bilinear = "bilinear";
    public const string Lanczos = "lanczos";

    public static string Decide(int dispW, int dispH, int srcW, int srcH, int chromaShift)
    {
        if (chromaShift <= 0 || dispW <= 0 || dispH <= 0 || srcW <= 0 || srcH <= 0)
        {
            return Lanczos;
        }

        var chromaW = (srcW + (1 << chromaShift) - 1) >> chromaShift;
        var chromaH = (srcH + (1 << chromaShift) - 1) >> chromaShift;
        return dispW <= chromaW && dispH <= chromaH ? Bilinear : Lanczos;
    }

    /// <summary>1 for 4:2:0 pixel formats (nv12, p010, yuv420p...), otherwise 0.</summary>
    public static int ChromaShift(string? pixelFormat)
    {
        if (string.IsNullOrEmpty(pixelFormat))
        {
            return 0;
        }

        var f = pixelFormat.ToLowerInvariant();
        return f.Contains("420") || f is "nv12" or "nv21" or "p010" or "p016" ? 1 : 0;
    }

    /// <summary>A5-b fix: with hwdec (e.g. d3d11va) pixelformat is the hw format ("d3d11"); the real surface format is hw-pixelformat.</summary>
    public static string? EffectivePixelFormat(string? hwPixelFormat, string? pixelFormat) =>
        string.IsNullOrEmpty(hwPixelFormat) ? pixelFormat : hwPixelFormat;

    /// <summary>Chroma plane size (ceil), same rounding as Decide.</summary>
    public static int ChromaSize(int size, int chromaShift) =>
        chromaShift <= 0 ? size : (size + (1 << chromaShift) - 1) >> chromaShift;
}
