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
    internal string? StartupMediaPath { get; private set; }
    internal int StartupArgCount { get; private set; }
#if DMP_DIAG
    internal DiagSeekRunner? DiagSeek { get; private set; }
#endif

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

        // DATADIR: resolve data root before any store/logger exists; invalid => fail, never fall back.
        if (!AppDataPaths.TryParseArgs(e.Args, out var dataPaths, out var startupArgs))
        {
            Console.Error.WriteLine("[error] data_dir reason=invalid");
            Environment.Exit(2);
            return;
        }

        if (dataPaths is not null)
        {
            AppDataPaths.Current = dataPaths;
        }

        Logger = new SpikeLogger();
#if DMP_DIAG
        Engine = new MpvPlaybackEngine(Logger, gpuPreference: GpuPrefsStore.Load(), gpuProbe: new DxgiGpuAdapterProbe(), cacheDir: AppDataPaths.Current.Cache,
            diagSearchDirs: new[] { AppDataPaths.Current.Root, AppContext.BaseDirectory });
#else
        Engine = new MpvPlaybackEngine(Logger, gpuPreference: GpuPrefsStore.Load(), gpuProbe: new DxgiGpuAdapterProbe(), cacheDir: AppDataPaths.Current.Cache);
#endif
        Facade = new PlaybackFacade(Engine, new LoggingObserver(Logger));
        Facade.RenderHost = Engine.RenderHost;
        Engine.Observer = Facade;
        Playlist = new MinimalPlaylistService(Facade);
        ResumeStore = new FileResumeStore(Logger);
#if DMP_DIAG
        var seekScriptPath = DiagSeekScript.ExtractPath(startupArgs, out var args);
        DiagSeek = LoadDiagSeekScript(seekScriptPath);
#else
        System.Collections.Generic.IReadOnlyList<string> args = startupArgs;
#endif
        StartupArgCount = args.Count;
        StartupMediaPath = StartupArgs.FirstMediaPath(args, System.IO.File.Exists);
    }

#if DMP_DIAG
    private DiagSeekRunner? LoadDiagSeekScript(string? path)
    {
        if (path is null || Logger is null || Facade is null)
        {
            return null;
        }

        string[] lines;
        try
        {
            lines = System.IO.File.ReadAllLines(System.IO.Path.GetFullPath(path), System.Text.Encoding.UTF8);
        }
        catch (Exception ex) when (ex is System.IO.IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            Logger.Log("warn", "diag_seek", $"read_error={ex.GetType().Name}");
            return null;
        }

        var result = DiagSeekScript.Parse(lines);
        if (result.RejectedLine is int line)
        {
            Logger.Log("warn", "diag_seek", $"rejected line={line}");
            return null;
        }

        Logger.Log("info", "diag_seek", $"loaded steps={result.Steps.Count}");
        return new DiagSeekRunner(result.Steps, Facade, Logger);
    }
#endif

    internal void ApplyColorTheme(bool dark)
    {
        var d = new ResourceDictionary { Source = new System.Uri(dark ? "Themes/Lumen.Colors.Dark.xaml" : "Themes/Lumen.Colors.Light.xaml", System.UriKind.Relative) };
        var m = Resources.MergedDictionaries;
        for (var i = 0; i < m.Count; i++)
        {
            if (m[i].Source?.OriginalString.Contains("Lumen.Colors.") == true)
            {
                m[i] = d;
                return;
            }
        }
        m.Add(d);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            Facade?.Dispose();
        }
        catch (Exception ex)
        {
            // ignore
            Logger?.Log("warn", "facade_dispose_exception", $"{ex.GetType().Name}: {ex.Message}");
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
