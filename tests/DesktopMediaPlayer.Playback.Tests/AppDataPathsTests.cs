using System;
using System.IO;
using DesktopMediaPlayer.Engine.Mpv;
using DesktopMediaPlayer.Platform;
using DesktopMediaPlayer.Playback;
using Xunit;

namespace DesktopMediaPlayer.Playback.Tests;

public sealed class AppDataPathsTests
{
    private static string NewTemp() => Path.Combine(Path.GetTempPath(), "dmp-datadir-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void Gate_NoDataDir_PathsEqualPreChangeStrings()
    {
        var legacy = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "desktop-media-player");
        Assert.True(AppDataPaths.TryParseArgs(new[] { "movie.mkv" }, out var parsed, out _));
        Assert.Null(parsed);
        var p = new AppDataPaths(AppDataPaths.DefaultRoot());
        Assert.Equal(legacy, p.Root);
        Assert.Equal(legacy, p.Settings);
        Assert.Equal(legacy, p.Logs);
        Assert.Equal(Path.Combine(legacy, "resume.json"), p.Resume);
    }

    [Fact]
    public void Override_UsesGivenRoot()
    {
        var dir = NewTemp();
        Directory.CreateDirectory(dir);
        try
        {
            Assert.True(AppDataPaths.TryParseArgs(new[] { "--data-dir", dir }, out var p, out var rest));
            Assert.Equal(Path.GetFullPath(dir), p!.Root);
            Assert.Equal(Path.Combine(p.Root, "resume.json"), p.Resume);
            Assert.Empty(rest);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Theory]
    [InlineData("relative/dir")]
    [InlineData("")]
    [InlineData("--other")]
    public void InvalidPath_Fails(string value)
    {
        Assert.False(AppDataPaths.TryParseArgs(new[] { "--data-dir", value }, out var p, out _));
        Assert.Null(p);
    }

    [Fact]
    public void MissingValueOrFileTarget_Fails()
    {
        Assert.False(AppDataPaths.TryParseArgs(new[] { "--data-dir" }, out _, out _));
        var file = Path.GetTempFileName();
        try
        {
            Assert.False(AppDataPaths.TryParseArgs(new[] { "--data-dir", file }, out _, out _));
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Fact]
    public void InvalidPath_CreatesNothing_UnderFakeDefaultRootOrTarget()
    {
        var fakeRoot = NewTemp();
        Directory.CreateDirectory(fakeRoot);
        var blocker = Path.Combine(fakeRoot, "blocker");
        File.WriteAllText(blocker, "x");
        try
        {
            var fake = new AppDataPaths(Path.Combine(fakeRoot, "default"));
            Assert.False(AppDataPaths.TryParseArgs(new[] { "--data-dir", Path.Combine(blocker, "sub") }, out _, out _));
            Assert.False(AppDataPaths.TryParseArgs(new[] { "--data-dir", "dmp-relative-" + Guid.NewGuid().ToString("N") }, out _, out _));
            Assert.Equal(new[] { blocker }, Directory.GetFileSystemEntries(fakeRoot));
            Assert.False(Directory.Exists(fake.Root));
        }
        finally
        {
            Directory.Delete(fakeRoot, true);
        }
    }

    [Fact]
    public void MaskText_ReplacesDataRootAndUserProfile_CaseInsensitive()
    {
        var user = Path.Combine(Path.GetTempPath(), "FakeUser");
        var p = new AppDataPaths(Path.Combine(user, "AppData", "dmp"));
        var message = $"Could not find file '{p.Root.ToUpperInvariant()}{Path.DirectorySeparatorChar}a.json' near '{user}{Path.DirectorySeparatorChar}Videos'";
        var masked = PathMask.MaskText(message, p, user);
        Assert.Contains("<data>", masked);
        Assert.Contains("<user>", masked);
        Assert.DoesNotContain("FakeUser", masked, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void NonexistentPath_IsCreated()
    {
        var dir = Path.Combine(NewTemp(), "nested");
        try
        {
            Assert.True(AppDataPaths.TryParseArgs(new[] { "--data-dir", dir }, out var p, out _));
            Assert.True(Directory.Exists(p!.Root));
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(dir)!, true);
        }
    }

    [Fact]
    public void Mask_NeverWritesAbsolutePath()
    {
        var root = NewTemp();
        var p = new AppDataPaths(root);
        Assert.Equal("<data>/spike.log", PathMask.Mask(Path.Combine(root, "spike.log"), p));
        var media = Path.Combine(Path.GetTempPath(), "secret folder", "movie.mkv");
        var masked = PathMask.Mask(media, p);
        Assert.Matches("^f-[0-9a-f]{8}\\.mkv$", masked);
        Assert.DoesNotContain("secret", masked);
        Assert.DoesNotContain("movie", masked);
    }

    [Fact]
    public void CoexistsWithCliFileArgs_DataDirValueNotMedia()
    {
        var dir = NewTemp();
        var media = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "dmp-args", "a.mkv"));
        try
        {
            Assert.True(AppDataPaths.TryParseArgs(new[] { "--data-dir", dir, media }, out _, out var rest));
            Assert.Equal(new[] { media }, rest);
            Assert.Equal(media, StartupArgs.FirstMediaPath(rest, x => x == media || x == Path.GetFullPath(dir)));
            Assert.Null(StartupArgs.FirstMediaPath(rest.GetRange(0, 0), _ => true));
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void MpvCacheOptions_SetUnderCacheDir()
    {
        Assert.Empty(MpvPlaybackEngine.CacheOptions(null));
        var cache = new AppDataPaths(NewTemp()).Cache;
        var opts = MpvPlaybackEngine.CacheOptions(cache);
        Assert.Contains(opts, o => o.Key == "gpu-shader-cache-dir" && o.Value.StartsWith(cache));
        Assert.Contains(opts, o => o.Key == "icc-cache-dir" && o.Value.StartsWith(cache));
    }
}
