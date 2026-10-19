using Memento.Core.Enumerations;
using Memento.Core.Hotkeys.Models;

namespace Memento.UnitTests.TestSupport;

internal static class TestData
{
    /// <summary>A hotkey other than the default (Ctrl+Shift+Space).</summary>
    public static readonly Hotkey CtrlAltM = new(HotkeyModifiers.Control | HotkeyModifiers.Alt, 0x4D);
}
