using DesktopMediaPlayer.Contracts;

namespace DesktopMediaPlayer.Playback.Tests;

internal sealed class FakePlaybackEngine : IPlaybackEngine, INativeRuntimeProbe
{
    private PlaybackState _state = PlaybackState.Idle;
    private bool _mute;
    private double _position;
    private double _duration = 120;
    private readonly List<MediaTrackInfo> _tracks = new();

    public int LastVolume { get; private set; } = 100;
    public string? LastOpenedPath { get; private set; }
    public double LastSeekSeconds { get; private set; }
    public bool Disposed { get; private set; }
    public bool ProbeResult { get; set; } = true;
    public string? ProbeDetail { get; set; } = "fake-ok";
    public List<string> Calls { get; } = new();

    public void Open(string path)
    {
        Calls.Add($"Open:{path}");
        LastOpenedPath = path;
        _state = PlaybackState.Opening;
        _position = 0;
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
        _position = 0;
    }

    public void Seek(double seconds)
    {
        Calls.Add($"Seek:{seconds}");
        LastSeekSeconds = seconds;
        _position = seconds;
    }

    public void SetVolume(int volume)
    {
        Calls.Add($"SetVolume:{volume}");
        LastVolume = volume;
    }

    public PlaybackState GetState() => _state;

    public void FrameStep(int steps)
    {
        Calls.Add($"FrameStep:{steps}");
        _position = Math.Max(0, _position + (steps * (1.0 / 30.0)));
    }

    public void SetMute(bool mute)
    {
        Calls.Add($"SetMute:{mute}");
        _mute = mute;
    }

    public bool GetMute() => _mute;

    public double GetPosition() => _position;

    public double GetDuration() => _duration;

    public IReadOnlyList<MediaTrackInfo> ListTracks() => _tracks.ToArray();

    public void SelectTrack(MediaTrackKind kind, int id)
    {
        Calls.Add($"SelectTrack:{kind}:{id}");
    }

    public void LoadExternalSubtitle(string path)
    {
        Calls.Add($"LoadExternalSubtitle:{path}");
    }

    public void SetSubtitleOffset(double seconds)
    {
        Calls.Add($"SetSubtitleOffset:{seconds}");
    }

    public void Screenshot(string path)
    {
        Calls.Add($"Screenshot:{path}");
    }

    public MediaInfoBasics GetMediaInfo() =>
        new(LastOpenedPath, "fake", "mp4", "h264", "aac", 1920, 1080, _duration);

    public bool TryProbe(out string? detail)
    {
        detail = ProbeDetail;
        return ProbeResult;
    }

    public void Dispose()
    {
        Disposed = true;
        Calls.Add("Dispose");
    }
}
