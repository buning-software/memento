using System.IO;
using System.Windows;
using Memento.Core.Hotkeys.Models;
using Memento.Core.Hotkeys.Services;
using Memento.Core.Settings.Models;
using Memento.Core.Settings.Services;
using Memento.Core.Storage;
using Memento.EntityFramework.Initialization;
using Memento.Hotkeys;
using Memento.Overlay;
using Memento.Overlay.ViewModels;
using Memento.Reminders;
using Memento.Settings.Services;
using Memento.Shell.Services;
using Memento.Theme;

namespace Memento.Shell;

/// <summary>The startup sequence, run once WPF has started: storage first, then the visible parts.</summary>
public sealed class AppStartup(
    DatabaseInitializer database,
    ThemeController theme,
    OverlayWindow overlay,
    OverlayViewModel overlayViewModel,
    SettingsWindowLauncher settingsWindow,
    HotkeyManager hotkeys,
    TrayIcon tray,
    ReminderScheduler reminders,
    ISettingsStore settings,
    IDialogService dialogs)
{
    public void Run()
    {
        if (!TryInitializeDatabase())
            return;

        theme.Apply();
        overlay.Prewarm();

        var hotkeyResult = hotkeys.RegisterConfigured(overlay.ToggleOverlay);

        overlayViewModel.SettingsRequested += (_, _) => settingsWindow.Open(overlay);
        settingsWindow.Closed += (_, _) => overlay.ResetAfterSettings();

        tray.OpenRequested += (_, _) => overlay.ShowOverlay();
        tray.ExitRequested += (_, _) => Application.Current.Shutdown();
        tray.Show();
        reminders.Start();
        ShowHotkeyInTray(settings.Current);
        settings.Changed += (_, current) => ShowHotkeyInTray(current);

        // Never fatal: the tray icon keeps Memento reachable.
        if (HotkeyMessages.ForStartup(hotkeyResult) is { } warning)
            dialogs.ShowWarning(warning);
    }

    /// <summary>Without a database there is nothing to show: explain and exit.</summary>
    private bool TryInitializeDatabase()
    {
        try
        {
            database.Initialize();
            return true;
        }
        catch (Exception ex) when (ex is StorageException or IOException or UnauthorizedAccessException)
        {
            dialogs.ShowError($"The database could not be initialized.\n\n{ex.Message}");
            Application.Current.Shutdown(1);
            return false;
        }
    }

    private void ShowHotkeyInTray(AppSettings current) => tray.SetHotkeyText(HotkeyText.Format(current.Hotkey));
}
