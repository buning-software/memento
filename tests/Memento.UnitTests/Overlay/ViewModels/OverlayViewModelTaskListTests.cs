using Memento.Core.Enumerations;
using Memento.Enumerations;
using Memento.Overlay.Tasks.Models;
using Memento.UnitTests.TestSupport;

namespace Memento.UnitTests.Overlay.ViewModels;

public class OverlayViewModelTaskListTests
{
    private readonly OverlayFixture _fixture = new();

    [Fact]
    public void Refresh_lists_the_open_tasks()
    {
        _fixture.AddTask("first");
        _fixture.AddTask("second");

        var viewModel = _fixture.CreateViewModel();

        Assert.True(viewModel.HasTasks);
        Assert.Equal(new[] { "first", "second" }, viewModel.Tasks.Select(row => row.Title));
    }

    [Fact]
    public void Without_tasks_the_list_is_hidden()
    {
        Assert.False(_fixture.CreateViewModel().HasTasks);
    }

    [Fact]
    public void A_row_shows_its_deadline_tags_and_whether_it_is_overdue()
    {
        _fixture.AddTask("report", new DateTime(2026, 10, 13, 9, 0, 0), "work", "q4");

        var row = Assert.Single(_fixture.CreateViewModel().Tasks);

        Assert.Equal("today 09:00", row.DeadlineText);
        Assert.Equal("#work  #q4", row.TagsText);
        Assert.True(row.IsOverdue);
    }

    [Fact]
    public void A_storage_failure_while_listing_hides_the_list_without_an_error()
    {
        _fixture.AddTask("unreachable");
        _fixture.Repository.Fail = true;

        var viewModel = _fixture.CreateViewModel();

        Assert.False(viewModel.HasTasks);
        Assert.Empty(viewModel.ErrorText);
    }

    [Fact]
    public void Typing_shows_the_deadline_and_tags_as_badges()
    {
        var viewModel = _fixture.CreateViewModel();

        viewModel.InputText = "call Anna tomorrow 3pm #work";

        Assert.Equal(
            new[] { new PreviewBadge(PreviewBadgeKind.Deadline, "tomorrow 15:00"), new PreviewBadge(PreviewBadgeKind.Tag, "work") },
            viewModel.Preview);
        Assert.Null(viewModel.PreviewHint);
    }

    [Fact]
    public void Typing_only_a_deadline_asks_for_a_title()
    {
        var viewModel = _fixture.CreateViewModel();

        viewModel.InputText = "tomorrow";

        Assert.Equal("Add a title", viewModel.PreviewHint);
        Assert.Single(viewModel.Preview);
    }

    [Fact]
    public void Clearing_the_input_clears_the_preview()
    {
        var viewModel = _fixture.CreateViewModel();
        viewModel.InputText = "tomorrow #work";

        viewModel.InputText = "";

        Assert.Empty(viewModel.Preview);
        Assert.Null(viewModel.PreviewHint);
    }

    [Fact]
    public void Typing_returns_to_new_task_mode()
    {
        _fixture.AddTask("selected");
        var viewModel = _fixture.CreateViewModel();
        viewModel.SelectedIndex = 0;

        viewModel.InputText = "x";

        Assert.Equal(-1, viewModel.SelectedIndex);
    }

    [Fact]
    public void Enter_saves_the_typed_task_clears_the_input_and_lists_it()
    {
        var viewModel = _fixture.CreateViewModel();
        viewModel.InputText = "buy milk";

        Assert.True(viewModel.HandleKey(OverlayKey.Enter));

        Assert.Equal("buy milk", Assert.Single(_fixture.Repository.Tasks).Title);
        Assert.Equal("", viewModel.InputText);
        Assert.Equal("buy milk", Assert.Single(viewModel.Tasks).Title);
    }

    [Fact]
    public void Enter_on_a_line_without_a_title_saves_nothing_and_keeps_the_text()
    {
        var viewModel = _fixture.CreateViewModel();
        viewModel.InputText = "#work";

        viewModel.HandleKey(OverlayKey.Enter);

        Assert.Empty(_fixture.Repository.Tasks);
        Assert.Equal("#work", viewModel.InputText);
    }

    [Fact]
    public void Failed_capture_keeps_the_text_and_shows_an_error()
    {
        var viewModel = _fixture.CreateViewModel();
        viewModel.InputText = "buy milk";
        _fixture.Repository.Fail = true;

        viewModel.HandleKey(OverlayKey.Enter);

        Assert.Equal("buy milk", viewModel.InputText);
        Assert.StartsWith("Could not save the task.", viewModel.ErrorText);
    }

    [Fact]
    public void Typing_after_a_failed_capture_clears_the_error()
    {
        var viewModel = _fixture.CreateViewModel();
        viewModel.InputText = "buy milk";
        _fixture.Repository.Fail = true;
        viewModel.HandleKey(OverlayKey.Enter);

        viewModel.InputText = "buy milk!";

        Assert.Empty(viewModel.ErrorText);
    }

    [Fact]
    public void A_successful_capture_after_a_failed_one_clears_the_error()
    {
        var viewModel = _fixture.CreateViewModel();
        viewModel.InputText = "buy milk";
        _fixture.Repository.Fail = true;
        viewModel.HandleKey(OverlayKey.Enter);
        _fixture.Repository.Fail = false;

        viewModel.HandleKey(OverlayKey.Enter);

        Assert.Empty(viewModel.ErrorText);
        Assert.Single(_fixture.Repository.Tasks);
    }

    [Fact]
    public void Enter_on_a_selected_task_completes_it_and_keeps_the_selection_in_place()
    {
        _fixture.AddTask("one");
        var two = _fixture.AddTask("two");
        _fixture.AddTask("three");
        var viewModel = _fixture.CreateViewModel();
        viewModel.SelectedIndex = 1;

        viewModel.HandleKey(OverlayKey.Enter);

        Assert.Equal(TaskItemStatus.Completed, _fixture.Repository.Tasks.Single(t => t.Id == two.Id).Status);
        Assert.Equal(new[] { "one", "three" }, viewModel.Tasks.Select(row => row.Title));
        Assert.Equal(1, viewModel.SelectedIndex);
    }

    [Fact]
    public void Completing_the_last_row_selects_the_row_above()
    {
        _fixture.AddTask("one");
        _fixture.AddTask("two");
        var viewModel = _fixture.CreateViewModel();
        viewModel.SelectedIndex = 1;

        viewModel.HandleKey(OverlayKey.Enter);

        Assert.Equal(0, viewModel.SelectedIndex);
    }

    [Fact]
    public void Completing_the_only_task_leaves_no_selection()
    {
        _fixture.AddTask("only");
        var viewModel = _fixture.CreateViewModel();
        viewModel.SelectedIndex = 0;

        viewModel.HandleKey(OverlayKey.Enter);

        Assert.Equal(-1, viewModel.SelectedIndex);
        Assert.False(viewModel.HasTasks);
    }

    [Fact]
    public void Failed_completion_shows_an_error_and_keeps_the_task()
    {
        _fixture.AddTask("stuck");
        var viewModel = _fixture.CreateViewModel();
        viewModel.SelectedIndex = 0;
        _fixture.Repository.Fail = true;

        viewModel.HandleKey(OverlayKey.Enter);

        Assert.StartsWith("Could not update the task.", viewModel.ErrorText);
        Assert.Single(viewModel.Tasks);
    }

    [Fact]
    public void A_successful_completion_after_a_failed_one_clears_the_error()
    {
        _fixture.AddTask("stuck");
        var viewModel = _fixture.CreateViewModel();
        viewModel.SelectedIndex = 0;
        _fixture.Repository.Fail = true;
        viewModel.HandleKey(OverlayKey.Enter);
        _fixture.Repository.Fail = false;

        viewModel.HandleKey(OverlayKey.Enter);

        Assert.Empty(viewModel.ErrorText);
        Assert.False(viewModel.HasTasks);
    }
}
