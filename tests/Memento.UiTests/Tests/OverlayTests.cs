using Memento.UiTests.Application;
using NUnit.Framework;
using WinUia.Input;
using static WinUia.NUnit.UiAssertions;

namespace Memento.UiTests.Tests;

public class OverlayTests : MementoFixture
{
    private const string Task = "call Anna tomorrow 3pm #work";

    [Test]
    public void Shortcut_OpensOverlayWithFocusedInput()
    {
        var overlay = App.OpenOverlay();

        Eventually(() => overlay.InputBox.HasKeyboardFocus, "the input box has the keyboard focus");
    }

    [Test]
    public void Escape_HidesOverlay()
    {
        var overlay = App.OpenOverlay();

        overlay.Press(VirtualKey.Escape);

        Eventually(() => !App.IsOverlayShown, "Esc hides the overlay");
    }

    [Test]
    public void TypingATask_ShowsWhatWasRecognisedBeforeSaving()
    {
        var overlay = App.OpenOverlay();

        overlay.Type(Task);

        Eventually(() => overlay.VisibleText().Any(t => t.Contains("work")), "the #work tag shows as a badge in the hint bar");
        Assert.That(overlay.Rows, Is.Empty, "nothing is saved until Enter");
    }

    [Test]
    public void Enter_SavesTheTaskAsARow()
    {
        var overlay = App.OpenOverlay();

        overlay.Type(Task);
        overlay.Press(VirtualKey.Enter);

        Eventually(() => overlay.Rows.Count == 1, "the task appears in the list");
        Assert.That(overlay.Rows[0].Name, Does.Contain("call Anna"));
        Eventually(() => overlay.InputBox.ValuePattern.Value == "", "the input is ready for the next task");
    }

    [Test]
    public void EnterOnASelectedTask_CompletesIt()
    {
        var overlay = App.OpenOverlay();
        overlay.Type(Task);
        overlay.Press(VirtualKey.Enter);
        Eventually(() => overlay.Rows.Count == 1, "the task appears in the list");

        overlay.Press(VirtualKey.Down);
        overlay.Press(VirtualKey.Enter);

        Eventually(() => overlay.Rows.Count == 0, "the completed task leaves the list");
    }

    [Test]
    public void DownArrow_WalksFromInputThroughTasksToSettings()
    {
        var overlay = App.OpenOverlay();
        overlay.Type(Task);
        overlay.Press(VirtualKey.Enter);
        Eventually(() => overlay.Rows.Count == 1, "the task appears in the list");

        overlay.Press(VirtualKey.Down); // the task
        overlay.Press(VirtualKey.Down); // past the last task

        Eventually(() => overlay.SettingsButton.HasKeyboardFocus, "the Settings button is the last stop");
    }

    [Test]
    public void Tasks_SurviveARestart()
    {
        var overlay = App.OpenOverlay();
        overlay.Type(Task);
        overlay.Press(VirtualKey.Enter);
        Eventually(() => overlay.Rows.Count == 1, "the task appears in the list");

        Restart();
        var reopened = App.OpenOverlay();

        Eventually(() => reopened.Rows.Count == 1, "the task is still there");
        Assert.That(reopened.Rows[0].Name, Does.Contain("call Anna"));
    }

    [Test]
    public void Overlay_AppearsOnScreen()
    {
        var overlay = App.OpenOverlay();

        Eventually(() => !overlay.Window.IsOffscreen && overlay.Window.BoundingRectangle.Left > -5000, "the overlay is on a screen, not at its off-screen prewarm position");
    }

    [Test]
    public void Enter_OnEmptyInput_SavesNothing()
    {
        var overlay = App.OpenOverlay();

        overlay.Press(VirtualKey.Enter);

        Assert.That(overlay.Rows, Is.Empty);
        Assert.That(App.IsOverlayShown, Is.True, "the overlay stays open");
    }

    [Test]
    public void LineWithoutATitle_AsksForOneAndSavesNothing()
    {
        var overlay = App.OpenOverlay();

        overlay.Type("#work");

        Eventually(() => overlay.VisibleText().Contains("Add a title"), "the hint bar asks for a title");
        overlay.Press(VirtualKey.Enter);
        Assert.That(overlay.Rows, Is.Empty, "tags alone are not a task");
    }

    [Test]
    public void Deadline_ShowsInThePreviewAndOnTheRow()
    {
        var overlay = App.OpenOverlay();

        overlay.Type("call Anna tomorrow 3pm");
        Eventually(() => overlay.VisibleText().Contains("tomorrow 15:00"), "the deadline shows as a badge while typing");
        overlay.Press(VirtualKey.Enter);

        Eventually(() => overlay.Rows.Count == 1 && OverlayPage.TextOf(overlay.Rows[0]).Contains("tomorrow 15:00"), "the row shows the deadline");
    }

    [Test]
    public void SeveralTasks_AreAllListed()
    {
        var overlay = App.OpenOverlay();

        foreach (var title in new[] { "call Anna", "pay rent", "book dentist" })
        {
            overlay.Type(title);
            overlay.Press(VirtualKey.Enter);
            Eventually(() => overlay.InputBox.ValuePattern.Value == "", "the input is ready for the next task");
        }

        Eventually(() => overlay.Rows.Count == 3, "all three tasks are listed");
    }

    [Test]
    public void UpArrow_FromTheInput_GoesToTheSettingsButton()
    {
        var overlay = App.OpenOverlay();

        overlay.Press(VirtualKey.Up);

        Eventually(() => overlay.SettingsButton.HasKeyboardFocus, "Up wraps round to the Settings button");
    }

    [Test]
    public void UpArrow_FromTheSettingsButton_ReturnsToTheLastTask()
    {
        var overlay = App.OpenOverlay();
        overlay.Type(Task);
        overlay.Press(VirtualKey.Enter);
        Eventually(() => overlay.Rows.Count == 1, "the task appears in the list");
        overlay.Press(VirtualKey.Down);
        overlay.Press(VirtualKey.Down);
        Eventually(() => overlay.SettingsButton.HasKeyboardFocus, "the Settings button has the focus");

        overlay.Press(VirtualKey.Up);

        Eventually(() => overlay.Rows[0].SelectionItemPattern.IsSelected, "the task is selected again");
    }

    [Test]
    public void EnterOnTheSettingsButton_OpensSettings()
    {
        var overlay = App.OpenOverlay();
        overlay.Press(VirtualKey.Down);
        Eventually(() => overlay.SettingsButton.HasKeyboardFocus, "the Settings button has the focus");

        overlay.Press(VirtualKey.Enter);

        Eventually(() => App.IsSettingsShown, "Enter on the button opens the settings window");
    }

    [Test]
    public void F2_EditsTheSelectedTaskInPlace()
    {
        var overlay = App.OpenOverlay();
        overlay.Type(Task);
        overlay.Press(VirtualKey.Enter);
        Eventually(() => overlay.Rows.Count == 1, "the task appears in the list");
        overlay.Press(VirtualKey.Down);

        overlay.PressF2();
        Eventually(() => overlay.Editor is not null, "the row turns into an editor");
        overlay.TypeInEditor("call Bob #work");
        overlay.Press(VirtualKey.Enter);

        Eventually(() => overlay.Editor is null && overlay.Rows.Count == 1 && overlay.Rows[0].Name.Contains("call Bob"), "the edited title is saved");
    }

    [Test]
    public void Escape_WhileEditing_CancelsTheEditAndKeepsTheOverlayOpen()
    {
        var overlay = App.OpenOverlay();
        overlay.Type(Task);
        overlay.Press(VirtualKey.Enter);
        Eventually(() => overlay.Rows.Count == 1, "the task appears in the list");
        overlay.Press(VirtualKey.Down);
        overlay.PressF2();
        Eventually(() => overlay.Editor is not null, "the row turns into an editor");
        overlay.TypeInEditor("something else");

        overlay.Press(VirtualKey.Escape);

        Eventually(() => overlay.Editor is null, "the editor is gone");
        Assert.That(App.IsOverlayShown, Is.True, "Esc only cancelled the edit");
        Assert.That(overlay.Rows[0].Name, Does.Contain("call Anna"), "the title is unchanged");
    }
}
