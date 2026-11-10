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
}
