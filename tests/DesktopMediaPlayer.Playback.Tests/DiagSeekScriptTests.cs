#if DMP_DIAG
using DesktopMediaPlayer.Engine.Mpv;
using DesktopMediaPlayer.Playback;
using Xunit;

namespace DesktopMediaPlayer.Playback.Tests;

public class DiagSeekScriptTests
{
    [Fact]
    public void Parse_ValidScript_SkipsCommentsAndBlanks()
    {
        var r = DiagSeekScript.Parse(new[] { "# warmup", "", "1000 abs 12.5", "500 rel -5", "0 pause", "250 PLAY" });
        Assert.Null(r.RejectedLine);
        Assert.Equal(4, r.Steps.Count);
        Assert.Equal(new DiagSeekScript.Step(1000, DiagSeekScript.Command.Abs, 12.5), r.Steps[0]);
        Assert.Equal(new DiagSeekScript.Step(500, DiagSeekScript.Command.Rel, -5), r.Steps[1]);
        Assert.Equal(DiagSeekScript.Command.Pause, r.Steps[2].Cmd);
        Assert.Equal(DiagSeekScript.Command.Play, r.Steps[3].Cmd);
    }

    [Theory]
    [InlineData("abc abs 1")]
    [InlineData("-5 abs 1")]
    [InlineData("100 jump 1")]
    [InlineData("100 abs")]
    [InlineData("100 abs -3")]
    [InlineData("100 rel x")]
    [InlineData("100 pause 1")]
    [InlineData("100 abs 1 extra")]
    [InlineData("100 abs NaN")]
    public void Parse_MalformedLine_RejectsWholeScriptWithLineNumber(string bad)
    {
        var r = DiagSeekScript.Parse(new[] { "# c", "100 abs 1", bad });
        Assert.Equal(3, r.RejectedLine);
        Assert.Empty(r.Steps);
    }

    [Fact]
    public void Parse_TooManyLines_Rejected()
    {
        var lines = Enumerable.Repeat("10 play", DiagSeekScript.MaxLines + 1).ToArray();
        var r = DiagSeekScript.Parse(lines);
        Assert.Equal(DiagSeekScript.MaxLines + 1, r.RejectedLine);
        Assert.Empty(r.Steps);
        Assert.Null(DiagSeekScript.Parse(lines.Take(DiagSeekScript.MaxLines).ToArray()).RejectedLine);
    }

    [Fact]
    public void ExtractPath_RemovesSwitchAndValue_KeepsMediaArgs()
    {
        var path = DiagSeekScript.ExtractPath(new[] { "--diag-seek-script", "\"C:\\s.txt\"", "movie.mkv" }, out var rest);
        Assert.Equal("C:\\s.txt", path);
        Assert.Equal(new[] { "movie.mkv" }, rest);
        Assert.Null(DiagSeekScript.ExtractPath(new[] { "movie.mkv", "--diag-seek-script" }, out var rest2));
        Assert.Equal(new[] { "movie.mkv" }, rest2);
    }

    [Theory]
    [InlineData(null, "none")]
    [InlineData("", "none")]
    [InlineData("{\"fresh\":[]}", "json")]
    [InlineData("fresh: ...", "other")]
    public void VoPassesFormat_ClassifiesStringForm(string? input, string expected) =>
        Assert.Equal(expected, DiagMpvOptions.VoPassesFormat(input));
}
#endif
