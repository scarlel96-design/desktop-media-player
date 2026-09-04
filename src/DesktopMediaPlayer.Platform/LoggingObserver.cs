using DesktopMediaPlayer.Contracts;

namespace DesktopMediaPlayer.Platform;

/// <summary>Forwards observer events into <see cref="SpikeLogger"/>.</summary>
public sealed class LoggingObserver : IPlaybackObserver
{
    private readonly SpikeLogger _logger;

    public LoggingObserver(SpikeLogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void OnFirstFrame() => _logger.FirstFrame();

    public void OnStateChanged(PlaybackState state) => _logger.State(state.ToString());

    public void OnError(string code, string message, bool recoverable) =>
        _logger.Error(code, message, recoverable);

    public void OnHardwareAccelChanged(bool active, string reason)
    {
        if (active)
        {
            _logger.HwdecActive(reason);
        }
        else
        {
            _logger.HwdecFallback(reason);
        }
    }
}
