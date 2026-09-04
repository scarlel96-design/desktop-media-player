using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using DesktopMediaPlayer.Contracts;
using DesktopMediaPlayer.Playback;
using Microsoft.Win32;

namespace DesktopMediaPlayer.Shell;

/// <summary>
/// Phase A S1–S5: UX-001 chrome, timeline, transport, volume, FS/Sub/Audio/Playlist.
/// Facade-only. Blur OFF. Subtitles via engine tracks (no UI overlay path).
/// </summary>
public partial class MainWindow : Window, IPlaybackObserver
{
    private readonly DispatcherTimer _positionTimer;
    private PlaybackFacade? _facade;
    private IPlaylistService? _playlist;
    private bool _renderAttached;
    private bool _seekDragging;
    private double _durationSeconds;
    private int _lastAudibleVolume = 80;
    private bool _muteUi;
    private MediaTrackKind? _flyoutKind;
    private bool _suppressTrackSelection;
    private WindowState _windowStateBeforeFullscreen = WindowState.Normal;
    private WindowStyle _windowStyleBeforeFullscreen = WindowStyle.SingleBorderWindow;

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
        _lastAudibleVolume = 80;
        _facade.SetVolume(80);
        _facade.SetMute(false);
        RefreshMuteGlyph();

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

        var volume = (int)Math.Round(e.NewValue);
        _facade.SetVolume(volume);
        if (volume > 0)
        {
            _lastAudibleVolume = volume;
            if (_muteUi)
            {
                _facade.SetMute(false);
                _muteUi = false;
            }
        }

        RefreshMuteGlyph();
    }

    private void Mute_Click(object sender, RoutedEventArgs e)
    {
        if (_facade is null)
        {
            return;
        }

        _muteUi = !_muteUi;
        _facade.SetMute(_muteUi);
        if (_muteUi)
        {
            if (VolumeSlider.Value > 0)
            {
                _lastAudibleVolume = (int)Math.Round(VolumeSlider.Value);
            }
        }
        else if (VolumeSlider.Value <= 0)
        {
            VolumeSlider.Value = Math.Clamp(_lastAudibleVolume, 1, 100);
        }

        RefreshMuteGlyph();
    }

    private void VolumeGroup_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
    {
        var delta = e.Delta > 0 ? 5 : -5;
        VolumeSlider.Value = Math.Clamp(VolumeSlider.Value + delta, 0, 100);
        e.Handled = true;
    }

    private void RefreshMuteGlyph()
    {
        var muted = _muteUi || VolumeSlider.Value <= 0;
        MuteButton.Content = muted ? "🔇" : "🔊";
        MuteButton.ToolTip = muted ? "Unmute" : "Mute";
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
            if (_flyoutKind is MediaTrackKind kind)
            {
                PopulateTrackList(kind);
            }

            RefreshPlaylistUi();
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
            RefreshPlaylistUi();
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


    // --- S5: FS / Sub / Audio / Playlist ---

    private void Fullscreen_Click(object sender, RoutedEventArgs e) => ToggleFullscreen();

    private void VideoArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            ToggleFullscreen();
            e.Handled = true;
        }
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F11 || (e.Key == Key.Escape && WindowStyle == WindowStyle.None))
        {
            ToggleFullscreen();
            e.Handled = true;
        }
    }

    private void ToggleFullscreen()
    {
        if (WindowStyle != WindowStyle.None)
        {
            _windowStateBeforeFullscreen = WindowState;
            _windowStyleBeforeFullscreen = WindowStyle;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            WindowState = WindowState.Maximized;
            FullscreenButton.ToolTip = "Exit fullscreen";
        }
        else
        {
            WindowStyle = _windowStyleBeforeFullscreen;
            ResizeMode = ResizeMode.CanResize;
            WindowState = _windowStateBeforeFullscreen;
            FullscreenButton.ToolTip = "Fullscreen";
        }

        Dispatcher.BeginInvoke(ResizeRenderHost, DispatcherPriority.Loaded);
    }

    private void Sub_Click(object sender, RoutedEventArgs e)
    {
        ToggleTrackFlyout(MediaTrackKind.Subtitle, "Subtitles (engine layer)");
        LoadSubButton.Visibility = Visibility.Visible;
    }

    private void Audio_Click(object sender, RoutedEventArgs e)
    {
        ToggleTrackFlyout(MediaTrackKind.Audio, "Audio tracks");
        LoadSubButton.Visibility = Visibility.Collapsed;
    }

    private void CloseTrackFlyout_Click(object sender, RoutedEventArgs e)
    {
        TrackFlyout.Visibility = Visibility.Collapsed;
        _flyoutKind = null;
    }

    private void ToggleTrackFlyout(MediaTrackKind kind, string title)
    {
        if (_flyoutKind == kind && TrackFlyout.Visibility == Visibility.Visible)
        {
            TrackFlyout.Visibility = Visibility.Collapsed;
            _flyoutKind = null;
            return;
        }

        _flyoutKind = kind;
        TrackFlyoutTitle.Text = title;
        PopulateTrackList(kind);
        TrackFlyout.Visibility = Visibility.Visible;
        PlaylistPanel.Visibility = Visibility.Collapsed;
        SideColumn.Width = new GridLength(0);
    }

    private void PopulateTrackList(MediaTrackKind kind)
    {
        if (_facade is null)
        {
            return;
        }

        _suppressTrackSelection = true;
        TrackList.Items.Clear();
        TrackList.Items.Add(new TrackListItem("Off", id: 0, kind));
        foreach (var track in _facade.ListTracks().Where(t => t.Kind == kind))
        {
            var label = $"{track.Id}: {track.Title ?? track.Language ?? kind.ToString()}";
            var item = new TrackListItem(label, track.Id, kind);
            TrackList.Items.Add(item);
            if (track.IsSelected)
            {
                TrackList.SelectedItem = item;
            }
        }

        _suppressTrackSelection = false;
    }

    private void TrackList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressTrackSelection || _facade is null || TrackList.SelectedItem is not TrackListItem item)
        {
            return;
        }

        _facade.SelectTrack(item.Kind, item.Id);
        StatusText.Text = $"Selected {item.Kind} #{item.Id}";
    }

    private void LoadExternalSub_Click(object sender, RoutedEventArgs e)
    {
        if (_facade is null)
        {
            return;
        }

        var dlg = new OpenFileDialog
        {
            Title = "Load external subtitle",
            Filter = "Subtitles|*.srt;*.ass;*.ssa;*.vtt;*.sub|All files|*.*"
        };
        if (dlg.ShowDialog(this) == true)
        {
            _facade.LoadExternalSubtitle(dlg.FileName);
            PopulateTrackList(MediaTrackKind.Subtitle);
        }
    }

    private void Playlist_Click(object sender, RoutedEventArgs e)
    {
        var open = PlaylistPanel.Visibility != Visibility.Visible;
        PlaylistPanel.Visibility = open ? Visibility.Visible : Visibility.Collapsed;
        SideColumn.Width = open ? new GridLength(300) : new GridLength(0);
        if (open)
        {
            TrackFlyout.Visibility = Visibility.Collapsed;
            _flyoutKind = null;
            RefreshPlaylistUi();
        }
    }

    private void PlaylistList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (_playlist is null || PlaylistList.SelectedIndex < 0)
        {
            return;
        }

        _playlist.PlayAt(PlaylistList.SelectedIndex);
        RefreshTransportEnabled();
        _positionTimer.Start();
    }

    private void RefreshPlaylistUi()
    {
        if (_playlist is null)
        {
            return;
        }

        PlaylistList.Items.Clear();
        var items = _playlist.Items;
        PlaylistEmpty.Visibility = items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        for (var i = 0; i < items.Count; i++)
        {
            var marker = i == _playlist.CurrentIndex ? "▶ " : "  ";
            PlaylistList.Items.Add($"{marker}{System.IO.Path.GetFileName(items[i])}");
        }
    }

    private sealed class TrackListItem
    {
        public TrackListItem(string label, int id, MediaTrackKind kind)
        {
            Label = label;
            Id = id;
            Kind = kind;
        }

        public string Label { get; }
        public int Id { get; }
        public MediaTrackKind Kind { get; }
        public override string ToString() => Label;
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
