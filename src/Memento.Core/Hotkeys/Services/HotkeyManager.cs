using Memento.Core.Enumerations;
using Memento.Core.Hotkeys.Models;
using Memento.Core.Settings.Services;

namespace Memento.Core.Hotkeys.Services;

/// <summary>
/// Owns the one capture hotkey. Never throws when a hotkey is taken: the tray icon keeps Memento reachable,
/// and a failed change leaves the previous hotkey working.
/// </summary>
public sealed class HotkeyManager(IHotkeyRegistrar registrar, ISettingsStore settings)
{
    private Action _onPressed = () => { };
    private Hotkey? _active;

    /// <summary>Registers the configured hotkey; when another app owns it, falls back to the default and saves that.</summary>
    public HotkeyStartupResult RegisterConfigured(Action onPressed)
    {
        _onPressed = onPressed;
        var configured = settings.Current.Hotkey;

        if (registrar.TryRegister(configured, onPressed))
        {
            _active = configured;
            return new(HotkeyStartupOutcome.Registered, configured);
        }

        if (configured == Hotkey.Default)
            return new(HotkeyStartupOutcome.DefaultInUse, configured);

        if (!registrar.TryRegister(Hotkey.Default, onPressed))
            return new(HotkeyStartupOutcome.ConfiguredAndDefaultInUse, configured);

        _active = Hotkey.Default;
        settings.Update(s => s.WithHotkey(Hotkey.Default));
        return new(HotkeyStartupOutcome.FellBackToDefault, configured);
    }

    /// <summary>Switches to <paramref name="hotkey"/> and saves it. False when another app owns it; the previous hotkey then stays active.</summary>
    public bool TryChange(Hotkey hotkey)
    {
        if (hotkey == _active)
            return true;

        registrar.UnregisterAll();
        if (registrar.TryRegister(hotkey, _onPressed))
        {
            _active = hotkey;
            settings.Update(s => s.WithHotkey(hotkey));
            return true;
        }

        // Put the previous hotkey back; if that fails too, nothing is active, so a later TryChange really registers it.
        if (_active is { } previous && !registrar.TryRegister(previous, _onPressed))
            _active = null;
        return false;
    }
}
