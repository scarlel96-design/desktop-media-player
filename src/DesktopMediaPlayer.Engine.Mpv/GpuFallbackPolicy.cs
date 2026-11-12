using DesktopMediaPlayer.Contracts;

namespace DesktopMediaPlayer.Engine.Mpv;

/// <summary>A5-a: Vo = end_file error -15 (retryable); VoTimeout = vo-configured not true within 3s (log only); Media = other end_file errors.</summary>
public enum GpuFailureStage
{
    None,
    Vo,
    VoTimeout,
    Media
}

/// <summary>A5-a: pure one-shot fallback rule for a forced d3d11-adapter.</summary>
public static class GpuFallbackPolicy
{
    public const int MpvErrorVoInitFailed = -15; // MPV_ERROR_VO_INIT_FAILED
    public static readonly TimeSpan VoConfiguredTimeout = TimeSpan.FromSeconds(3);

    public static bool ShouldRetry(GpuPreference? pref, GpuFailureStage stage, bool alreadyRetried, bool firstFrameRaised, bool diagOverride) =>
        pref == GpuPreference.HighPerformance && stage == GpuFailureStage.Vo && !alreadyRetried && !firstFrameRaised && !diagOverride;

    public static GpuFailureStage StageForEndFileError(int error) =>
        error == MpvErrorVoInitFailed ? GpuFailureStage.Vo : GpuFailureStage.Media;

    /// <summary>A vo-configured timeout only counts for the load generation that armed it.</summary>
    public static bool IsTimeoutCurrent(long armedGeneration, long currentGeneration) => armedGeneration == currentGeneration;
}
