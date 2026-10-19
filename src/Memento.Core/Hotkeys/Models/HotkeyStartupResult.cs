using Memento.Core.Enumerations;

namespace Memento.Core.Hotkeys.Models;

public sealed record HotkeyStartupResult(HotkeyStartupOutcome Outcome, Hotkey Configured);
