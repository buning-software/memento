using Memento.Core.Enumerations;
using Memento.Core.Hotkeys.Models;
using Memento.Core.Hotkeys.Services;
using Memento.Core.Settings.Models;
using Memento.Core.Storage;
using Memento.Core.UnitTests.TestSupport;
using Memento.Settings.ViewModels;
using Memento.UnitTests.TestSupport;
using static Memento.UnitTests.TestSupport.TestData;

namespace Memento.UnitTests.Settings.ViewModels;

public class SettingsViewModelTests
{
    private static readonly MementoPaths Paths = new(@"C:\Users\test\AppData\Roaming\Memento");

    private readonly InMemorySettingsStore _settings = new(new AppSettings
    {
        Theme = AppTheme.System,
        Monitor = OverlayMonitor.Cursor,
        DefaultTime = new TimeOnly(8, 30),
    });
    private readonly FakeHotkeyRegistrar _registrar = new();
    private readonly FakeAutostart _autostart = new() { Enabled = true };
    private readonly FakeShellLauncher _shell = new();

    private SettingsViewModel CreateViewModel()
    {
        var hotkeys = new HotkeyManager(_registrar, _settings);
        hotkeys.RegisterConfigured(() => { }); // as at startup: the configured hotkey is active
        return new SettingsViewModel(_settings, hotkeys, _autostart, _shell, Paths);
    }

    [Fact]
    public void Shows_the_current_settings()
    {
        var viewModel = CreateViewModel();

        Assert.Equal(AppTheme.System, viewModel.Theme);
        Assert.Equal(OverlayMonitor.Cursor, viewModel.Monitor);
        Assert.Equal(new TimeOnly(8, 30), viewModel.DefaultTime);
        Assert.True(viewModel.StartWithWindows);
        Assert.Equal("Ctrl+Shift+Space", viewModel.ShortcutText);
        Assert.Equal(Paths.DataDirectory, viewModel.DataDirectory);
        Assert.StartsWith("Version ", viewModel.Version);
    }

    [Fact]
    public void Opening_the_window_saves_nothing()
    {
        CreateViewModel(); // loading sets every property once; none of that may be saved

        Assert.Equal(0, _settings.UpdateCount);
    }

    [Fact]
    public void Changing_theme_monitor_and_default_time_saves_them()
    {
        var viewModel = CreateViewModel();

        viewModel.Theme = AppTheme.Light;
        viewModel.Monitor = OverlayMonitor.Primary;
        viewModel.DefaultTime = new TimeOnly(7, 0);

        Assert.Equal(AppTheme.Light, _settings.Current.Theme);
        Assert.Equal(OverlayMonitor.Primary, _settings.Current.Monitor);
        Assert.Equal(new TimeOnly(7, 0), _settings.Current.DefaultTime);
    }

    [Fact]
    public void Changing_the_reminder_settings_saves_them()
    {
        var viewModel = CreateViewModel();
        Assert.True(viewModel.RemindersEnabled);
        Assert.Equal(15, viewModel.ReminderLeadMinutes);

        viewModel.RemindersEnabled = false;
        viewModel.ReminderLeadMinutes = 30;

        Assert.False(_settings.Current.RemindersEnabled);
        Assert.Equal(30, _settings.Current.ReminderLeadMinutes);
    }

    [Fact]
    public void Reminder_lead_options_include_a_custom_value()
    {
        _settings.Update(s => s with { ReminderLeadMinutes = 20 });

        Assert.Equal(new[] { 5, 10, 15, 20, 30, 60 }, CreateViewModel().ReminderLeadOptions);
    }

    [Fact]
    public void Default_time_options_run_every_half_hour_from_six_and_include_a_custom_value()
    {
        _settings.Update(s => s with { DefaultTime = new TimeOnly(8, 15) });

        var options = CreateViewModel().DefaultTimeOptions;

        Assert.Equal(new TimeOnly(6, 0), options[0]);
        Assert.Equal(new TimeOnly(23, 30), options[options.Count - 1]);
        Assert.Equal(37, options.Count);
        Assert.Contains(new TimeOnly(8, 15), options);
        Assert.Equal(options.Order(), options);
    }

    [Fact]
    public void Standard_options_are_not_duplicated_when_the_configured_value_is_one_of_them()
    {
        var viewModel = CreateViewModel(); // 08:30 and 15 minutes are both standard choices

        Assert.Equal(36, viewModel.DefaultTimeOptions.Count);
        Assert.Equal(new[] { 5, 10, 15, 30, 60 }, viewModel.ReminderLeadOptions);
    }

    [Theory]
    [InlineData(5, 0)]
    [InlineData(23, 45)]
    public void A_custom_default_time_outside_the_standard_range_is_added_in_order(int hour, int minute)
    {
        var custom = new TimeOnly(hour, minute);
        _settings.Update(s => s with { DefaultTime = custom });

        var options = CreateViewModel().DefaultTimeOptions;

        Assert.Equal(37, options.Count);
        Assert.Contains(custom, options);
        Assert.Equal(options.Order(), options);
    }

    [Theory]
    [InlineData(1, new[] { 1, 5, 10, 15, 30, 60 })]
    [InlineData(120, new[] { 5, 10, 15, 30, 60, 120 })]
    public void A_custom_reminder_lead_time_is_added_in_order(int minutes, int[] expected)
    {
        _settings.Update(s => s with { ReminderLeadMinutes = minutes });

        Assert.Equal(expected, CreateViewModel().ReminderLeadOptions);
    }

    [Fact]
    public void Setting_a_value_to_what_it_already_is_saves_nothing()
    {
        var viewModel = CreateViewModel();

        viewModel.Theme = AppTheme.System;
        viewModel.Monitor = OverlayMonitor.Cursor;
        viewModel.DefaultTime = new TimeOnly(8, 30);
        viewModel.RemindersEnabled = true;
        viewModel.ReminderLeadMinutes = 15;

        Assert.Equal(0, _settings.UpdateCount);
    }

    [Fact]
    public void Each_changed_setting_is_saved_once()
    {
        var viewModel = CreateViewModel();

        viewModel.Theme = AppTheme.Light;
        viewModel.RemindersEnabled = false;

        Assert.Equal(2, _settings.UpdateCount);
    }

    [Fact]
    public void Changing_one_setting_leaves_the_others_as_they_were()
    {
        var viewModel = CreateViewModel();

        viewModel.Theme = AppTheme.Light;

        Assert.Equal(OverlayMonitor.Cursor, _settings.Current.Monitor);
        Assert.Equal(new TimeOnly(8, 30), _settings.Current.DefaultTime);
        Assert.Equal(Hotkey.Default, _settings.Current.Hotkey);
    }

    [Fact]
    public void Toggling_start_with_windows_on_enables_autostart()
    {
        _autostart.Enabled = false;
        var viewModel = CreateViewModel();
        Assert.False(viewModel.StartWithWindows);

        viewModel.StartWithWindows = true;

        Assert.True(_autostart.Enabled);
        Assert.True(viewModel.IsAutostartAvailable);
    }

    [Fact]
    public void A_refused_autostart_change_saves_no_settings()
    {
        var viewModel = CreateViewModel();
        _autostart.Blocked = true;

        viewModel.StartWithWindows = false;

        Assert.Equal(0, _settings.UpdateCount);
    }

    [Fact]
    public void Blocked_autostart_at_the_start_leaves_the_switch_off()
    {
        _autostart.Blocked = true;

        Assert.False(CreateViewModel().StartWithWindows);
    }

    [Fact]
    public void Available_autostart_has_its_normal_description()
    {
        Assert.Equal("Launch in the tray when you sign in.", CreateViewModel().AutostartDescription);
    }

    [Fact]
    public void Toggling_start_with_windows_changes_autostart()
    {
        var viewModel = CreateViewModel();

        viewModel.StartWithWindows = false;

        Assert.False(_autostart.Enabled);
    }

    [Fact]
    public void Blocked_autostart_is_shown_as_unavailable()
    {
        _autostart.Blocked = true;

        var viewModel = CreateViewModel();

        Assert.False(viewModel.IsAutostartAvailable);
        Assert.Equal("Unavailable: Windows does not allow Memento to change your startup settings.", viewModel.AutostartDescription);
    }

    [Fact]
    public void A_refused_autostart_change_reverts_the_switch()
    {
        var viewModel = CreateViewModel();
        _autostart.Blocked = true;

        viewModel.StartWithWindows = false;

        Assert.True(viewModel.StartWithWindows);
        Assert.False(viewModel.IsAutostartAvailable);
    }

    [Fact]
    public void Recording_asks_for_a_shortcut()
    {
        var viewModel = CreateViewModel();

        viewModel.StartRecording();

        Assert.True(viewModel.IsRecordingShortcut);
        Assert.Equal("Press a shortcut…", viewModel.ShortcutText);
    }

    [Fact]
    public void A_free_shortcut_is_applied_and_saved()
    {
        var viewModel = CreateViewModel();
        viewModel.StartRecording();

        viewModel.ApplyShortcut(CtrlAltM);

        Assert.Equal(CtrlAltM, _settings.Current.Hotkey);
        Assert.Equal("Ctrl+Alt+M", viewModel.ShortcutText);
        Assert.Null(viewModel.ShortcutError);
        Assert.False(viewModel.IsRecordingShortcut);
    }

    [Fact]
    public void A_taken_shortcut_is_explained_and_the_old_one_stays()
    {
        _registrar.TakenByOtherApps.Add(CtrlAltM);
        var viewModel = CreateViewModel();
        viewModel.StartRecording();

        viewModel.ApplyShortcut(CtrlAltM);

        Assert.Equal("Ctrl+Alt+M is already in use by another application.", viewModel.ShortcutError);
        Assert.Equal(Hotkey.Default, _settings.Current.Hotkey);
        Assert.Equal("Ctrl+Shift+Space", viewModel.ShortcutText);
    }

    [Fact]
    public void A_shortcut_without_a_modifier_is_refused_and_recording_continues()
    {
        var viewModel = CreateViewModel();
        viewModel.StartRecording();

        viewModel.ApplyShortcut(new Hotkey(HotkeyModifiers.None, 0x4D));

        Assert.Equal("Include Ctrl, Alt, Shift or Win in the shortcut.", viewModel.ShortcutError);
        Assert.True(viewModel.IsRecordingShortcut);
    }

    [Fact]
    public void Starting_a_new_recording_clears_the_previous_error()
    {
        var viewModel = CreateViewModel();
        viewModel.StartRecording();
        viewModel.ApplyShortcut(new Hotkey(HotkeyModifiers.None, 0x4D));

        viewModel.StopRecording();
        viewModel.StartRecording();

        Assert.Null(viewModel.ShortcutError);
    }

    [Fact]
    public void Applying_the_active_shortcut_again_succeeds_without_saving()
    {
        var viewModel = CreateViewModel();
        viewModel.StartRecording();

        viewModel.ApplyShortcut(Hotkey.Default);

        Assert.Null(viewModel.ShortcutError);
        Assert.Equal("Ctrl+Shift+Space", viewModel.ShortcutText);
        Assert.False(viewModel.IsRecordingShortcut);
        Assert.Equal(0, _settings.UpdateCount);
    }

    [Fact]
    public void A_shortcut_without_a_modifier_changes_nothing()
    {
        var viewModel = CreateViewModel();
        viewModel.StartRecording();

        viewModel.ApplyShortcut(new Hotkey(HotkeyModifiers.None, 0x4D));

        Assert.Equal(0, _settings.UpdateCount);
        Assert.Equal("Press a shortcut…", viewModel.ShortcutText);
        Assert.Single(_registrar.Registered);
    }

    [Fact]
    public void A_free_shortcut_after_a_refused_one_clears_the_error()
    {
        _registrar.TakenByOtherApps.Add(CtrlAltM);
        var viewModel = CreateViewModel();
        viewModel.StartRecording();
        viewModel.ApplyShortcut(CtrlAltM);
        Assert.NotNull(viewModel.ShortcutError);

        _registrar.TakenByOtherApps.Clear();
        viewModel.StartRecording();
        viewModel.ApplyShortcut(CtrlAltM);

        Assert.Null(viewModel.ShortcutError);
        Assert.Equal("Ctrl+Alt+M", viewModel.ShortcutText);
    }

    [Fact]
    public void Stopping_the_recording_shows_the_current_shortcut_again()
    {
        var viewModel = CreateViewModel();
        viewModel.StartRecording();

        viewModel.StopRecording();

        Assert.False(viewModel.IsRecordingShortcut);
        Assert.Equal("Ctrl+Shift+Space", viewModel.ShortcutText);
    }

    [Fact]
    public void The_links_open_the_data_folder_and_the_windows_color_settings()
    {
        var viewModel = CreateViewModel();

        viewModel.OpenDataFolderCommand.Execute(null);
        viewModel.OpenColorSettingsCommand.Execute(null);

        Assert.Equal(new[] { Paths.DataDirectory, "ms-settings:colors" }, _shell.Opened);
    }
}
