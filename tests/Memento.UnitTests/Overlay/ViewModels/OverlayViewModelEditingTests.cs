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
