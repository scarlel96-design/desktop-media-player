namespace DesktopMediaPlayer.Platform;

/// <summary>
/// Ordered hardware decode attempts: D3D11VA → D3D11VA-copy → software.
/// </summary>
public sealed class HardwareAccelPolicy
{
    public static readonly string[] HwdecCandidates =
    {
        "d3d11va",
        "d3d11va-copy",
        "no"
    };

    public static readonly string[] VoCandidates =
    {
        "gpu-next",
        "gpu"
    };

    public const string GpuContext = "d3d11";

    private int _hwdecIndex;
    private readonly SpikeLogger _logger;

    public HardwareAccelPolicy(SpikeLogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string CurrentHwdec => HwdecCandidates[Math.Clamp(_hwdecIndex, 0, HwdecCandidates.Length - 1)];

    public bool IsSoftware => string.Equals(CurrentHwdec, "no", StringComparison.OrdinalIgnoreCase);

    public void Reset() => _hwdecIndex = 0;

    /// <summary>
    /// Advances to the next fallback. Returns false if already on software.
    /// </summary>
    public bool TryFallback(string reason)
    {
        if (_hwdecIndex >= HwdecCandidates.Length - 1)
        {
            _logger.HwdecFallback($"already_software reason={reason}");
            return false;
        }

        var from = CurrentHwdec;
        _hwdecIndex++;
        var to = CurrentHwdec;
        _logger.HwdecFallback($"from={from} to={to} reason={reason}");
        return true;
    }

    public void ReportActive(string hwdecCurrent)
    {
        var active = !string.IsNullOrWhiteSpace(hwdecCurrent)
                     && !string.Equals(hwdecCurrent, "no", StringComparison.OrdinalIgnoreCase)
                     && !string.Equals(hwdecCurrent, "none", StringComparison.OrdinalIgnoreCase);
        if (active)
        {
            _logger.HwdecActive($"hwdec-current={hwdecCurrent}");
        }
        else
        {
            _logger.HwdecFallback($"hwdec-current={hwdecCurrent ?? "(null)"} (software/inactive)");
        }
    }
}
