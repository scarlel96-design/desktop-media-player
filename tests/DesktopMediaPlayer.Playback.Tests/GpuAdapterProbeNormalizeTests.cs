using DesktopMediaPlayer.Platform;
using Xunit;

namespace DesktopMediaPlayer.Playback.Tests;

public sealed class GpuAdapterProbeNormalizeTests
{
    [Fact]
    public void HighPerformanceConstant_IsTwo() => Assert.Equal(2, DxgiGpuAdapterProbe.DxgiGpuPreferenceHighPerformance);

    [Fact]
    public void Normalize_DropsBasicRenderAndDuplicates_KeepsOrder()
    {
        const string Nv = "NVIDIA GeForce RTX 4060 Laptop GPU", Igpu = "Intel(R) UHD Graphics";
        var result = DxgiGpuAdapterProbe.NormalizeHighPerformanceOrder(new[] { Nv, "Microsoft Basic Render Driver", Igpu, Nv, null, "" });
        Assert.Equal(new[] { Nv, Igpu }, result);
    }
}
