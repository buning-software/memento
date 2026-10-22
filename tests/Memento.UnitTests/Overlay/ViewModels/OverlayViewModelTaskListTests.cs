using Memento.Core.Enumerations;
using Memento.Enumerations;
using Memento.Overlay.Tasks.Models;
using Memento.Overlay.ViewModels;
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

    [Theory]
    [InlineData(49, 49)]
    [InlineData(50, 50)]
    [InlineData(51, 50)]
    [InlineData(80, 50)]
    public void The_list_shows_at_most_the_maximum_number_of_tasks(int tasks, int listed)
    {
        for (int i = 0; i < tasks; i++)
            _fixture.AddTask($"task {i}");

        Assert.Equal(listed, _fixture.CreateViewModel().Tasks.Count);
    }

    [Fact]
    public void A_task_beyond_the_maximum_appears_once_a_listed_one_is_completed()
    {
        for (int i = 0; i < OverlayViewModel.MaxListedTasks + 1; i++)
            _fixture.AddTask($"task {i}", new DateTime(2026, 10, 14, 9, 0, 0).AddMinutes(i));
        var viewModel = _fixture.CreateViewModel();
        Assert.DoesNotContain(viewModel.Tasks, row => row.Title == "task 50");
        viewModel.SelectedIndex = 0;

        viewModel.HandleKey(OverlayKey.Enter);

        Assert.Equal(OverlayViewModel.MaxListedTasks, viewModel.Tasks.Count);
        Assert.Contains(viewModel.Tasks, row => row.Title == "task 50");
    }

    [Fact]
    public void The_list_is_ordered_by_deadline_with_tasks_without_one_last()
    {
        _fixture.AddTask("no deadline");
        _fixture.AddTask("later", new DateTime(2026, 10, 15, 9, 0, 0));
        _fixture.AddTask("sooner", new DateTime(2026, 10, 14, 9, 0, 0));

        var viewModel = _fixture.CreateViewModel();

        Assert.Equal(new[] { "sooner", "later", "no deadline" }, viewModel.Tasks.Select(row => row.Title));
    }

    [Fact]
    public void Refresh_picks_up_new_tasks_and_drops_the_selection()
    {
        _fixture.AddTask("first");
        var viewModel = _fixture.CreateViewModel();
        viewModel.SelectedIndex = 0;
        _fixture.AddTask("second");

        viewModel.Refresh();

        Assert.Equal(2, viewModel.Tasks.Count);
        Assert.Equal(-1, viewModel.SelectedIndex);
    }

    [Fact]
    public void Refresh_notifies_that_the_list_visibility_may_have_changed()
    {
        var viewModel = _fixture.CreateViewModel();
        var changed = new List<string?>();
        viewModel.PropertyChanged += (_, e) => changed.Add(e.PropertyName);
        _fixture.AddTask("first");

        viewModel.Refresh();

        Assert.Contains(nameof(OverlayViewModel.HasTasks), changed);
    }

    [Fact]
    public void A_storage_failure_on_a_later_refresh_empties_the_list()
    {
        _fixture.AddTask("first");
        var viewModel = _fixture.CreateViewModel();
        _fixture.Repository.Fail = true;

        viewModel.Refresh();

        Assert.False(viewModel.HasTasks);
        Assert.Empty(viewModel.Tasks);
        Assert.Empty(viewModel.ErrorText);
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

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void A_blank_input_has_no_preview_and_no_hint(string input)
    {
        var viewModel = _fixture.CreateViewModel();

        viewModel.InputText = input;

        Assert.Empty(viewModel.Preview);
        Assert.Null(viewModel.PreviewHint);
    }

    [Fact]
    public void A_plain_title_has_no_badges_and_no_hint()
    {
        var viewModel = _fixture.CreateViewModel();

        viewModel.InputText = "buy milk";

        Assert.Empty(viewModel.Preview);
        Assert.Null(viewModel.PreviewHint);
    }

    [Fact]
    public void Typing_only_tags_asks_for_a_title_and_shows_every_tag()
    {
        var viewModel = _fixture.CreateViewModel();

        viewModel.InputText = "#work #home";

        Assert.Equal("Add a title", viewModel.PreviewHint);
        Assert.Equal(
            new[] { new PreviewBadge(PreviewBadgeKind.Tag, "work"), new PreviewBadge(PreviewBadgeKind.Tag, "home") },
            viewModel.Preview);
    }

    [Fact]
    public void Adding_a_title_removes_the_hint()
    {
        var viewModel = _fixture.CreateViewModel();
        viewModel.InputText = "tomorrow";

        viewModel.InputText = "tomorrow dentist";

        Assert.Null(viewModel.PreviewHint);
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

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Enter_on_a_blank_line_saves_nothing_and_shows_no_error(string input)
    {
        var viewModel = _fixture.CreateViewModel();
        viewModel.InputText = input;

        Assert.True(viewModel.HandleKey(OverlayKey.Enter));

        Assert.Empty(_fixture.Repository.Tasks);
        Assert.Empty(viewModel.ErrorText);
    }

    [Fact]
    public void Enter_saves_the_deadline_and_tags_of_the_typed_line()
    {
        var viewModel = _fixture.CreateViewModel();
        viewModel.InputText = "call Anna tomorrow 3pm #work";

        viewModel.HandleKey(OverlayKey.Enter);

        var row = Assert.Single(viewModel.Tasks);
        Assert.Equal("call Anna", row.Title);
        Assert.Equal("tomorrow 15:00", row.DeadlineText);
        Assert.Equal("#work", row.TagsText);
        Assert.Empty(viewModel.Preview);
    }

    [Fact]
    public void Failed_capture_lists_nothing_and_keeps_the_preview()
    {
        var viewModel = _fixture.CreateViewModel();
        viewModel.InputText = "buy milk #home";
        _fixture.Repository.Fail = true;

        viewModel.HandleKey(OverlayKey.Enter);

        Assert.False(viewModel.HasTasks);
        Assert.Single(viewModel.Preview);
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
    public void Space_on_a_selected_task_completes_it_like_enter()
    {
        _fixture.AddTask("one");
        var two = _fixture.AddTask("two");
        _fixture.AddTask("three");
        var viewModel = _fixture.CreateViewModel();
        viewModel.SelectedIndex = 1;

        Assert.True(viewModel.HandleKey(OverlayKey.Space));

        Assert.Equal(TaskItemStatus.Completed, _fixture.Repository.Tasks.Single(t => t.Id == two.Id).Status);
        Assert.Equal(new[] { "one", "three" }, viewModel.Tasks.Select(row => row.Title));
        Assert.Equal(1, viewModel.SelectedIndex);
    }

    [Fact]
    public void Space_without_a_selected_task_is_left_to_the_input()
    {
        _fixture.AddTask("one");
        var viewModel = _fixture.CreateViewModel();
        viewModel.InputText = "call";

        Assert.False(viewModel.HandleKey(OverlayKey.Space));

        Assert.Single(viewModel.Tasks);
        Assert.Equal("call", viewModel.InputText);
    }

    [Fact]
    public void Completing_the_first_row_keeps_the_selection_on_the_first_row()
    {
        _fixture.AddTask("one");
        _fixture.AddTask("two");
        var viewModel = _fixture.CreateViewModel();
        viewModel.SelectedIndex = 0;

        viewModel.HandleKey(OverlayKey.Enter);

        Assert.Equal(0, viewModel.SelectedIndex);
        Assert.Equal("two", Assert.Single(viewModel.Tasks).Title);
    }

    [Fact]
    public void Failed_completion_with_space_shows_an_error_and_keeps_the_task_selected()
    {
        _fixture.AddTask("stuck");
        var viewModel = _fixture.CreateViewModel();
        viewModel.SelectedIndex = 0;
        _fixture.Repository.Fail = true;

        Assert.True(viewModel.HandleKey(OverlayKey.Space));

        Assert.StartsWith("Could not update the task.", viewModel.ErrorText);
        Assert.Equal(0, viewModel.SelectedIndex);
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
