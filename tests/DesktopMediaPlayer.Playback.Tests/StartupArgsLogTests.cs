using DesktopMediaPlayer.Playback;
using Xunit;

namespace DesktopMediaPlayer.Playback.Tests;

public class StartupArgsLogTests
{
    [Fact]
    public void Opened_HasNoReason() =>
        Assert.Equal("args.count=1 result=opened ext=.mkv",
            StartupArgsLog.Format(1, StartupOpenOutcome.Opened, null, ".mkv"));

    [Fact]
    public void OpenFailed_CarriesReason() =>
        Assert.Equal("args.count=1 result=open_failed reason=not_found ext=.mp4",
            StartupArgsLog.Format(1, StartupOpenOutcome.OpenFailed, "not_found", ".mp4"));

    [Fact]
    public void OpenFailed_DefaultsToException() =>
        Assert.Equal("args.count=1 result=open_failed reason=exception ext=.mp4",
            StartupArgsLog.Format(1, StartupOpenOutcome.OpenFailed, null, ".mp4"));

    [Fact]
    public void NoArgs_ZeroCount()
    {
        Assert.Equal(StartupOpenOutcome.NoArgs, StartupArgsLog.NoPathOutcome(0));
        Assert.Equal("args.count=0 result=none reason=no_args ext=",
            StartupArgsLog.Format(0, StartupOpenOutcome.NoArgs, null, null));
    }

    [Fact]
    public void NoExistingFile_WhenArgsPresent()
    {
        Assert.Equal(StartupOpenOutcome.NoExistingFile, StartupArgsLog.NoPathOutcome(2));
        Assert.Equal("args.count=2 result=none reason=no_existing_file ext=",
            StartupArgsLog.Format(2, StartupOpenOutcome.NoExistingFile, null, null));
    }

    [Fact]
    public void ProbeFailed_IsSkipped() =>
        Assert.Equal("args.count=1 result=skipped reason=probe_failed ext=.avi",
            StartupArgsLog.Format(1, StartupOpenOutcome.ProbeFailed, null, ".avi"));
}
