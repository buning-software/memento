using System.Windows.Input;
using Memento.Core.Enumerations;
using Memento.Core.Hotkeys.Models;

namespace Memento.Hotkeys;

/// <summary>Converts hotkeys between WPF key presses and display text such as "Ctrl+Shift+Space".</summary>
public static class HotkeyText
{
    private static readonly HashSet<Key> ModifierOnlyKeys =
    [
        Key.LeftCtrl, Key.RightCtrl, Key.LeftAlt, Key.RightAlt,
        Key.LeftShift, Key.RightShift, Key.LWin, Key.RWin, Key.System, Key.None,
    ];

    public static string Format(Hotkey hotkey)
    {
        var parts = new List<string>();
        if (hotkey.Modifiers.HasFlag(HotkeyModifiers.Control)) parts.Add("Ctrl");
        if (hotkey.Modifiers.HasFlag(HotkeyModifiers.Alt)) parts.Add("Alt");
        if (hotkey.Modifiers.HasFlag(HotkeyModifiers.Shift)) parts.Add("Shift");
        if (hotkey.Modifiers.HasFlag(HotkeyModifiers.Windows)) parts.Add("Win");

        string key = KeyInterop.KeyFromVirtualKey((int)hotkey.VirtualKey).ToString();
        if (key.Length == 2 && key[0] == 'D' && char.IsAsciiDigit(key[1]))
            key = key[1..]; // WPF names the digit keys D0..D9
        parts.Add(key);

        return string.Join('+', parts);
    }

    public static HotkeyModifiers ToModifiers(ModifierKeys keys)
    {
        var modifiers = HotkeyModifiers.None;
        if (keys.HasFlag(ModifierKeys.Control)) modifiers |= HotkeyModifiers.Control;
        if (keys.HasFlag(ModifierKeys.Alt)) modifiers |= HotkeyModifiers.Alt;
        if (keys.HasFlag(ModifierKeys.Shift)) modifiers |= HotkeyModifiers.Shift;
        if (keys.HasFlag(ModifierKeys.Windows)) modifiers |= HotkeyModifiers.Windows;
        return modifiers;
    }

    /// <summary>The hotkey a key press describes, or null while only modifier keys are held.</summary>
    public static Hotkey? FromKeyPress(Key key, ModifierKeys modifiers) =>
        ModifierOnlyKeys.Contains(key)
            ? null
            : new Hotkey(ToModifiers(modifiers), (uint)KeyInterop.VirtualKeyFromKey(key));
}
