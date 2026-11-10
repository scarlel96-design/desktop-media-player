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
}
