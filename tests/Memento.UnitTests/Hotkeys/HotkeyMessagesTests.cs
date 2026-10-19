using Memento.Core.Enumerations;
using Memento.Core.Hotkeys.Models;
using Memento.Hotkeys;
using static Memento.UnitTests.TestSupport.TestData;

namespace Memento.UnitTests.Hotkeys;

public class HotkeyMessagesTests
{
    [Fact]
    public void A_registered_hotkey_needs_no_message()
    {
        Assert.Null(HotkeyMessages.ForStartup(new HotkeyStartupResult(HotkeyStartupOutcome.Registered, CtrlAltM)));
    }

    [Theory]
    [InlineData(HotkeyStartupOutcome.FellBackToDefault,
        "Ctrl+Alt+M is in use by another application. Memento now uses Ctrl+Shift+Space. You can change this in Settings.")]
    [InlineData(HotkeyStartupOutcome.DefaultInUse,
        "Ctrl+Alt+M is in use by another application. Open Memento from the tray icon and pick another shortcut in Settings.")]
    [InlineData(HotkeyStartupOutcome.ConfiguredAndDefaultInUse,
        "Ctrl+Alt+M and Ctrl+Shift+Space are both in use by other applications. Open Memento from the tray icon and pick another shortcut in Settings.")]
    public void Each_startup_problem_explains_how_to_reach_memento(HotkeyStartupOutcome outcome, string expected)
    {
        Assert.Equal(expected, HotkeyMessages.ForStartup(new HotkeyStartupResult(outcome, CtrlAltM)));
    }

    [Fact]
    public void InUse_names_the_hotkey()
    {
        Assert.Equal("Ctrl+Alt+M is already in use by another application.", HotkeyMessages.InUse(CtrlAltM));
    }
}
