using WinUia.Input;
using WinUia.Models;

namespace Memento.UiTests.Application;

/// <summary>A throwaway data folder for one launch: Memento reads and writes it instead of %APPDATA%.</summary>
public sealed class TestDataDirectory : IDisposable
{
    /// <summary>
    /// Ctrl+Alt+Shift+Space: not the app's default (Ctrl+Shift+Space), so a Memento that is already running on the
    /// machine does not take the shortcut from the one under test, or the other way round.
    /// </summary>
    public static readonly VirtualKey[] CaptureShortcut = [VirtualKey.Control, VirtualKey.Alt, VirtualKey.Shift, VirtualKey.Space];

    /// <param name="extraSettings">More settings.json properties, each starting with a comma: <c>, "DefaultTime": "08:00"</c>.</param>
    public TestDataDirectory(string extraSettings = "")
    {
        Directory.CreateDirectory(Root);
        File.WriteAllText(SettingsFile, $$"""{ "HotkeyModifiers": 7, "HotkeyKey": 32{{extraSettings}} }""");
    }

    public string Root { get; } = Path.Combine(Path.GetTempPath(), $"memento-ui-tests-{Guid.NewGuid():N}");

    public string SettingsFile => Path.Combine(Root, "settings.json");

    public string DatabaseFile => Path.Combine(Root, "memento.db");

    public AppLaunchOptions LaunchOptions => new()
    {
        Environment = new Dictionary<string, string?> { ["MEMENTO_DATA_DIR"] = Root },
    };

    public string? TryReadSettingsFile()
    {
        try
        {
            return File.Exists(SettingsFile) ? File.ReadAllText(SettingsFile) : null;
        }
        catch (IOException)
        {
            return null; // the app is writing it right now
        }
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(Root))
                Directory.Delete(Root, recursive: true);
        }
        catch (IOException)
        {
            // The SQLite files can stay locked for a moment after the process ends; a temp folder is not worth failing a test.
        }
    }
}
