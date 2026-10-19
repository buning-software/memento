using System.Windows.Input;
using Memento.Core.Enumerations;
using Memento.Core.Hotkeys.Models;
using Memento.Hotkeys;

namespace Memento.UnitTests.Hotkeys;

public class HotkeyTextTests
{
    [Fact]
    public void The_default_hotkey_reads_ctrl_shift_space()
    {
        Assert.Equal("Ctrl+Shift+Space", HotkeyText.Format(Hotkey.Default));
    }

    [Fact]
    public void Modifiers_come_in_a_fixed_order()
    {
        var all = HotkeyModifiers.Windows | HotkeyModifiers.Shift | HotkeyModifiers.Alt | HotkeyModifiers.Control;

        Assert.Equal("Ctrl+Alt+Shift+Win+M", HotkeyText.Format(new Hotkey(all, 0x4D)));
    }

    [Fact]
    public void Digit_keys_are_shown_without_the_D_prefix()
    {
        Assert.Equal("Ctrl+Alt+5", HotkeyText.Format(new Hotkey(HotkeyModifiers.Control | HotkeyModifiers.Alt, 0x35)));
    }

    [Fact]
    public void Wpf_modifier_keys_map_to_hotkey_modifiers()
    {
        Assert.Equal(
            HotkeyModifiers.Control | HotkeyModifiers.Shift | HotkeyModifiers.Windows,
            HotkeyText.ToModifiers(ModifierKeys.Control | ModifierKeys.Shift | ModifierKeys.Windows));
    }

    [Fact]
    public void A_key_press_becomes_a_hotkey()
    {
        Assert.Equal(
            new Hotkey(HotkeyModifiers.Control | HotkeyModifiers.Alt, 0x4D),
            HotkeyText.FromKeyPress(Key.M, ModifierKeys.Control | ModifierKeys.Alt));
    }

    [Theory]
    [InlineData(Key.LeftCtrl)]
    [InlineData(Key.RightShift)]
    [InlineData(Key.LWin)]
    [InlineData(Key.System)]
    public void A_lone_modifier_is_not_a_hotkey_yet(Key key)
    {
        Assert.Null(HotkeyText.FromKeyPress(key, ModifierKeys.Control));
    }
}
