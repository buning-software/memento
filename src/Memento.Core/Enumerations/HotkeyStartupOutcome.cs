using Memento.Core.Hotkeys.Models;

namespace Memento.Core.Enumerations;

public enum HotkeyStartupOutcome
{
    Registered,

    /// <summary>The configured hotkey was taken; <see cref="Hotkey.Default"/> is active and saved instead.</summary>
    FellBackToDefault,

    /// <summary>The configured hotkey is the default and it is taken: no hotkey is active.</summary>
    DefaultInUse,

    ConfiguredAndDefaultInUse,
}
