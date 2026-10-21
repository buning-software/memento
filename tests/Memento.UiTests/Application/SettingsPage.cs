using WinUia;

namespace Memento.UiTests.Application;

public sealed class SettingsPage(Element window)
{
    public Element Window { get; } = window;

    public Element ShortcutButton => Window.FindByAutomationId("ShortcutButton");

    public Element MonitorComboBox => Window.FindByAutomationId("MonitorComboBox");

    public Element DefaultTimeComboBox => Window.FindByAutomationId("DefaultTimeComboBox");

    public Element ThemeComboBox => Window.FindByAutomationId("ThemeComboBox");

    public Element StartWithWindowsToggle => Window.FindByAutomationId("StartWithWindowsToggle");

    public IReadOnlyList<string> VisibleText() => UiText.In(Window);

    /// <summary>Picks <paramref name="item"/> by its visible text and closes the list again.</summary>
    public static void Choose(Element comboBox, string item)
    {
        comboBox.Expand();
        comboBox.Find(e => e.ControlType == ControlType.ListItem && e.Name == item).Select();
        comboBox.Collapse();
    }

    /// <summary>The visible text of every choice, in list order.</summary>
    public static IReadOnlyList<string> Items(Element comboBox)
    {
        comboBox.Expand();
        var items = comboBox.FindAll(e => e.ControlType == ControlType.ListItem).Select(e => e.Name).ToList();
        comboBox.Collapse();
        return items;
    }

    public static string SelectedItem(Element comboBox)
    {
        comboBox.Expand();
        var selected = comboBox.FindAll(e => e.ControlType == ControlType.ListItem)
            .Single(i => i.SelectionItemPattern.IsSelected).Name;
        comboBox.Collapse();
        return selected;
    }
}
