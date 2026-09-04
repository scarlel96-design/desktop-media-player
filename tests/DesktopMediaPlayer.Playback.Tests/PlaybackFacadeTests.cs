using DesktopMediaPlayer.Contracts;
using DesktopMediaPlayer.Playback;
using Xunit;

namespace DesktopMediaPlayer.Playback.Tests;

public sealed class PlaybackFacadeTests
{
    [Theory]
    [InlineData(-10, 0)]
    [InlineData(0, 0)]
    [InlineData(50, 50)]
    [InlineData(100, 100)]
    [InlineData(250, 100)]
    public void SetVolume_ClampsToZeroThroughOneHundred(int input, int expected)
    {
        var fake = new FakePlaybackEngine();
        using var facade = new PlaybackFacade(fake);

        facade.SetVolume(input);

        Assert.Equal(expected, fake.LastVolume);
        Assert.Contains($"SetVolume:{expected}", fake.Calls);
    }

    [Fact]
    public void ObserverFanOut_DeliversToAllObservers()
    {
        var fake = new FakePlaybackEngine();
        var a = new RecordingObserver();
        var b = new RecordingObserver();
        using var facade = new PlaybackFacade(fake, a, b);

        facade.OnFirstFrame();
        facade.OnStateChanged(PlaybackState.Playing);
        facade.OnError("e", "m", true);
        facade.OnHardwareAccelChanged(true, "d3d11va");

        Assert.Equal(1, a.FirstFrameCount);
        Assert.Equal(1, b.FirstFrameCount);
        Assert.Equal(new[] { PlaybackState.Playing }, a.States);
        Assert.Equal(new[] { PlaybackState.Playing }, b.States);
        Assert.Single(a.Errors);
        Assert.Single(b.Errors);
        Assert.True(a.HwAccel[0].Active);
        Assert.True(b.HwAccel[0].Active);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Open_NullOrEmptyPath_ReportsError_DoesNotCallInner(string? path)
    {
        var fake = new FakePlaybackEngine();
        var observer = new RecordingObserver();
        using var facade = new PlaybackFacade(fake, observer);

        facade.Open(path!);

        Assert.Null(fake.LastOpenedPath);
        Assert.DoesNotContain(fake.Calls, c => c.StartsWith("Open:", StringComparison.Ordinal));
        Assert.Single(observer.Errors);
        Assert.Equal("invalid_path", observer.Errors[0].Code);
        Assert.True(observer.Errors[0].Recoverable);
    }

    [Fact]
    public void Open_ValidPath_DelegatesToInner()
    {
        var fake = new FakePlaybackEngine();
        using var facade = new PlaybackFacade(fake);

        facade.Open(@"C:\media\sample.mp4");

        Assert.Equal(@"C:\media\sample.mp4", fake.LastOpenedPath);
        Assert.Equal(PlaybackState.Opening, facade.GetState());
    }

    [Fact]
    public void AddObserver_ReceivesSubsequentEvents()
    {
        var fake = new FakePlaybackEngine();
        using var facade = new PlaybackFacade(fake);
        var observer = new RecordingObserver();
        facade.AddObserver(observer);

        facade.OnStateChanged(PlaybackState.Paused);

        Assert.Equal(new[] { PlaybackState.Paused }, observer.States);
    }

    [Fact]
    public void TryProbe_DelegatesToInnerHealthCheck()
    {
        var fake = new FakePlaybackEngine { ProbeResult = true, ProbeDetail = "lib-ok" };
        using var facade = new PlaybackFacade(fake);

        var ok = facade.TryProbe(out var detail);

        Assert.True(ok);
        Assert.Equal("lib-ok", detail);
    }

    [Fact]
    public void SetMute_And_FrameStep_DelegateToInner()
    {
        var fake = new FakePlaybackEngine();
        using var facade = new PlaybackFacade(fake);

        facade.SetMute(true);
        facade.FrameStep(1);

        Assert.True(facade.GetMute());
        Assert.Contains("SetMute:True", fake.Calls);
        Assert.Contains("FrameStep:1", fake.Calls);
    }

    [Fact]
    public void LoadExternalSubtitle_EmptyPath_ReportsError()
    {
        var fake = new FakePlaybackEngine();
        var observer = new RecordingObserver();
        using var facade = new PlaybackFacade(fake, observer);

        facade.LoadExternalSubtitle("  ");

        Assert.DoesNotContain(fake.Calls, c => c.StartsWith("LoadExternalSubtitle:", StringComparison.Ordinal));
        Assert.Single(observer.Errors);
        Assert.Equal("invalid_path", observer.Errors[0].Code);
    }

    [Fact]
    public void MinimalPlaylist_PlayAt_OpensViaEngine()
    {
        var fake = new FakePlaybackEngine();
        using var facade = new PlaybackFacade(fake);
        var playlist = new MinimalPlaylistService(facade);
        playlist.Add("C:/media/a.mp4");
        playlist.Add("C:/media/b.mp4");

        playlist.PlayAt(1);

        Assert.Equal(1, playlist.CurrentIndex);
        Assert.Equal("C:/media/b.mp4", fake.LastOpenedPath);
    }

    [Fact]
    public void PlayNext_AtEnd_StopsWithoutWrap()
    {
        var fake = new FakePlaybackEngine();
        using var facade = new PlaybackFacade(fake);
        var playlist = new MinimalPlaylistService(facade);
        playlist.Add("C:/a.mp4");
        playlist.Add("C:/b.mp4");
        playlist.PlayAt(1);

        playlist.PlayNext();

        Assert.False(playlist.CanPlayNext);
        Assert.Contains("Stop", fake.Calls);
        Assert.Equal("C:/b.mp4", fake.LastOpenedPath);
    }

    [Fact]
    public void PlayPrevious_AtStart_StopsWithoutWrap()
    {
        var fake = new FakePlaybackEngine();
        using var facade = new PlaybackFacade(fake);
        var playlist = new MinimalPlaylistService(facade);
        playlist.Add("C:/a.mp4");
        playlist.PlayAt(0);

        playlist.PlayPrevious();

        Assert.False(playlist.CanPlayPrevious);
        Assert.Contains("Stop", fake.Calls);
    }
}
