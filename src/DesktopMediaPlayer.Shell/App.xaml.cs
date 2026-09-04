using System.Windows;
using DesktopMediaPlayer.Engine.Mpv;
using DesktopMediaPlayer.Playback;
using DesktopMediaPlayer.Platform;

namespace DesktopMediaPlayer.Shell;

/// <summary>Composes SpikeLogger + MpvPlaybackEngine + PlaybackFacade.</summary>
public partial class App : Application
{
    internal SpikeLogger? Logger { get; private set; }
    internal MpvPlaybackEngine? Engine { get; private set; }
    internal PlaybackFacade? Facade { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += (_, args) =>
        {
            Logger?.Error("ui_unhandled", args.Exception.Message, recoverable: true);
            args.Handled = true;
            MessageBox.Show(
                args.Exception.Message,
                "Desktop Media Player",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        };

        Logger = new SpikeLogger();
        Engine = new MpvPlaybackEngine(Logger);
        Facade = new PlaybackFacade(Engine, new LoggingObserver(Logger));
        Facade.RenderHost = Engine.RenderHost;
        Engine.Observer = Facade;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            Facade?.Dispose();
        }
        catch
        {
            // ignore
        }

        try
        {
            Logger?.Dispose();
        }
        catch
        {
            // ignore
        }

        base.OnExit(e);
    }
}
