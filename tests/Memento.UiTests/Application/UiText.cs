using WinUia;

namespace Memento.UiTests.Application;

public static class UiText
{
    /// <summary>The text of every text element under <paramref name="root"/>, to look for a badge, hint or message without an id.</summary>
    public static IReadOnlyList<string> In(Element root) =>
        root.FindAll(e => e.ControlType == ControlType.Text).Select(e => e.Name).ToList();
}
