using System.Security;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Memento.Core.Enumerations;
using Memento.Core.Hotkeys.Models;
using Memento.Core.Hotkeys.Services;
using Memento.Core.Settings.Models;
using Memento.Core.Settings.Services;
using Memento.Core.Storage;
using Memento.Hotkeys;
using Memento.Settings.Services;

namespace Memento.Settings.ViewModels;

/// <summary>The settings window. Every change is saved at once; there is no OK/Cancel.</summary>
public sealed partial class SettingsViewModel : ObservableObject
{
    private const string AvailableAutostartText = "Launch in the tray when you sign in.";
    private const string UnavailableAutostartText = "Unavailable: Windows does not allow Memento to change your startup settings.";

    private readonly ISettingsStore _settings;
    private readonly HotkeyManager _hotkeys;
    private readonly IAutostart _autostart;
    private readonly IShellLauncher _shell;

    /// <summary>True while properties are set from code (loading, reverting), so those sets are not saved.</summary>
    private bool _suppressSave = true;

    public SettingsViewModel(ISettingsStore settings, HotkeyManager hotkeys, IAutostart autostart, IShellLauncher shell, MementoPaths paths)
    {
        _settings = settings;
        _hotkeys = hotkeys;
        _autostart = autostart;
        _shell = shell;

        var current = settings.Current;
        Theme = current.Theme;
        Monitor = current.Monitor;
        DefaultTime = current.DefaultTime;
        DefaultTimeOptions = BuildTimeOptions(current.DefaultTime);
        RemindersEnabled = current.RemindersEnabled;
        ReminderLeadMinutes = current.ReminderLeadMinutes;
        ReminderLeadOptions = BuildLeadOptions(current.ReminderLeadMinutes);
        ShortcutText = HotkeyText.Format(current.Hotkey);
        DataDirectory = paths.DataDirectory;
        Version = $"Version {typeof(SettingsViewModel).Assembly.GetName().Version?.ToString(3)}";

        try
        {
            StartWithWindows = autostart.IsEnabled();
            IsAutostartAvailable = true;
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or SecurityException)
        {
            IsAutostartAvailable = false;
        }

        _suppressSave = false; // loaded: from now on every change is saved
    }

    [ObservableProperty]
    public partial AppTheme Theme { get; set; }

    [ObservableProperty]
    public partial OverlayMonitor Monitor { get; set; }

    [ObservableProperty]
    public partial TimeOnly DefaultTime { get; set; }

    /// <summary>Every half hour from 06:00 to 23:30, plus the configured time if it is not one of those.</summary>
    public IReadOnlyList<TimeOnly> DefaultTimeOptions { get; }

    [ObservableProperty]
    public partial bool RemindersEnabled { get; set; }

    [ObservableProperty]
    public partial int ReminderLeadMinutes { get; set; }

    /// <summary>The lead times to choose from (minutes), plus the configured one if it is not one of those.</summary>
    public IReadOnlyList<int> ReminderLeadOptions { get; }

    [ObservableProperty]
    public partial bool StartWithWindows { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AutostartDescription))]
    public partial bool IsAutostartAvailable { get; set; }

    public string AutostartDescription => IsAutostartAvailable ? AvailableAutostartText : UnavailableAutostartText;

    /// <summary>The shortcut button's text: the current shortcut, or a prompt while recording.</summary>
    [ObservableProperty]
    public partial string ShortcutText { get; set; } = "";

    [ObservableProperty]
    public partial string? ShortcutError { get; set; }

    [ObservableProperty]
    public partial bool IsRecordingShortcut { get; set; }

    public string DataDirectory { get; }

    public string Version { get; }

    public void StartRecording()
    {
        IsRecordingShortcut = true;
        ShortcutError = null;
        ShortcutText = "Press a shortcut…";
    }

    public void StopRecording()
    {
        IsRecordingShortcut = false;
        ShortcutText = HotkeyText.Format(_settings.Current.Hotkey);
    }

    /// <summary>A shortcut needs a modifier (recording continues); a taken one is explained and the old one stays.</summary>
    public void ApplyShortcut(Hotkey hotkey)
    {
        if (hotkey.Modifiers == HotkeyModifiers.None)
        {
            ShortcutError = "Include Ctrl, Alt, Shift or Win in the shortcut.";
            return;
        }

        ShortcutError = _hotkeys.TryChange(hotkey) ? null : HotkeyMessages.InUse(hotkey);
        StopRecording();
    }

    [RelayCommand]
    private void OpenDataFolder() => _shell.Open(DataDirectory);

    [RelayCommand]
    private void OpenColorSettings() => _shell.Open("ms-settings:colors");

    partial void OnThemeChanged(AppTheme value) => Save(s => s with { Theme = value });

    partial void OnMonitorChanged(OverlayMonitor value) => Save(s => s with { Monitor = value });

    partial void OnDefaultTimeChanged(TimeOnly value) => Save(s => s with { DefaultTime = value });

    partial void OnRemindersEnabledChanged(bool value) => Save(s => s with { RemindersEnabled = value });

    partial void OnReminderLeadMinutesChanged(int value) => Save(s => s with { ReminderLeadMinutes = value });

    partial void OnStartWithWindowsChanged(bool value)
    {
        if (_suppressSave)
            return;

        try
        {
            _autostart.SetEnabled(value);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or SecurityException)
        {
            IsAutostartAvailable = false;
            _suppressSave = true;
            StartWithWindows = !value; // put the switch back
            _suppressSave = false;
        }
    }

    private void Save(Func<AppSettings, AppSettings> change)
    {
        if (!_suppressSave)
            _settings.Update(change);
    }

    private static IReadOnlyList<int> BuildLeadOptions(int current)
    {
        var options = new List<int> { 5, 10, 15, 30, 60 };
        if (!options.Contains(current))
        {
            options.Add(current);
            options.Sort();
        }
        return options;
    }

    private static IReadOnlyList<TimeOnly> BuildTimeOptions(TimeOnly current)
    {
        var options = Enumerable.Range(0, 36).Select(i => new TimeOnly(6, 0).AddMinutes(30 * i)).ToList();
        if (!options.Contains(current))
        {
            options.Add(current);
            options.Sort();
        }
        return options;
    }
}
