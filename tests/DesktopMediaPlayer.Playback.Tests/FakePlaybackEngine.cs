using DesktopMediaPlayer.Contracts;

namespace DesktopMediaPlayer.Playback.Tests;

internal sealed class FakePlaybackEngine : IPlaybackEngine
{
    private PlaybackState _state = PlaybackState.Idle;
    public int LastVolume { get; private set; } = 100;
    public string? LastOpenedPath { get; private set; }
    public double LastSeekSeconds { get; private set; }
    public bool Disposed { get; private set; }
    public List<string> Calls { get; } = new();

    public void Open(string path)
    {
        Calls.Add($"Open:{path}");
        LastOpenedPath = path;
        _state = PlaybackState.Opening;
    }

    public void Close()
    {
        Calls.Add("Close");
        _state = PlaybackState.Idle;
    }

    public void Play()
    {
        Calls.Add("Play");
        _state = PlaybackState.Playing;
    }

    public void Pause()
    {
        Calls.Add("Pause");
        _state = PlaybackState.Paused;
    }

    public void Stop()
    {
        Calls.Add("Stop");
        _state = PlaybackState.Stopped;
    }

    public void Seek(double seconds)
    {
        Calls.Add($"Seek:{seconds}");
        LastSeekSeconds = seconds;
    }

    public void SetVolume(int volume)
    {
        Calls.Add($"SetVolume:{volume}");
        LastVolume = volume;
    }

    public PlaybackState GetState() => _state;

    public void Dispose()
    {
        Disposed = true;
        Calls.Add("Dispose");
    }
}
