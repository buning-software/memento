using System.Text.Json;
using System.Text.Json.Serialization;
using Memento.Core.Settings.Models;
using Memento.Core.Storage;

namespace Memento.Core.Settings.Services;

/// <summary>Settings in settings.json. Reading never fails (defaults instead); a failed write keeps the change in memory for this session.</summary>
public sealed class JsonSettingsStore : ISettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly string _path;

    public JsonSettingsStore(MementoPaths paths)
    {
        _path = paths.SettingsPath;
        Current = Load(_path);
    }

    public AppSettings Current { get; private set; }

    public event EventHandler<AppSettings>? Changed;

    public void Update(Func<AppSettings, AppSettings> change)
    {
        var updated = change(Current);
        if (updated == Current)
            return;

        Current = updated;
        Save();
        Changed?.Invoke(this, updated);
    }

    private static AppSettings Load(string path)
    {
        try
        {
            return File.Exists(path)
                ? JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path), JsonOptions) ?? new AppSettings()
                : new AppSettings();
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            return new AppSettings(); // unreadable file: defaults
        }
    }

    private void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            File.WriteAllText(_path, JsonSerializer.Serialize(Current, JsonOptions));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Not worth interrupting the user: the settings stay in memory for this session.
        }
    }
}
