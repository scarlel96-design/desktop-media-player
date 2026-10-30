using System;
using System.Collections.Generic;
using System.IO;
using DesktopMediaPlayer.Playback;
using Xunit;

namespace DesktopMediaPlayer.Playback.Tests;

public sealed class StartupArgsTests
{
    private static readonly string Root = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "dmp-args"));

    private static Func<string, bool> Existing(params string[] fullPaths)
    {
        var set = new HashSet<string>(fullPaths, StringComparer.OrdinalIgnoreCase);
        return set.Contains;
    }

    [Fact]
    public void EmptyArgs_ReturnsNull()
    {
        Assert.Null(StartupArgs.FirstMediaPath(Array.Empty<string>(), _ => true));
    }

    [Fact]
    public void SwitchesOnly_ReturnsNull()
    {
        Assert.Null(StartupArgs.FirstMediaPath(new[] { "-v", "/safe", "--log" }, _ => false));
    }

    [Fact]
    public void QuotedPathWithSpaces_IsTrimmedAndReturned()
    {
        var path = Path.Combine(Root, "my movie.mkv");
        var result = StartupArgs.FirstMediaPath(new[] { "\"" + path + "\"" }, Existing(path));
        Assert.Equal(path, result);
    }

    [Fact]
    public void MissingThenExisting_ReturnsExisting()
    {
        var missing = Path.Combine(Root, "missing.mp4");
        var present = Path.Combine(Root, "present.mp4");
        var result = StartupArgs.FirstMediaPath(new[] { missing, present }, Existing(present));
        Assert.Equal(present, result);
    }

    [Fact]
    public void RelativePath_IsMadeAbsolute()
    {
        var expected = Path.GetFullPath("clip.mp4");
        var result = StartupArgs.FirstMediaPath(new[] { "clip.mp4" }, Existing(expected));
        Assert.Equal(expected, result);
    }

    [Fact]
    public void MultipleExisting_ReturnsFirst()
    {
        var a = Path.Combine(Root, "a.mp4");
        var b = Path.Combine(Root, "b.mp4");
        var result = StartupArgs.FirstMediaPath(new[] { "-x", a, b }, Existing(a, b));
        Assert.Equal(a, result);
    }
}
