using DesktopMediaPlayer.Engine.Mpv;
using Xunit;

namespace DesktopMediaPlayer.Playback.Tests;

public sealed class CscalePolicyTests
{
    [Fact]
    public void Downscaled_BelowChroma_Bilinear()
    {
        Assert.Equal(CscalePolicy.Bilinear, CscalePolicy.Decide(353, 628, 3840, 2160, 1));
    }

    [Fact]
    public void SameSize_Lanczos()
    {
        Assert.Equal(CscalePolicy.Lanczos, CscalePolicy.Decide(1920, 1080, 1920, 1080, 1));
    }

    [Fact]
    public void Upscaled_Lanczos()
    {
        Assert.Equal(CscalePolicy.Lanczos, CscalePolicy.Decide(2560, 1440, 1920, 1080, 1));
    }

    [Fact]
    public void Boundary_EqualToChroma_Bilinear_OnePixelOver_Lanczos()
    {
        Assert.Equal(CscalePolicy.Bilinear, CscalePolicy.Decide(1920, 1080, 3840, 2160, 1));
        Assert.Equal(CscalePolicy.Lanczos, CscalePolicy.Decide(1921, 1080, 3840, 2160, 1));
        Assert.Equal(CscalePolicy.Lanczos, CscalePolicy.Decide(1215, 2160, 3840, 2160, 1));
    }

    [Fact]
    public void NoSubsampling_444OrRgb_Lanczos()
    {
        Assert.Equal(CscalePolicy.Lanczos, CscalePolicy.Decide(320, 180, 3840, 2160, 0));
        Assert.Equal(0, CscalePolicy.ChromaShift("yuv444p"));
        Assert.Equal(0, CscalePolicy.ChromaShift("rgb24"));
        Assert.Equal(0, CscalePolicy.ChromaShift(null));
    }

    [Fact]
    public void ZeroSize_Lanczos()
    {
        Assert.Equal(CscalePolicy.Lanczos, CscalePolicy.Decide(0, 0, 3840, 2160, 1));
        Assert.Equal(CscalePolicy.Lanczos, CscalePolicy.Decide(320, 180, 0, 0, 1));
    }

    [Fact]
    public void ChromaShift_420Formats_One()
    {
        Assert.Equal(1, CscalePolicy.ChromaShift("nv12"));
        Assert.Equal(1, CscalePolicy.ChromaShift("p010"));
        Assert.Equal(1, CscalePolicy.ChromaShift("yuv420p10"));
    }

    [Theory]
    [InlineData("nv12", "d3d11", 1)]
    [InlineData("p010", "d3d11", 1)]
    [InlineData(null, "yuv420p", 1)]
    [InlineData("", "yuv420p", 1)]
    [InlineData(null, "d3d11", 0)]
    public void EffectivePixelFormat_PrefersHw(string? hw, string? sw, int shift) =>
        Assert.Equal(shift, CscalePolicy.ChromaShift(CscalePolicy.EffectivePixelFormat(hw, sw)));

    [Fact]
    public void BothFormatsEmpty_Lanczos() =>
        Assert.Equal(CscalePolicy.Lanczos,
            CscalePolicy.Decide(1280, 720, 3840, 2160, CscalePolicy.ChromaShift(CscalePolicy.EffectivePixelFormat(null, null))));

    [Fact]
    public void Hw4kSmallWindow_Bilinear() =>
        Assert.Equal(CscalePolicy.Bilinear,
            CscalePolicy.Decide(1280, 720, 3840, 2160, CscalePolicy.ChromaShift(CscalePolicy.EffectivePixelFormat("nv12", "d3d11"))));

    [Fact]
    public void ChromaSize_Ceil() => Assert.Equal(541, CscalePolicy.ChromaSize(1081, 1));
}
