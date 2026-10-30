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
}
