using Memento.Core.Enumerations;
using Memento.Core.Hotkeys.Models;
using Memento.Core.Settings.Models;
using Memento.Core.Settings.Services;
using Memento.Core.UnitTests.TestSupport;

namespace Memento.Core.UnitTests.Settings.Services;

public sealed class JsonSettingsStoreTests : IDisposable
{
    private readonly TempDataDirectory _data = new();

    public void Dispose() => _data.Dispose();

    private void WriteSettingsFile(string json)
    {
        Directory.CreateDirectory(_data.Paths.DataDirectory);
        File.WriteAllText(_data.Paths.SettingsPath, json);
    }

    [Fact]
    public void Without_a_file_the_defaults_apply()
    {
        var settings = new JsonSettingsStore(_data.Paths).Current;

        Assert.Equal(Hotkey.Default, settings.Hotkey);
        Assert.Equal(AppTheme.Dark, settings.Theme);
        Assert.Equal(new TimeOnly(9, 0), settings.DefaultTime);
        Assert.Equal(OverlayMonitor.Primary, settings.Monitor);
    }

    [Fact]
    public void An_update_is_saved_and_read_back_by_a_new_store()
    {
        var hotkey = new Hotkey(HotkeyModifiers.Control | HotkeyModifiers.Alt, 0x4D);

        new JsonSettingsStore(_data.Paths).Update(s => s.WithHotkey(hotkey) with { Theme = AppTheme.Light });

        var reloaded = new JsonSettingsStore(_data.Paths).Current;
        Assert.Equal(hotkey, reloaded.Hotkey);
        Assert.Equal(AppTheme.Light, reloaded.Theme);
    }

    [Fact]
    public void An_update_raises_Changed_with_the_new_settings()
    {
        var store = new JsonSettingsStore(_data.Paths);
        AppSettings? received = null;
        store.Changed += (_, settings) => received = settings;

        store.Update(s => s with { Monitor = OverlayMonitor.Cursor });

        Assert.Equal(OverlayMonitor.Cursor, received?.Monitor);
    }

    [Fact]
    public void An_update_that_changes_nothing_neither_saves_nor_raises_Changed()
    {
        var store = new JsonSettingsStore(_data.Paths);
        bool raised = false;
        store.Changed += (_, _) => raised = true;

        store.Update(s => s with { Theme = AppTheme.Dark });

        Assert.False(raised);
        Assert.False(File.Exists(_data.Paths.SettingsPath));
    }

    [Fact]
    public void Reads_the_settings_file_written_by_the_mvp()
    {
        WriteSettingsFile("""
            {
              "HotkeyModifiers": 3,
              "HotkeyKey": 77,
              "Theme": "System",
              "DefaultTime": "08:30",
              "Monitor": "Cursor"
            }
            """);

        var settings = new JsonSettingsStore(_data.Paths).Current;

        Assert.Equal(new Hotkey(HotkeyModifiers.Alt | HotkeyModifiers.Control, 77), settings.Hotkey);
        Assert.Equal(AppTheme.System, settings.Theme);
        Assert.Equal(new TimeOnly(8, 30), settings.DefaultTime);
        Assert.Equal(OverlayMonitor.Cursor, settings.Monitor);
    }

    [Fact]
    public void Writes_the_format_the_mvp_reads()
    {
        new JsonSettingsStore(_data.Paths).Update(s => s with { Theme = AppTheme.Light });

        string json = File.ReadAllText(_data.Paths.SettingsPath);
        Assert.Contains("\"HotkeyModifiers\": 6", json);
        Assert.Contains("\"Theme\": \"Light\"", json);
        Assert.Contains("\"DefaultTime\": \"09:00\"", json);
    }

    [Theory]
    [InlineData("{ not json")]
    [InlineData("""{ "Theme": "Purple" }""")]
    public void An_unreadable_file_gives_the_defaults(string json)
    {
        WriteSettingsFile(json);

        Assert.Equal(new AppSettings(), new JsonSettingsStore(_data.Paths).Current);
    }

    [Theory]
    [InlineData("\"25:99\"")]
    [InlineData("930")]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("""{ "Hour": 9, "Minute": [30] }""")]
    [InlineData("[]")]
    [InlineData("[\"09:30\"]")]
    public void An_invalid_default_time_falls_back_to_nine_and_keeps_the_rest(string defaultTime)
    {
        WriteSettingsFile($$"""{ "DefaultTime": {{defaultTime}}, "Theme": "Light" }""");

        var settings = new JsonSettingsStore(_data.Paths).Current;

        Assert.Equal(new TimeOnly(9, 0), settings.DefaultTime);
        Assert.Equal(AppTheme.Light, settings.Theme);
    }
}
