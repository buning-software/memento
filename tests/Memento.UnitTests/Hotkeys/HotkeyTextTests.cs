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

    [Theory]
    [InlineData(0x70u, "F1")]
    [InlineData(0x7Bu, "F12")]
    [InlineData(0x20u, "Space")]
    [InlineData(0x0Du, "Return")]
    [InlineData(0x1Bu, "Escape")]
    [InlineData(0x09u, "Tab")]
    [InlineData(0x41u, "A")]
    [InlineData(0x44u, "D")]       // the letter D is not a digit key
    [InlineData(0x30u, "0")]
    [InlineData(0x39u, "9")]
    [InlineData(0x60u, "NumPad0")] // only the main-row digits lose their prefix
    public void Format_names_the_key(uint virtualKey, string expected)
    {
        Assert.Equal(expected, HotkeyText.Format(new Hotkey(HotkeyModifiers.None, virtualKey)));
    }

    [Fact]
    public void Format_without_modifiers_is_just_the_key()
    {
        Assert.Equal("F5", HotkeyText.Format(new Hotkey(HotkeyModifiers.None, 0x74)));
    }

    [Theory]
    [InlineData(HotkeyModifiers.Control, "Ctrl+F5")]
    [InlineData(HotkeyModifiers.Alt, "Alt+F5")]
    [InlineData(HotkeyModifiers.Shift, "Shift+F5")]
    [InlineData(HotkeyModifiers.Windows, "Win+F5")]
    public void Format_names_a_single_modifier(HotkeyModifiers modifier, string expected)
    {
        Assert.Equal(expected, HotkeyText.Format(new Hotkey(modifier, 0x74)));
    }

    [Fact]
    public void No_modifier_keys_map_to_no_hotkey_modifiers()
    {
        Assert.Equal(HotkeyModifiers.None, HotkeyText.ToModifiers(ModifierKeys.None));
    }

    [Theory]
    [InlineData(ModifierKeys.Control, HotkeyModifiers.Control)]
    [InlineData(ModifierKeys.Alt, HotkeyModifiers.Alt)]
    [InlineData(ModifierKeys.Shift, HotkeyModifiers.Shift)]
    [InlineData(ModifierKeys.Windows, HotkeyModifiers.Windows)]
    public void Each_wpf_modifier_key_maps_to_its_own_hotkey_modifier(ModifierKeys keys, HotkeyModifiers expected)
    {
        Assert.Equal(expected, HotkeyText.ToModifiers(keys));
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

    [Fact]
    public void A_key_press_without_modifiers_is_a_hotkey_with_no_modifiers()
    {
        Assert.Equal(new Hotkey(HotkeyModifiers.None, 0x74), HotkeyText.FromKeyPress(Key.F5, ModifierKeys.None));
    }

    [Theory]
    [InlineData(Key.F5, ModifierKeys.Control | ModifierKeys.Shift, "Ctrl+Shift+F5")]
    [InlineData(Key.Space, ModifierKeys.Control | ModifierKeys.Shift, "Ctrl+Shift+Space")]
    [InlineData(Key.D5, ModifierKeys.Alt, "Alt+5")]
    [InlineData(Key.M, ModifierKeys.Windows | ModifierKeys.Alt, "Alt+Win+M")]
    [InlineData(Key.Return, ModifierKeys.Control, "Ctrl+Return")]
    public void A_key_press_reads_back_as_the_text_the_user_pressed(Key key, ModifierKeys modifiers, string expected)
    {
        var hotkey = HotkeyText.FromKeyPress(key, modifiers);

        Assert.True(hotkey.HasValue);
        Assert.Equal(expected, HotkeyText.Format(hotkey.Value));
    }

    [Fact]
    public void The_default_hotkey_is_what_pressing_ctrl_shift_space_gives()
    {
        Assert.Equal(Hotkey.Default, HotkeyText.FromKeyPress(Key.Space, ModifierKeys.Control | ModifierKeys.Shift));
    }

    [Fact]
    public void No_key_at_all_is_not_a_hotkey()
    {
        Assert.Null(HotkeyText.FromKeyPress(Key.None, ModifierKeys.None));
        Assert.Null(HotkeyText.FromKeyPress(Key.None, ModifierKeys.Control | ModifierKeys.Alt));
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
