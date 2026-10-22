using Memento.Core.Enumerations;
using Memento.Enumerations;
using Memento.Overlay.Tasks.Models;
using Memento.Overlay.Tasks.ViewModels;
using Memento.Overlay.ViewModels;
using Memento.UnitTests.TestSupport;

namespace Memento.UnitTests.Overlay.ViewModels;

public class OverlayViewModelEditingTests
{
    private readonly OverlayFixture _fixture = new();
    private readonly OverlayViewModel _viewModel;

    public OverlayViewModelEditingTests()
    {
        _viewModel = _fixture.CreateViewModel();
        _viewModel.InputText = "call Anna tomorrow 3pm #work";
        _viewModel.HandleKey(OverlayKey.Enter); // captured through the real service, with its original text
        _viewModel.SelectedIndex = 0;
    }

    private TaskRowViewModel Row => _viewModel.Tasks[0];

    [Fact]
    public void F2_edits_the_selected_task_starting_from_the_line_as_typed()
    {
        Assert.True(_viewModel.HandleKey(OverlayKey.F2));

        Assert.True(_viewModel.IsEditing);
        Assert.True(Row.IsEditing);
        Assert.Equal("call Anna tomorrow 3pm #work", Row.EditText);
        Assert.Equal(OverlayFocus.Editor, _viewModel.Focus);
        Assert.Equal(("save", "cancel", false), (_viewModel.EnterHint, _viewModel.EscapeHint, _viewModel.ShowEditHint));
        Assert.Equal(2, _viewModel.Preview.Count);
    }

    [Fact]
    public void F2_without_a_selection_does_nothing()
    {
        _viewModel.SelectedIndex = -1;

        Assert.True(_viewModel.HandleKey(OverlayKey.F2));
        Assert.False(_viewModel.IsEditing);
    }

    [Fact]
    public void The_preview_follows_the_editor_text()
    {
        _viewModel.HandleKey(OverlayKey.F2);

        Row.EditText = "call Bob friday";

        Assert.Equal(new PreviewBadge(PreviewBadgeKind.Deadline, "fri 16 oct 09:00"), Assert.Single(_viewModel.Preview));
    }

    [Fact]
    public void Enter_saves_the_edit_and_keeps_the_task_selected()
    {
        string id = Row.Item.Id;
        _viewModel.HandleKey(OverlayKey.F2);
        Row.EditText = "call Bob friday";

        _viewModel.HandleKey(OverlayKey.Enter);

        Assert.False(_viewModel.IsEditing);
        Assert.Equal("call Bob", Assert.Single(_fixture.Repository.Tasks).Title);
        Assert.Equal(id, _viewModel.Tasks[_viewModel.SelectedIndex].Item.Id);
        Assert.Equal(OverlayFocus.Input, _viewModel.Focus);
    }

    [Fact]
    public void Enter_on_unchanged_text_ends_the_edit_and_keeps_the_task_as_it_was()
    {
        var before = _fixture.Repository.Tasks.Single();
        _viewModel.HandleKey(OverlayKey.F2);

        _viewModel.HandleKey(OverlayKey.Enter);

        Assert.False(_viewModel.IsEditing);
        Assert.Equal(before, Assert.Single(_fixture.Repository.Tasks));
        Assert.Equal(0, _viewModel.SelectedIndex);
        Assert.Empty(_viewModel.ErrorText);
    }

    [Fact]
    public void Enter_on_an_emptied_editor_keeps_editing()
    {
        _viewModel.HandleKey(OverlayKey.F2);
        Row.EditText = "";

        _viewModel.HandleKey(OverlayKey.Enter);

        Assert.True(_viewModel.IsEditing);
        Assert.Equal("call Anna", Assert.Single(_fixture.Repository.Tasks).Title);
    }

    [Fact]
    public void A_line_without_a_title_in_the_editor_asks_for_a_title()
    {
        _viewModel.HandleKey(OverlayKey.F2);

        Row.EditText = "#work";

        Assert.Equal("Add a title", _viewModel.PreviewHint);
    }

    [Fact]
    public void Editing_only_the_tags_saves_the_task_and_lists_the_new_tags()
    {
        _viewModel.HandleKey(OverlayKey.F2);
        Row.EditText = "call Anna tomorrow 3pm #home";

        _viewModel.HandleKey(OverlayKey.Enter);

        Assert.Equal("#home", _viewModel.Tasks[0].TagsText);
        Assert.Equal("tomorrow 15:00", _viewModel.Tasks[0].DeadlineText);
    }

    [Fact]
    public void Saving_an_edit_that_moves_the_task_down_keeps_it_selected()
    {
        var first = _fixture.AddTask("a", new DateTime(2026, 10, 13, 11, 0, 0));
        _fixture.AddTask("b", new DateTime(2026, 10, 13, 12, 0, 0));
        _viewModel.Refresh();
        Assert.Equal(first.Id, Row.Item.Id); // today 11:00 sorts before the captured task of tomorrow
        _viewModel.SelectedIndex = 0;
        _viewModel.HandleKey(OverlayKey.F2);

        Row.EditText = "a 2026-10-20 09:00";
        _viewModel.HandleKey(OverlayKey.Enter);

        Assert.Equal(_viewModel.Tasks.Count - 1, _viewModel.SelectedIndex);
        Assert.Equal(first.Id, _viewModel.Tasks[_viewModel.SelectedIndex].Item.Id);
    }

    [Fact]
    public void A_task_stored_without_its_original_text_is_edited_from_a_rebuilt_line()
    {
        _fixture.AddTask("report", new DateTime(2026, 10, 12, 9, 0, 0), "work");
        _viewModel.Refresh();
        _viewModel.SelectedIndex = 0; // the oldest deadline comes first

        _viewModel.HandleKey(OverlayKey.F2);

        Assert.Equal("report 2026-10-12 09:00 #work", Row.EditText);
    }

    [Fact]
    public void Escape_after_a_failed_save_clears_the_error_and_stops_editing()
    {
        _viewModel.HandleKey(OverlayKey.F2);
        Row.EditText = "call Bob";
        _fixture.Repository.Fail = true;
        _viewModel.HandleKey(OverlayKey.Enter);
        Assert.NotEmpty(_viewModel.ErrorText);

        _viewModel.HandleKey(OverlayKey.Escape);

        Assert.False(_viewModel.IsEditing);
        Assert.Empty(_viewModel.ErrorText);
    }

    [Fact]
    public void A_successful_save_after_a_failed_one_clears_the_error()
    {
        _viewModel.HandleKey(OverlayKey.F2);
        Row.EditText = "call Bob";
        _fixture.Repository.Fail = true;
        _viewModel.HandleKey(OverlayKey.Enter);
        _fixture.Repository.Fail = false;

        _viewModel.HandleKey(OverlayKey.Enter);

        Assert.Empty(_viewModel.ErrorText);
        Assert.False(_viewModel.IsEditing);
        Assert.Equal("call Bob", Assert.Single(_fixture.Repository.Tasks).Title);
    }

    [Fact]
    public void Editing_notifies_that_the_hints_changed()
    {
        var changed = new List<string?>();
        _viewModel.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        _viewModel.HandleKey(OverlayKey.F2);

        Assert.Contains(nameof(OverlayViewModel.IsEditing), changed);
        Assert.Contains(nameof(OverlayViewModel.EnterHint), changed);
        Assert.Contains(nameof(OverlayViewModel.EscapeHint), changed);
        Assert.Contains(nameof(OverlayViewModel.ShowEditHint), changed);
    }

    [Fact]
    public void Space_while_editing_is_left_to_the_editor()
    {
        _viewModel.HandleKey(OverlayKey.F2);

        Assert.False(_viewModel.HandleKey(OverlayKey.Space));

        Assert.True(_viewModel.IsEditing);
        Assert.Equal(TaskItemStatus.Open, Assert.Single(_fixture.Repository.Tasks).Status);
    }

    [Fact]
    public void Enter_on_a_line_without_a_title_keeps_editing()
    {
        _viewModel.HandleKey(OverlayKey.F2);
        Row.EditText = "#work";

        _viewModel.HandleKey(OverlayKey.Enter);

        Assert.True(_viewModel.IsEditing);
        Assert.Equal("call Anna", Assert.Single(_fixture.Repository.Tasks).Title);
    }

    [Fact]
    public void Escape_cancels_the_edit_instead_of_closing()
    {
        int closeRequests = 0;
        _viewModel.CloseRequested += (_, _) => closeRequests++;
        _viewModel.HandleKey(OverlayKey.F2);
        Row.EditText = "call Bob";

        _viewModel.HandleKey(OverlayKey.Escape);

        Assert.False(_viewModel.IsEditing);
        Assert.False(Row.IsEditing);
        Assert.Equal("call Anna", Assert.Single(_fixture.Repository.Tasks).Title);
        Assert.Equal(0, closeRequests);
        Assert.Empty(_viewModel.Preview); // back to the (empty) input line
        Assert.Equal(OverlayFocus.Input, _viewModel.Focus);
    }

    [Theory]
    [InlineData(OverlayKey.Down)]
    [InlineData(OverlayKey.Up)]
    [InlineData(OverlayKey.Tab)]
    [InlineData(OverlayKey.F2)]
    public void Navigation_keys_are_blocked_while_editing(OverlayKey key)
    {
        _viewModel.HandleKey(OverlayKey.F2);

        Assert.True(_viewModel.HandleKey(key));

        Assert.True(_viewModel.IsEditing);
        Assert.Equal(0, _viewModel.SelectedIndex);
    }

    [Fact]
    public void A_failed_save_shows_an_error_and_keeps_editing()
    {
        _viewModel.HandleKey(OverlayKey.F2);
        Row.EditText = "call Bob";
        _fixture.Repository.Fail = true;

        _viewModel.HandleKey(OverlayKey.Enter);

        Assert.StartsWith("Could not update the task.", _viewModel.ErrorText);
        Assert.True(_viewModel.IsEditing);
        Assert.Equal("call Bob", Row.EditText);
    }

    [Fact]
    public void Changing_the_edit_after_a_failed_save_clears_the_error()
    {
        _viewModel.HandleKey(OverlayKey.F2);
        Row.EditText = "call Bob";
        _fixture.Repository.Fail = true;
        _viewModel.HandleKey(OverlayKey.Enter);

        Row.EditText = "call Bob now";

        Assert.Empty(_viewModel.ErrorText);
        Assert.True(_viewModel.IsEditing);
    }

    [Fact]
    public void Refresh_ends_an_edit_in_progress()
    {
        _viewModel.HandleKey(OverlayKey.F2);

        _viewModel.Refresh();

        Assert.False(_viewModel.IsEditing);
    }

    [Fact]
    public void CancelEdit_without_an_edit_does_nothing()
    {
        _viewModel.CancelEdit();

        Assert.False(_viewModel.IsEditing);
        Assert.Equal(0, _viewModel.SelectedIndex);
    }
}
