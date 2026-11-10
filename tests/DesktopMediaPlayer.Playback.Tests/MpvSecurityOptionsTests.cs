using System.Collections.Generic;
using DesktopMediaPlayer.Engine.Mpv;
using Xunit;

namespace DesktopMediaPlayer.Playback.Tests;

public sealed class MpvSecurityOptionsTests
{
    [Fact]
    public void HardeningOptions_DisableYtdlAndScripts()
    {
        Assert.Equal(
            new[] { new KeyValuePair<string, string>("ytdl", "no"), new KeyValuePair<string, string>("load-scripts", "no") },
            MpvPlaybackEngine.HardeningOptions);
    }

    [Fact]
    public void HwdecCodecs_ExactDefaultListWithoutHevc()
    {
        Assert.Equal("h264,vc1,vp8,vp9,av1,prores", MpvPlaybackEngine.HwdecCodecs);
        Assert.DoesNotContain("hevc", MpvPlaybackEngine.HwdecCodecs.Split(','));
    }
}
