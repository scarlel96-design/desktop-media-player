using System.Windows;
using System.Windows.Media;
using DesktopMediaPlayer.Contracts;
using DesktopMediaPlayer.Playback;
using Microsoft.Win32;

namespace DesktopMediaPlayer.Shell;

/// <summary>
/// Phase A S1: UX-001 Control Bar skeleton over video host.
/// Calls <see cref="PlaybackFacade"/> only — never mpv P/Invoke. Blur OFF.
/// </summary>
public partial class MainWindow : Window, IPlaybackObserver
{
    private PlaybackFacade? _facade;
    private bool _renderAttached;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        SizeChanged += (_, _) => ResizeRenderHost();
        DpiChanged += (_, _) => ResizeRenderHost();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var app = (App)Application.Current;
        _facade = app.Facade;
        if (_facade is null)
        {
            ShowError("Playback facade was not composed.");
            return;
        }

        _facade.AddObserver(this);
        VolumeSlider.Value = 80;
        _facade.SetVolume(80);

        TryAttachRenderHost();
        ResizeRenderHost();

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

        var hwnd = VideoHost.Handle;
        if (hwnd == nint.Zero)
        {
            Dispatcher.BeginInvoke(TryAttachRenderHost, System.Windows.Threading.DispatcherPriority.Loaded);
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
            _facade?.Open(dlg.FileName);
        }
    }

    private void Play_Click(object sender, RoutedEventArgs e) => _facade?.Play();
    private void Pause_Click(object sender, RoutedEventArgs e) => _facade?.Pause();
    private void Stop_Click(object sender, RoutedEventArgs e) => _facade?.Stop();

    private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_facade is null || !IsLoaded)
        {
            return;
        }

        _facade.SetVolume((int)Math.Round(e.NewValue));
    }

    private void SeekSlider_Committed(object sender, System.Windows.Input.MouseButtonEventArgs e) => CommitSeek();

    private void SeekSlider_LostCapture(object sender, System.Windows.Input.MouseEventArgs e) => CommitSeek();

    private void CommitSeek()
    {
        _facade?.Seek(SeekSlider.Value);
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
        });
    }

    public void OnStateChanged(PlaybackState state)
    {
        Dispatcher.Invoke(() => StatusText.Text = $"State: {state}");
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
        // Wired in S2 timeline; keep no-op for S0 Shell.
    }

    protected override void OnClosed(EventArgs e)
    {
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
