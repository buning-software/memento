using Memento.Core.Enumerations;

namespace Memento.Core.Hotkeys.Models;

/// <summary>A global keyboard shortcut: modifiers plus a Windows virtual-key code.</summary>
public readonly record struct Hotkey(HotkeyModifiers Modifiers, uint VirtualKey)
{
    private const uint VirtualKeySpace = 0x20;

    /// <summary>Ctrl+Shift+Space: used on first run and as the fallback when the configured hotkey is taken.</summary>
    public static Hotkey Default { get; } = new(HotkeyModifiers.Control | HotkeyModifiers.Shift, VirtualKeySpace);
}
