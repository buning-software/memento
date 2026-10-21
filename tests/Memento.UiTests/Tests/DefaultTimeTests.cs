using NUnit.Framework;
using WinUia.Input;
using static WinUia.NUnit.UiAssertions;

namespace Memento.UiTests.Tests;

/// <summary>A saved default time is what a deadline with a date but no time gets.</summary>
public class DefaultTimeTests : MementoFixture
{
    protected override string ExtraSettings => """, "DefaultTime": "08:00" """;

    [Test]
    public void SavedDefaultTime_IsUsedForADateWithoutATime()
    {
        var overlay = App.OpenOverlay();

        overlay.Type("call Anna tomorrow");

        Eventually(() => overlay.VisibleText().Contains("tomorrow 08:00"), "the badge uses the saved default time");
    }
}
