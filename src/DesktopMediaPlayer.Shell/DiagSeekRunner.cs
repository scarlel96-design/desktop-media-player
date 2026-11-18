#if DMP_DIAG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Threading;
using DesktopMediaPlayer.Contracts;
using DesktopMediaPlayer.Playback;
using DesktopMediaPlayer.Platform;

namespace DesktopMediaPlayer.Shell;

/// <summary>DMP_DIAG only: replays a parsed seek script through the public PlaybackFacade API (never mpv directly).</summary>
internal sealed class DiagSeekRunner
{
    private readonly IReadOnlyList<DiagSeekScript.Step> _steps;
    private readonly PlaybackFacade _facade;
    private readonly SpikeLogger _logger;
    private readonly DispatcherTimer _timer = new();
    private readonly Stopwatch _clock = new();
    private int _next;

    public DiagSeekRunner(IReadOnlyList<DiagSeekScript.Step> steps, PlaybackFacade facade, SpikeLogger logger)
    {
        _steps = steps;
        _facade = facade;
        _logger = logger;
        _timer.Tick += (_, _) => IssueNext();
    }

    public bool Started { get; private set; }

    /// <summary>Starts once; wait_ms of each step counts from the previous command's issue time.</summary>
    public void Start()
    {
        if (Started)
        {
            return;
        }

        Started = true;
        _clock.Start();
        _logger.Log("info", "diag_seek", $"start steps={_steps.Count}");
        Schedule();
    }

    public void Stop() => _timer.Stop();

    private void Schedule()
    {
        _timer.Stop();
        if (_next >= _steps.Count)
        {
            _logger.Log("info", "diag_seek", "done");
            return;
        }

        _timer.Interval = TimeSpan.FromMilliseconds(Math.Max(1, _steps[_next].WaitMs));
        _timer.Start();
    }

    private void IssueNext()
    {
        _timer.Stop();
        var i = _next++;
        var step = _steps[i];
        var value = step.Value.ToString("0.###", CultureInfo.InvariantCulture);
        _logger.Log("info", "diag_seek", $"step={i + 1} cmd={step.Cmd.ToString().ToLowerInvariant()} value={value} issued_ms={_clock.ElapsedMilliseconds}");
        try
        {
            switch (step.Cmd)
            {
                case DiagSeekScript.Command.Abs:
                    _facade.Seek(step.Value);
                    break;
                case DiagSeekScript.Command.Rel:
                    _facade.SeekRelative(step.Value, SeekPrecision.Keyframe);
                    break;
                case DiagSeekScript.Command.Pause:
                    _facade.Pause();
                    break;
                case DiagSeekScript.Command.Play:
                    _facade.Play();
                    break;
            }
        }
        catch (Exception ex) when (ex is ObjectDisposedException or InvalidOperationException)
        {
            _logger.Log("warn", "diag_seek", $"step={i + 1} error={ex.GetType().Name}");
            return;
        }

        Schedule();
    }
}
#endif
