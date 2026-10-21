using WinUia;
using WinUia.Input;

namespace Memento.UiTests.Application;

/// <summary>The capture overlay: input box, open tasks and the Settings button.</summary>
public sealed class OverlayPage(Element window)
{
    public Element Window { get; } = window;

    public Element InputBox => Window.FindByAutomationId("InputBox");

    public Element SettingsButton => Window.FindByAutomationId("SettingsButton");

    /// <summary>The open task rows in list order (named after their titles). Empty while there is no list.</summary>
    public IReadOnlyList<Element> Rows =>
        Window.TryFindByAutomationId("TaskList", TimeSpan.FromMilliseconds(200))?
            .FindAll(e => e.ControlType == ControlType.ListItem) ?? [];

    /// <summary>The inline title editor of the row being edited (F2); null while no row is.</summary>
    public Element? Editor => Window.TryFind(e => e.ControlType == ControlType.Edit && e.Name == "Task title", TreeScope.Descendants, TimeSpan.FromMilliseconds(300));

    public void PressF2() => NativeInput.PressF2();

    public static IReadOnlyList<string> TextOf(Element row) => UiText.In(row);

    /// <summary>Types a line into the input box and checks it arrived whole, typing it again if characters were lost.</summary>
    public void Type(string text) => TypeInto(() => InputBox, text);

    /// <summary>Replaces the text of the row being edited (F2).</summary>
    public void TypeInEditor(string text) => TypeInto(() => Editor ?? throw new InvalidOperationException("No row is being edited."), text);

    private void TypeInto(Func<Element> field, string text)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            field().Focus();
            Press(VirtualKey.Control, VirtualKey.A);
            Window.Context.Input.SendText(text);
            if (Poll.Until(() => field().ValuePattern.Value == text, TimeSpan.FromSeconds(2)))
                return;
        }

        throw new InvalidOperationException($"Could not type '{text}' into the overlay.");
    }

    public void Press(params VirtualKey[] keys) => Window.Context.Input.SendKeys(keys);

    /// <summary>The text of every text element in the window, to look for a badge or hint without knowing its id.</summary>
    public IEnumerable<string> VisibleText() => UiText.In(Window);
}
