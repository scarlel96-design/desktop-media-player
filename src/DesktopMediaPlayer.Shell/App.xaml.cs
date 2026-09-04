using System.Windows;
using DesktopMediaPlayer.Contracts;
using DesktopMediaPlayer.Engine.Mpv;
using DesktopMediaPlayer.Playback;
using DesktopMediaPlayer.Platform;

namespace DesktopMediaPlayer.Shell;

/// <summary>Composition root: logger, engine, facade, playlist, resume.</summary>
public partial class App : Application
{
    internal SpikeLogger? Logger { get; private set; }
    internal MpvPlaybackEngine? Engine { get; private set; }
    internal PlaybackFacade? Facade { get; private set; }
    internal IPlaylistService? Playlist { get; private set; }
    internal IResumeStore? ResumeStore { get; private set; }

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
        Playlist = new MinimalPlaylistService(Facade);
        ResumeStore = new FileResumeStore();
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
