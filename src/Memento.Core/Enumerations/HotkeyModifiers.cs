namespace Memento.Core.Enumerations;

/// <summary>Modifier keys of a hotkey. The values equal Win32's MOD_* flags, so they pass to RegisterHotKey unchanged.</summary>
[Flags]
public enum HotkeyModifiers : uint
{
    None = 0,
    Alt = 0x1,
    Control = 0x2,
    Shift = 0x4,
    Windows = 0x8,
}
