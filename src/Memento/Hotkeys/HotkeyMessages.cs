using Memento.Core.Enumerations;
using Memento.Core.Hotkeys.Models;

namespace Memento.Hotkeys;

/// <summary>User-facing text for hotkey problems. Every message says how to still reach Memento.</summary>
public static class HotkeyMessages
{
    /// <summary>The warning to show after startup, or null when the configured hotkey works.</summary>
    public static string? ForStartup(HotkeyStartupResult result)
    {
        string configured = HotkeyText.Format(result.Configured);
        string fallback = HotkeyText.Format(Hotkey.Default);

        return result.Outcome switch
        {
            HotkeyStartupOutcome.Registered => null,
            HotkeyStartupOutcome.FellBackToDefault =>
                $"{configured} is in use by another application. Memento now uses {fallback}. You can change this in Settings.",
            HotkeyStartupOutcome.DefaultInUse =>
                $"{configured} is in use by another application. Open Memento from the tray icon and pick another shortcut in Settings.",
            HotkeyStartupOutcome.ConfiguredAndDefaultInUse =>
                $"{configured} and {fallback} are both in use by other applications. Open Memento from the tray icon and pick another shortcut in Settings.",
            _ => throw new ArgumentOutOfRangeException(nameof(result), result.Outcome, null),
        };
    }

    public static string InUse(Hotkey hotkey) => $"{HotkeyText.Format(hotkey)} is already in use by another application.";
}
