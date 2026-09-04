using System.Collections.Concurrent;
using System.Linq;
using System.Globalization;
using System.Text.Json;
using DesktopMediaPlayer.Contracts;

namespace DesktopMediaPlayer.Platform;

/// <summary>JSON file under LocalAppData for resume positions.</summary>
public sealed class FileResumeStore : IResumeStore
{
    private readonly string _filePath;
    private readonly ConcurrentDictionary<string, double> _map = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _io = new();

    public FileResumeStore()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "desktop-media-player");
        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, "resume.json");
        Load();
    }

    public void Save(string path, double positionSeconds, double durationSeconds)
    {
        if (string.IsNullOrWhiteSpace(path) || positionSeconds < 1)
        {
            return;
        }

        // Near end: treat as finished — clear resume.
        if (durationSeconds > 0 && positionSeconds >= durationSeconds - 2)
        {
            Clear(path);
            return;
        }

        _map[path] = positionSeconds;
        Persist();
    }

    public bool TryLoad(string path, out double positionSeconds)
    {
        return _map.TryGetValue(path, out positionSeconds) && positionSeconds > 0;
    }

    public void Clear(string path)
    {
        if (_map.TryRemove(path, out _))
        {
            Persist();
        }
    }

    private void Load()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                return;
            }

            var json = File.ReadAllText(_filePath);
            var data = JsonSerializer.Deserialize<Dictionary<string, double>>(json);
            if (data is null)
            {
                return;
            }

            foreach (var kv in data)
            {
                _map[kv.Key] = kv.Value;
            }
        }
        catch
        {
            // Ignore corrupt resume store.
        }
    }

    private void Persist()
    {
        lock (_io)
        {
            try
            {
                var snapshot = _map.ToDictionary(static kv => kv.Key, static kv => kv.Value, StringComparer.OrdinalIgnoreCase);
                var json = JsonSerializer.Serialize(snapshot);
                File.WriteAllText(_filePath, json);
            }
            catch
            {
                // Best-effort.
            }
        }
    }
}
