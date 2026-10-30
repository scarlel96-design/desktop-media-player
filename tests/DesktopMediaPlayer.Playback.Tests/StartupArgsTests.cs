using System;
using System.Collections.Generic;
using System.IO;
using DesktopMediaPlayer.Playback;
using Xunit;

namespace DesktopMediaPlayer.Playback.Tests;

public sealed class StartupArgsTests
{
    // Relative root: an absolute Linux path ("/tmp/...") would be treated as a "/" switch by design.
    private const string Root = "dmp-args";

    private static string Full(string relative) => Path.GetFullPath(relative);

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
        Assert.Null(StartupArgs.FirstMediaPath(new[] { "-v", "/safe", "--log" }, _ => true));
    }

    [Fact]
    public void QuotedPathWithSpaces_IsTrimmedAndReturned()
    {
        var path = Path.Combine(Root, "my movie.mkv");
        var result = StartupArgs.FirstMediaPath(new[] { "\"" + path + "\"" }, Existing(Full(path)));
        Assert.Equal(Full(path), result);
    }

    [Fact]
    public void MissingThenExisting_ReturnsExisting()
    {
        var missing = Path.Combine(Root, "missing.mp4");
        var present = Path.Combine(Root, "present.mp4");
        var result = StartupArgs.FirstMediaPath(new[] { missing, present }, Existing(Full(present)));
        Assert.Equal(Full(present), result);
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
        var result = StartupArgs.FirstMediaPath(new[] { "-x", a, b }, Existing(Full(a), Full(b)));
        Assert.Equal(Full(a), result);
    }
}
