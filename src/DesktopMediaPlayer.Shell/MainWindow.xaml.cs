using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using DesktopMediaPlayer.Contracts;
using DesktopMediaPlayer.Playback;
using Microsoft.Win32;

namespace DesktopMediaPlayer.Shell;

/// <summary>
/// Phase A S1–S2: UX-001 chrome + timeline time/seek binding via Facade.
/// Never calls mpv P/Invoke. Blur OFF.
/// </summary>
public partial class MainWindow : Window, IPlaybackObserver
{
    private readonly DispatcherTimer _positionTimer;
    private PlaybackFacade? _facade;
    private IPlaylistService? _playlist;
    private bool _renderAttached;
    private bool _seekDragging;
    private double _durationSeconds;

    public MainWindow()
    {
        InitializeComponent();
        _positionTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        _positionTimer.Tick += (_, _) => PollPosition();

        Loaded += OnLoaded;
        SizeChanged += (_, _) => ResizeRenderHost();
        DpiChanged += (_, _) => ResizeRenderHost();
        SeekSlider.PreviewMouseLeftButtonDown += (_, _) => _seekDragging = true;
        SeekSlider.PreviewMouseLeftButtonUp += (_, _) => _seekDragging = false;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var app = (App)Application.Current;
        _facade = app.Facade;
        _playlist = app.Playlist;
        if (_facade is null)
        {
            ShowError("Playback facade was not composed.");
            return;
        }

        _facade.AddObserver(this);
        RefreshTransportEnabled();
        VolumeSlider.Value = 80;
        _facade.SetVolume(80);

        TryAttachRenderHost();
        ResizeRenderHost();
        UpdateTimeLabels(0, 0);

        if (!_facade.TryProbe(out var detail))
        {
            ShowError(
                "libmpv-2.dll not found. Place LGPL binaries under native/win-x64 or set DMP_LIBMPV_PATH. "
                + (detail ?? string.Empty));
        }
        else
        {
            StatusText.Text = "Ready — open a local media file.";
        }
    }

    private void TryAttachRenderHost()
    {
        if (_renderAttached || _facade?.RenderHost is null)
        {
            return;
        }

        var hwnd = VideoHost.ChildHwnd;
        if (hwnd == nint.Zero)
        {
            Dispatcher.BeginInvoke(TryAttachRenderHost, DispatcherPriority.Loaded);
            return;
        }

        _facade.RenderHost.Attach(hwnd);
        _renderAttached = true;
    }

    private void ResizeRenderHost()
    {
        if (_facade?.RenderHost is null || VideoHost.ActualWidth <= 0 || VideoHost.ActualHeight <= 0)
        {
            return;
        }

        var dpi = VisualTreeHelper.GetDpi(this);
        var width = (int)Math.Round(VideoHost.ActualWidth * dpi.DpiScaleX);
        var height = (int)Math.Round(VideoHost.ActualHeight * dpi.DpiScaleY);
        if (width > 0 && height > 0)
        {
            _facade.RenderHost.Resize(width, height);
        }
    }

    private void Open_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "Open media file",
            Filter = "Media files|*.mp4;*.mkv;*.avi;*.mov;*.webm;*.ts;*.m4v;*.wmv|All files|*.*"
        };

        if (dlg.ShowDialog(this) == true)
        {
            ErrorText.Visibility = Visibility.Collapsed;
            TryAttachRenderHost();
            if (_playlist is not null)
            {
                _playlist.Add(dlg.FileName);
                _playlist.PlayAt(_playlist.Items.Count - 1);
            }
            else
            {
                _facade?.Open(dlg.FileName);
            }

            RefreshTransportEnabled();
            _positionTimer.Start();
        }
    }

    private void Play_Click(object sender, RoutedEventArgs e)
    {
        _facade?.Play();
        _positionTimer.Start();
    }

    private void Pause_Click(object sender, RoutedEventArgs e) => _facade?.Pause();

    private void Stop_Click(object sender, RoutedEventArgs e)
    {
        _facade?.Stop();
        UpdateTimeLabels(0, _durationSeconds);
        if (!_seekDragging)
        {
            SeekSlider.Value = 0;
        }

        RefreshTransportEnabled();
    }

    private void Prev_Click(object sender, RoutedEventArgs e)
    {
        _playlist?.PlayPrevious();
        RefreshTransportEnabled();
        _positionTimer.Start();
    }

    private void Next_Click(object sender, RoutedEventArgs e)
    {
        _playlist?.PlayNext();
        RefreshTransportEnabled();
        _positionTimer.Start();
    }

    private void RefreshTransportEnabled()
    {
        if (_playlist is null)
        {
            PrevButton.IsEnabled = false;
            NextButton.IsEnabled = false;
            return;
        }

        PrevButton.IsEnabled = _playlist.CanPlayPrevious;
        NextButton.IsEnabled = _playlist.CanPlayNext;
    }

    private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_facade is null || !IsLoaded)
        {
            return;
        }

        _facade.SetVolume((int)Math.Round(e.NewValue));
    }

    private void SeekSlider_Committed(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        _seekDragging = false;
        CommitSeek();
    }

    private void SeekSlider_LostCapture(object sender, System.Windows.Input.MouseEventArgs e)
    {
        _seekDragging = false;
        CommitSeek();
    }

    private void CommitSeek()
    {
        if (_facade is null)
        {
            return;
        }

        // Slider Maximum tracks duration seconds when known.
        _facade.Seek(SeekSlider.Value);
    }

    private void PollPosition()
    {
        if (_facade is null)
        {
            return;
        }

        var state = _facade.GetState();
        if (state is PlaybackState.Idle or PlaybackState.Stopped or PlaybackState.Error)
        {
            return;
        }

        var pos = _facade.GetPosition();
        var dur = _facade.GetDuration();
        ApplyTimeline(pos, dur);
    }

    private void ApplyTimeline(double positionSeconds, double durationSeconds)
    {
        _durationSeconds = durationSeconds > 0 ? durationSeconds : _durationSeconds;
        UpdateTimeLabels(positionSeconds, _durationSeconds);

        if (_seekDragging)
        {
            return;
        }

        if (_durationSeconds > 0)
        {
            SeekSlider.Maximum = _durationSeconds;
            SeekSlider.Value = Math.Clamp(positionSeconds, 0, _durationSeconds);
        }
    }

    private void UpdateTimeLabels(double positionSeconds, double durationSeconds)
    {
        CurrentTimeText.Text = FormatTime(positionSeconds, durationKnown: durationSeconds > 0 || positionSeconds > 0);
        TotalTimeText.Text = FormatTime(durationSeconds, durationKnown: durationSeconds > 0);
    }

    private static string FormatTime(double seconds, bool durationKnown)
    {
        if (!durationKnown || double.IsNaN(seconds) || seconds < 0)
        {
            return "--:--";
        }

        var whole = (int)Math.Floor(seconds);
        var h = whole / 3600;
        var m = (whole % 3600) / 60;
        var s = whole % 60;
        return h > 0 ? $"{h}:{m:00}:{s:00}" : $"{m:00}:{s:00}";
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.Visibility = Visibility.Visible;
        StatusText.Text = "Error";
    }

    public void OnFirstFrame()
    {
        Dispatcher.Invoke(() =>
        {
            StatusText.Text = "First frame";
            ResizeRenderHost();
            _positionTimer.Start();
            PollPosition();
        });
    }

    public void OnStateChanged(PlaybackState state)
    {
        Dispatcher.Invoke(() =>
        {
            StatusText.Text = $"State: {state}";
            if (state is PlaybackState.Playing or PlaybackState.Opening or PlaybackState.Paused)
            {
                _positionTimer.Start();
            }

            RefreshTransportEnabled();
        });
    }

    public void OnError(string code, string message, bool recoverable)
    {
        Dispatcher.Invoke(() => ShowError($"[{code}] {message} (recoverable={recoverable})"));
    }

    public void OnHardwareAccelChanged(bool active, string reason)
    {
        Dispatcher.Invoke(() =>
        {
            StatusText.Text = active
                ? $"HW accel active: {reason}"
                : $"HW accel inactive: {reason}";
        });
    }

    public void OnPositionChanged(double positionSeconds, double durationSeconds)
    {
        Dispatcher.Invoke(() => ApplyTimeline(positionSeconds, durationSeconds));
    }

    protected override void OnClosed(EventArgs e)
    {
        _positionTimer.Stop();
        try
        {
            _facade?.RenderHost?.Detach();
            _facade?.RemoveObserver(this);
        }
        catch
        {
            // ignore
        }

        base.OnClosed(e);
    }
}
