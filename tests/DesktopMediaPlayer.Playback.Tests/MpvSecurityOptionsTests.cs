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
    public void SecurityOptionFailure_WarnsOnlyOnNegativeRc()
    {
        Assert.Equal("name=ytdl rc=-5 error=option not found", MpvPlaybackEngine.SecurityOptionFailure("ytdl", -5, "option not found"));
        Assert.Null(MpvPlaybackEngine.SecurityOptionFailure("hwdec-codecs", 0, ""));
    }

    [Theory]
    [InlineData("ytdl", -5, MpvPlaybackEngine.SecurityOptionResult.Absent)]
    [InlineData("load-scripts", -5, MpvPlaybackEngine.SecurityOptionResult.Failed)]
    [InlineData("hwdec-codecs", -5, MpvPlaybackEngine.SecurityOptionResult.Failed)]
    [InlineData("ytdl", -6, MpvPlaybackEngine.SecurityOptionResult.Failed)]
    [InlineData("ytdl", -7, MpvPlaybackEngine.SecurityOptionResult.Failed)]
    [InlineData("ytdl", 0, MpvPlaybackEngine.SecurityOptionResult.Ok)]
    [InlineData("load-scripts", 0, MpvPlaybackEngine.SecurityOptionResult.Ok)]
    public void ClassifySecurityOption_AbsentOnlyForToleratedNotFound(string name, int rc, MpvPlaybackEngine.SecurityOptionResult expected)
    {
        Assert.Equal(expected, MpvPlaybackEngine.ClassifySecurityOption(name, rc));
    }

    [Fact]
    public void SecurityOptionDisplay_AbsentOnlyWhenSetAbsentAndReadNull()
    {
        Assert.Equal("absent", MpvPlaybackEngine.SecurityOptionDisplay(null, absentOnSet: true));
        Assert.Equal("(n/a)", MpvPlaybackEngine.SecurityOptionDisplay(null, absentOnSet: false));
        Assert.Equal("no", MpvPlaybackEngine.SecurityOptionDisplay("no", absentOnSet: true));
    }
}
