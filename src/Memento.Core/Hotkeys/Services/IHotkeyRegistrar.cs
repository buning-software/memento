using Memento.Core.Hotkeys.Models;

namespace Memento.Core.Hotkeys.Services;

/// <summary>Claims system-wide hotkeys. The Windows implementation is Memento.Hotkeys.Win32HotkeyRegistrar.</summary>
public interface IHotkeyRegistrar
{
    /// <summary>False when another application already owns the hotkey.</summary>
    bool TryRegister(Hotkey hotkey, Action onPressed);

    void UnregisterAll();
}
