using DesktopMediaPlayer.Contracts;
using DesktopMediaPlayer.Engine.Mpv;
using Xunit;

namespace DesktopMediaPlayer.Playback.Tests;

public sealed class GpuPolicyTests
{
    private static readonly string[] Hybrid = { "NVIDIA GeForce RTX 4060 Laptop GPU", "Intel(R) UHD Graphics" };

    [Fact]
    public void Decide_Auto_NoAdapter() =>
        Assert.Equal(new GpuAdapterDecision(null, "default"), GpuAdapterPolicy.Decide(GpuPreference.Auto, Hybrid, Hybrid[1]));

    [Fact]
    public void Decide_HighPerformance_SelectsFirst() =>
        Assert.Equal(new GpuAdapterDecision(Hybrid[0], "selected"), GpuAdapterPolicy.Decide(GpuPreference.HighPerformance, Hybrid, Hybrid[1]));

    [Fact]
    public void Decide_HighPerformance_NoAdapters() =>
        Assert.Equal(new GpuAdapterDecision(null, "no_hp_adapter"), GpuAdapterPolicy.Decide(GpuPreference.HighPerformance, Array.Empty<string>(), null));

    [Fact]
    public void Decide_HighPerformance_AlreadyDefault() =>
        Assert.Equal(new GpuAdapterDecision(null, "default"), GpuAdapterPolicy.Decide(GpuPreference.HighPerformance, Hybrid, Hybrid[0]));

    [Fact]
    public void Decide_ProbeFailed_And_InvalidPref()
    {
        Assert.Equal(new GpuAdapterDecision(null, "probe_failed"), GpuAdapterPolicy.Decide(GpuPreference.HighPerformance, Array.Empty<string>(), null, probeFailed: true));
        Assert.Equal(new GpuAdapterDecision(null, "invalid_pref"), GpuAdapterPolicy.Decide(null, Hybrid, Hybrid[1]));
    }

    [Theory]
    [InlineData(GpuPreference.HighPerformance, GpuFailureStage.Vo, false, false, true)]
    [InlineData(GpuPreference.Auto, GpuFailureStage.Vo, false, false, false)]
    [InlineData(GpuPreference.HighPerformance, GpuFailureStage.Vo, true, false, false)]
    [InlineData(GpuPreference.HighPerformance, GpuFailureStage.None, false, false, false)]
    [InlineData(GpuPreference.HighPerformance, GpuFailureStage.Vo, false, true, false)]
    public void ShouldRetry_Cases(GpuPreference pref, GpuFailureStage stage, bool retried, bool firstFrame, bool expected) =>
        Assert.Equal(expected, GpuFallbackPolicy.ShouldRetry(pref, stage, retried, firstFrame, diagOverride: false));

    [Fact]
    public void ShouldRetry_MediaEndFileError_False()
    {
        var stage = GpuFallbackPolicy.StageForEndFileError(-13); // MPV_ERROR_LOADING_FAILED
        Assert.Equal(GpuFailureStage.Media, stage);
        Assert.False(GpuFallbackPolicy.ShouldRetry(GpuPreference.HighPerformance, stage, false, false, false));
        Assert.Equal(GpuFailureStage.Vo, GpuFallbackPolicy.StageForEndFileError(-15));
    }

    [Fact]
    public void ShouldRetry_DiagOverride_False() =>
        Assert.False(GpuFallbackPolicy.ShouldRetry(GpuPreference.HighPerformance, GpuFailureStage.Vo, false, false, diagOverride: true));

    [Fact]
    public void VoTimeout_StaleGeneration_NoRetry()
    {
        Assert.True(GpuFallbackPolicy.IsTimeoutCurrent(3, 3));
        Assert.False(GpuFallbackPolicy.IsTimeoutCurrent(3, 4));
    }

    [Theory]
    [InlineData(null, GpuPreference.Auto)]
    [InlineData("", GpuPreference.Auto)]
    [InlineData("highPerformance", GpuPreference.HighPerformance)]
    [InlineData("auto", GpuPreference.Auto)]
    [InlineData("turbo", null)]
    [InlineData("7", null)]
    public void ParsePreference_InvalidIsNull(string? raw, GpuPreference? expected) =>
        Assert.Equal(expected, GpuAdapterPolicy.ParsePreference(raw));
}
