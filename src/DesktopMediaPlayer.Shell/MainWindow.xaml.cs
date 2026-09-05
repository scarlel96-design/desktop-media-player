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
/// Phase A Shell: UX-001 chrome through S8 Soft (resume@first-frame).
/// Facade-only. Blur OFF. No settings search / P1.
/// </summary>
public partial class MainWindow : Window, IPlaybackObserver
{
    private readonly DispatcherTimer _positionTimer;
    private readonly DispatcherTimer _autoHideTimer;
    private PlaybackFacade? _facade;
    private IPlaylistService? _playlist;
    private IResumeStore? _resume;
    private bool _renderAttached;
    private bool _seekDragging;
    private double _durationSeconds;
    private int _lastAudibleVolume = 80;
    private bool _muteUi;
    private MediaTrackKind? _flyoutKind;
    private bool _suppressTrackSelection;
    private WindowState _windowStateBeforeFullscreen = WindowState.Normal;
    private WindowStyle _windowStyleBeforeFullscreen = WindowStyle.SingleBorderWindow;
    private AppThemeMode _theme = AppThemeMode.Dark;
    private string? _currentPath;
    private double? _pendingResumeSeconds;

    public MainWindow()
    {
        InitializeComponent();
        _positionTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        _positionTimer.Tick += (_, _) =>
        {
            PollPosition();
            SyncMuteFromEngine();
        };

        _autoHideTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(2.5)
        };
        _autoHideTimer.Tick += (_, _) => TryAutoHideChrome();

        Loaded += OnLoaded;
        SizeChanged += (_, _) => ResizeRenderHost();
        DpiChanged += (_, _) => ResizeRenderHost();
        SeekSlider.PreviewMouseLeftButtonDown += (_, _) => _seekDragging = true;
        SeekSlider.PreviewMouseLeftButtonUp += (_, _) => _seekDragging = false;
        ApplyTheme(_theme);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var app = (App)Application.Current;
        _facade = app.Facade;
        _playlist = app.Playlist;
        _resume = app.ResumeStore;
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
        RefreshThemeButton();

        TryAttachRenderHost();
        ResizeRenderHost();
        UpdateTimeLabels(0, 0);
        ShowChrome();

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

        if (dlg.ShowDialog(this) != true)
        {
            return;
        }

        ErrorText.Visibility = Visibility.Collapsed;
        TryAttachRenderHost();
        PersistResume();
        _currentPath = dlg.FileName;
        ArmPendingResume(_currentPath);

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
        RefreshPlaylistUi();
        _positionTimer.Start();
        ArmAutoHide();
    }

    private void Play_Click(object sender, RoutedEventArgs e)
    {
        _facade?.Play();
        _positionTimer.Start();
        ArmAutoHide();
    }

    private void Pause_Click(object sender, RoutedEventArgs e)
    {
        _facade?.Pause();
        PersistResume();
        ShowChrome();
        _autoHideTimer.Stop();
    }

    private void Stop_Click(object sender, RoutedEventArgs e)
    {
        PersistResume();
        _facade?.Stop();
        UpdateTimeLabels(0, _durationSeconds);
        if (!_seekDragging)
        {
            SeekSlider.Value = 0;
        }

        RefreshTransportEnabled();
        ShowChrome();
        _autoHideTimer.Stop();
    }

    private void Prev_Click(object sender, RoutedEventArgs e)
    {
        PersistResume();
        _playlist?.PlayPrevious();
        SyncCurrentPathFromPlaylist();
        RefreshTransportEnabled();
        RefreshPlaylistUi();
        _positionTimer.Start();
        ArmAutoHide();
    }

    private void Next_Click(object sender, RoutedEventArgs e)
    {
        PersistResume();
        _playlist?.PlayNext();
        SyncCurrentPathFromPlaylist();
        RefreshTransportEnabled();
        RefreshPlaylistUi();
        _positionTimer.Start();
        ArmAutoHide();
    }

    private void SyncCurrentPathFromPlaylist()
    {
        if (_playlist is null || _playlist.CurrentIndex < 0 || _playlist.CurrentIndex >= _playlist.Items.Count)
        {
            return;
        }

        _currentPath = _playlist.Items[_playlist.CurrentIndex];
        ArmPendingResume(_currentPath);
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

    private void VolumeGroup_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
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

    private void SyncMuteFromEngine()
    {
        if (_facade is null)
        {
            return;
        }

        var engineMute = _facade.GetMute();
        if (engineMute != _muteUi)
        {
            _muteUi = engineMute;
            RefreshMuteGlyph();
        }
    }

    private void SeekSlider_Committed(object sender, MouseButtonEventArgs e)
    {
        _seekDragging = false;
        CommitSeek();
    }

    private void SeekSlider_LostCapture(object sender, MouseEventArgs e)
    {
        _seekDragging = false;
        CommitSeek();
    }

    private void CommitSeek()
    {
        _facade?.Seek(SeekSlider.Value);
        PersistResume();
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

        ApplyTimeline(_facade.GetPosition(), _facade.GetDuration());
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
        ShowChrome();
        if (ErrorText.Style is null)
        {
            // ensure error remains visible in both themes
        }
    }

    // --- S5 panels ---

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
        if (_facade is null)
        {
            return;
        }

        switch (e.Key)
        {
            case Key.Space:
                if (_facade.GetState() == PlaybackState.Playing)
                {
                    Pause_Click(sender, e);
                }
                else
                {
                    Play_Click(sender, e);
                }

                e.Handled = true;
                break;
            case Key.Left:
                _facade.Seek(Math.Max(0, _facade.GetPosition() - 5));
                e.Handled = true;
                break;
            case Key.Right:
                _facade.Seek(_facade.GetPosition() + 5);
                e.Handled = true;
                break;
            case Key.Up:
                VolumeSlider.Value = Math.Min(100, VolumeSlider.Value + 5);
                e.Handled = true;
                break;
            case Key.Down:
                VolumeSlider.Value = Math.Max(0, VolumeSlider.Value - 5);
                e.Handled = true;
                break;
            case Key.M:
                Mute_Click(sender, e);
                e.Handled = true;
                break;
            case Key.F:
            case Key.F11:
                ToggleFullscreen();
                e.Handled = true;
                break;
            case Key.Escape when WindowStyle == WindowStyle.None:
                ToggleFullscreen();
                e.Handled = true;
                break;
            case Key.O when (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control:
                Open_Click(sender, e);
                e.Handled = true;
                break;
            case Key.S when (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control:
                Stop_Click(sender, e);
                e.Handled = true;
                break;
            case Key.P when (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control:
                Playlist_Click(sender, e);
                e.Handled = true;
                break;
            case Key.T when (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control:
                Theme_Click(sender, e);
                e.Handled = true;
                break;
            case Key.OemPeriod:
            case Key.Decimal:
                _facade.FrameStep(1);
                e.Handled = true;
                break;
            case Key.OemComma:
                _facade.FrameStep(-1);
                e.Handled = true;
                break;
            case Key.F12:
                Screenshot_Click(sender, e);
                e.Handled = true;
                break;
            case Key.I when (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control:
                Info_Click(sender, e);
                e.Handled = true;
                break;
        }

        ArmAutoHide();
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
        ArmAutoHide();
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
        ArmAutoHide();
    }

    private void ToggleTrackFlyout(MediaTrackKind kind, string title)
    {
        if (_flyoutKind == kind && TrackFlyout.Visibility == Visibility.Visible)
        {
            TrackFlyout.Visibility = Visibility.Collapsed;
            _flyoutKind = null;
            ArmAutoHide();
            return;
        }

        _flyoutKind = kind;
        TrackFlyoutTitle.Text = title;
        PopulateTrackList(kind);
        TrackFlyout.Visibility = Visibility.Visible;
        PlaylistPanel.Visibility = Visibility.Collapsed;
        SideColumn.Width = new GridLength(0);
        ShowChrome();
        _autoHideTimer.Stop();
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
            ShowChrome();
            _autoHideTimer.Stop();
        }
        else
        {
            ArmAutoHide();
        }
    }

    private void PlaylistList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (_playlist is null || PlaylistList.SelectedIndex < 0)
        {
            return;
        }

        PersistResume();
        _playlist.PlayAt(PlaylistList.SelectedIndex);
        SyncCurrentPathFromPlaylist();
        RefreshTransportEnabled();
        RefreshPlaylistUi();
        _positionTimer.Start();
        ArmAutoHide();
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

    // --- S6 theme / auto-hide / resume ---

    private void Theme_Click(object sender, RoutedEventArgs e)
    {
        _theme = _theme switch
        {
            AppThemeMode.Dark => AppThemeMode.Light,
            AppThemeMode.Light => AppThemeMode.System,
            _ => AppThemeMode.Dark
        };
        ApplyTheme(_theme);
        RefreshThemeButton();
    }

    private void RefreshThemeButton()
    {
        ThemeButton.ToolTip = $"Theme: {_theme} (click to cycle)";
        ThemeButton.Content = _theme switch
        {
            AppThemeMode.Dark => "◐",
            AppThemeMode.Light => "◑",
            _ => "◎"
        };
    }

    private void ApplyTheme(AppThemeMode mode)
    {
        var useDark = mode switch
        {
            AppThemeMode.Dark => true,
            AppThemeMode.Light => false,
            AppThemeMode.System => !IsSystemLightTheme(),
            _ => true
        };

        var bg = useDark ? Color.FromRgb(0x12, 0x12, 0x12) : Color.FromRgb(0xF2, 0xF2, 0xF2);
        var fg = useDark ? Color.FromRgb(0xF0, 0xF0, 0xF0) : Color.FromRgb(0x1A, 0x1A, 0x1A);
        var chrome = useDark ? Color.FromArgb(0xE6, 0x12, 0x12, 0x12) : Color.FromArgb(0xE6, 0xF2, 0xF2, 0xF2);
        Background = new SolidColorBrush(bg);
        Foreground = new SolidColorBrush(fg);
        BottomChrome.Background = new SolidColorBrush(chrome);
        StatusText.Foreground = new SolidColorBrush(useDark ? Color.FromArgb(0xA0, 0xFF, 0xFF, 0xFF) : Color.FromArgb(0xA0, 0x00, 0x00, 0x00));
    }

    private static bool IsSystemLightTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var value = key?.GetValue("AppsUseLightTheme");
            return value is int i && i == 1;
        }
        catch
        {
            return false;
        }
    }

    private void Root_MouseMove(object sender, MouseEventArgs e)
    {
        ShowChrome();
        ArmAutoHide();
    }

    private void ShowChrome()
    {
        BottomChrome.Opacity = 1;
        BottomChrome.IsHitTestVisible = true;
    }

    private void TryAutoHideChrome()
    {
        _autoHideTimer.Stop();
        if (ShouldPauseAutoHide())
        {
            return;
        }

        if (_facade?.GetState() != PlaybackState.Playing)
        {
            return;
        }

        // Opacity-only hide (Blur OFF, no Invalidate spam).
        BottomChrome.Opacity = 0;
        BottomChrome.IsHitTestVisible = false;
    }

    private bool ShouldPauseAutoHide() =>
        TrackFlyout.Visibility == Visibility.Visible
        || PlaylistPanel.Visibility == Visibility.Visible
        || MediaInfoFlyout.Visibility == Visibility.Visible
        || ErrorText.Visibility == Visibility.Visible;

    private void ArmAutoHide()
    {
        ShowChrome();
        _autoHideTimer.Stop();
        if (!ShouldPauseAutoHide() && _facade?.GetState() == PlaybackState.Playing)
        {
            _autoHideTimer.Start();
        }
    }

    private void PersistResume()
    {
        if (_resume is null || string.IsNullOrWhiteSpace(_currentPath) || _facade is null)
        {
            return;
        }

        _resume.Save(_currentPath, _facade.GetPosition(), _facade.GetDuration());
    }

    /// <summary>S8 Soft: queue resume position; seek only after first frame (opening race).</summary>
    private void ArmPendingResume(string path)
    {
        _pendingResumeSeconds = null;
        if (_resume is null || string.IsNullOrWhiteSpace(path) || !_resume.TryLoad(path, out var pos) || pos < 1)
        {
            return;
        }

        _pendingResumeSeconds = pos;
    }

    private void ApplyPendingResumeAtFirstFrame()
    {
        if (_facade is null || _pendingResumeSeconds is not double pos)
        {
            return;
        }

        _pendingResumeSeconds = null;
        _facade.Seek(pos);
        StatusText.Text = $"Resumed at {FormatTime(pos, true)}";
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
            ArmAutoHide();
            ApplyPendingResumeAtFirstFrame();
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

            if (state is PlaybackState.Paused or PlaybackState.Stopped or PlaybackState.Ended)
            {
                PersistResume();
                ShowChrome();
                _autoHideTimer.Stop();
            }
            else if (state == PlaybackState.Playing)
            {
                ArmAutoHide();
            }

            RefreshTransportEnabled();
            RefreshPlaylistUi();
            SyncMuteFromEngine();
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


    // --- S7 ---

    private void FrameStep_Click(object sender, RoutedEventArgs e)
    {
        _facade?.FrameStep(1);
        _facade?.Pause();
        ShowChrome();
        _autoHideTimer.Stop();
    }

    private void FrameBack_Click(object sender, RoutedEventArgs e)
    {
        _facade?.FrameStep(-1);
        _facade?.Pause();
        ShowChrome();
        _autoHideTimer.Stop();
    }

    private void Screenshot_Click(object sender, RoutedEventArgs e)
    {
        if (_facade is null)
        {
            return;
        }

        var dlg = new SaveFileDialog
        {
            Title = "Save screenshot",
            Filter = "PNG image|*.png|JPEG image|*.jpg",
            FileName = $"capture-{DateTime.Now:yyyyMMdd-HHmmss}.png"
        };
        if (dlg.ShowDialog(this) == true)
        {
            _facade.Screenshot(dlg.FileName);
            StatusText.Text = $"Screenshot → {dlg.FileName}";
        }
    }

    private void Info_Click(object sender, RoutedEventArgs e)
    {
        if (_facade is null)
        {
            return;
        }

        if (MediaInfoFlyout.Visibility == Visibility.Visible)
        {
            MediaInfoFlyout.Visibility = Visibility.Collapsed;
            ArmAutoHide();
            return;
        }

        var info = _facade.GetMediaInfo();
        MediaInfoText.Text =
            "Path: " + (info.Path ?? "-") + "\n" +
            "Title: " + (info.Title ?? "-") + "\n" +
            "Format: " + (info.Format ?? "-") + "\n" +
            "Video: " + (info.VideoCodec ?? "-") + " " + info.Width + "x" + info.Height + "\n" +
            "Audio: " + (info.AudioCodec ?? "-") + "\n" +
            "Duration: " + FormatTime(info.DurationSeconds, info.DurationSeconds > 0);
        MediaInfoFlyout.Visibility = Visibility.Visible;
        ShowChrome();
        _autoHideTimer.Stop();
    }

    private void CloseInfo_Click(object sender, RoutedEventArgs e)
    {
        MediaInfoFlyout.Visibility = Visibility.Collapsed;
        ArmAutoHide();
    }

    protected override void OnClosed(EventArgs e)
    {
        PersistResume();
        _positionTimer.Stop();
        _autoHideTimer.Stop();
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
}
