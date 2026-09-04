using System.Text;

namespace DesktopMediaPlayer.Platform;

/// <summary>
/// Spike logger writing UTF-8 lines to %LocalAppData%/desktop-media-player/spike.log and Console.
/// </summary>
public sealed class SpikeLogger : IDisposable
{
    private readonly object _gate = new();
    private readonly StreamWriter _writer;
    private bool _disposed;

    public SpikeLogger()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "desktop-media-player");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "spike.log");
        _writer = new StreamWriter(
            new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false))
        {
            AutoFlush = true
        };
        Log("info", "logger_started", $"path={path}");
    }

    public void Log(string level, string eventKey, string message)
    {
        var line = $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz} [{level}] {eventKey} {message}";
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            try
            {
                _writer.WriteLine(line);
            }
            catch
            {
                // Never crash the player because of logging I/O.
            }
        }

        try
        {
            Console.WriteLine(line);
        }
        catch
        {
            // Console may be unavailable in some hosts.
        }
    }

    public void Open(string path) => Log("info", "open", path);
    public void FirstFrame() => Log("info", "first_frame", "ok");
    public void HwdecActive(string detail) => Log("info", "hwdec_active", detail);
    public void HwdecFallback(string detail) => Log("warn", "hwdec_fallback", detail);
    public void State(string detail) => Log("info", "state", detail);
    public void Error(string code, string message, bool recoverable) =>
        Log("error", "error", $"code={code} recoverable={recoverable} message={message}");

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            try
            {
                _writer.Dispose();
            }
            catch
            {
                // ignore
            }
        }
    }
}
