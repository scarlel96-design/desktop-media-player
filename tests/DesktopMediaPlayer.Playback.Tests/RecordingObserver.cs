using DesktopMediaPlayer.Contracts;

namespace DesktopMediaPlayer.Playback.Tests;

internal sealed class RecordingObserver : IPlaybackObserver
{
    public int FirstFrameCount { get; private set; }
    public List<PlaybackState> States { get; } = new();
    public List<(string Code, string Message, bool Recoverable)> Errors { get; } = new();
    public List<(bool Active, string Reason)> HwAccel { get; } = new();

    public void OnFirstFrame() => FirstFrameCount++;
    public void OnStateChanged(PlaybackState state) => States.Add(state);
    public void OnError(string code, string message, bool recoverable) =>
        Errors.Add((code, message, recoverable));
    public void OnHardwareAccelChanged(bool active, string reason) =>
        HwAccel.Add((active, reason));
}
