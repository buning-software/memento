using Memento.UiTests.Application;
using NUnit.Framework;
using WinUia.Input;
using static WinUia.NUnit.UiAssertions;

namespace Memento.UiTests.Tests;

public class SettingsTests : MementoFixture
{
    private SettingsPage OpenSettings()
    {
        App.OpenOverlay().SettingsButton.Click();
        return App.Settings();
    }

    [Test]
    public void SettingsButton_OpensSettingsWindow()
    {
        OpenSettings();

        Eventually(() => App.IsSettingsShown, "the settings window opens");
    }

    [Test]
    public void OpeningSettingsAgain_ReusesTheOpenWindow()
    {
        OpenSettings();

        App.OpenOverlay().SettingsButton.Click();

        Eventually(() => App.SettingsWindowCount == 1, "there is still one settings window");
    }

    [Test]
    public void Theme_IsSaved()
    {
        var settings = OpenSettings();

        SettingsPage.Choose(settings.ThemeComboBox, "Light");

        Eventually(() => Data.TryReadSettingsFile()?.Contains("\"Theme\": \"Light\"") == true, "the theme is written to settings.json");
        Assert.That(SettingsPage.SelectedItem(settings.ThemeComboBox), Is.EqualTo("Light"));
    }

    [Test]
    public void Monitor_IsSaved()
    {
        var settings = OpenSettings();

        SettingsPage.Choose(settings.MonitorComboBox, "Monitor with the mouse");

        Eventually(() => Data.TryReadSettingsFile()?.Contains("\"Monitor\": \"Cursor\"") == true, "the monitor is written to settings.json");
    }

    [Test]
    public void DefaultTime_IsSaved()
    {
        var settings = OpenSettings();
        var current = SettingsPage.SelectedItem(settings.DefaultTimeComboBox);
        var other = SettingsPage.Items(settings.DefaultTimeComboBox).First(time => time != current);

        SettingsPage.Choose(settings.DefaultTimeComboBox, other);

        // UI Automation names the item with the time's own culture-dependent text ("6:00 AM"); settings.json holds "HH:mm".
        var saved = TimeOnly.Parse(other).ToString("HH:mm");
        Eventually(() => Data.TryReadSettingsFile()?.Contains($"\"DefaultTime\": \"{saved}\"") == true, $"the time {saved} is written to settings.json");
    }

    [Test]
    public void Shortcut_IsSavedAfterPressingTheNewCombination()
    {
        var settings = OpenSettings();

        settings.ShortcutButton.Click();
        Eventually(() => settings.ShortcutButton.HasKeyboardFocus && settings.ShortcutButton.GetText().StartsWith("Press"), "the button is waiting for a new shortcut");
        settings.Window.Context.Input.SendKeys(VirtualKey.Control, VirtualKey.Alt, VirtualKey.Shift, VirtualKey.Z);

        Eventually(() => Data.TryReadSettingsFile()?.Contains("\"HotkeyKey\": 90") == true, "the new key (Z) is written to settings.json");
    }

    [Test]
    public void SettingsButton_KeepsTheOverlayOpen()
    {
        App.OpenOverlay().SettingsButton.Click();

        Eventually(() => App.IsSettingsShown, "the settings window opens");
        Eventually(() => App.IsSettingsFocused, "the settings window comes in front with the focus, nothing had to give it");
        Assert.That(App.IsOverlayShown, Is.True, "the overlay stays open behind the settings window");
    }

    [Test]
    public void ClosingSettings_ReturnsToTheOverlayAsFreshlyOpened()
    {
        var settings = OpenSettings();

        settings.Window.Context.Input.SendKeys(VirtualKey.Escape);

        Eventually(() => !App.IsSettingsShown, "Esc closes the settings window");
        Assert.That(App.IsOverlayShown, Is.True, "the overlay is still open");
        Eventually(() => App.Overlay().InputBox.HasKeyboardFocus, "the input box has the keyboard focus again, not the Settings button");
    }

    [Test]
    public void Escape_ClosesTheSettingsWindow()
    {
        var settings = OpenSettings();
        settings.Window.Focus();

        settings.Window.Context.Input.SendKeys(VirtualKey.Escape);

        Eventually(() => !App.IsSettingsShown, "Esc closes the window");
    }

    [Test]
    public void Escape_WhileRecordingAShortcut_OnlyCancelsTheRecording()
    {
        var settings = OpenSettings();
        settings.ShortcutButton.Click();
        Eventually(() => settings.ShortcutButton.GetText().StartsWith("Press"), "the button is waiting for a new shortcut");

        settings.Window.Context.Input.SendKeys(VirtualKey.Escape);

        Eventually(() => settings.ShortcutButton.GetText() == "Ctrl+Alt+Shift+Space", "the old shortcut is shown again");
        Assert.That(App.IsSettingsShown, Is.True, "the window stays open");
    }

    [Test]
    public void Shortcut_WithoutAModifier_IsRejected()
    {
        var settings = OpenSettings();
        settings.ShortcutButton.Click();
        Eventually(() => settings.ShortcutButton.GetText().StartsWith("Press"), "the button is waiting for a new shortcut");

        settings.Window.Context.Input.SendKeys(VirtualKey.Z);

        Eventually(() => settings.VisibleText().Any(t => t.StartsWith("Include Ctrl, Alt, Shift or Win")), "the window explains what is missing");
        Assert.That(Data.TryReadSettingsFile(), Does.Contain("\"HotkeyKey\": 32"), "the saved shortcut is unchanged");
    }

    [Test]
    public void NewShortcut_OpensTheOverlay()
    {
        var settings = OpenSettings();
        settings.ShortcutButton.Click();
        Eventually(() => settings.ShortcutButton.GetText().StartsWith("Press"), "the button is waiting for a new shortcut");
        settings.Window.Context.Input.SendKeys(VirtualKey.Control, VirtualKey.Alt, VirtualKey.Shift, VirtualKey.Z);
        Eventually(() => Data.TryReadSettingsFile()?.Contains("\"HotkeyKey\": 90") == true, "the new shortcut is saved");
        settings.Window.Focus();
        settings.Window.Context.Input.SendKeys(VirtualKey.Escape);
        Eventually(() => !App.IsSettingsShown, "the settings window is closed");

        var overlay = App.OpenOverlay([VirtualKey.Control, VirtualKey.Alt, VirtualKey.Shift, VirtualKey.Z]);

        Eventually(() => overlay.InputBox.HasKeyboardFocus, "the new shortcut opens the overlay");
    }

    [Test]
    public void Theme_SurvivesARestart()
    {
        var settings = OpenSettings();
        SettingsPage.Choose(settings.ThemeComboBox, "Light");
        Eventually(() => Data.TryReadSettingsFile()?.Contains("\"Theme\": \"Light\"") == true, "the theme is saved");

        Restart();
        var reopened = OpenSettings();

        Assert.That(SettingsPage.SelectedItem(reopened.ThemeComboBox), Is.EqualTo("Light"));
    }

    [Test]
    public void About_ShowsTheDataFolderInUse()
    {
        var settings = OpenSettings();

        settings.Window.FindByAutomationId("ExpanderToggleButton").Click();

        Eventually(() => settings.VisibleText().Contains(Data.Root), "the About section names the data folder, so the test never touched the real one");
    }
}
