using Microsoft.Extensions.Time.Testing;
using Memento.Core.Tasks.Models;
using Memento.Core.UnitTests.TestSupport;
using Memento.Overlay.Tasks.Services;
using Memento.Overlay.Tasks.ViewModels;

namespace Memento.UnitTests.Overlay.Tasks.ViewModels;

public class TaskRowViewModelTests
{
    private readonly DeadlineFormatter _deadlines = new(new FakeTimeProvider(TestClock.NowOffset));

    private TaskRowViewModel Row(DateTime? deadline = null, params string[] tags) =>
        new(new TaskItem("1", "call Anna", deadline, tags, TestClock.Now), _deadlines);

    [Fact]
    public void The_row_shows_the_title_of_its_task()
    {
        Assert.Equal("call Anna", Row().Title);
    }

    [Fact]
    public void The_row_exposes_the_task_it_was_made_from()
    {
        var item = new TaskItem("1", "call Anna", null, [], TestClock.Now);

        Assert.Same(item, new TaskRowViewModel(item, _deadlines).Item);
    }

    [Fact]
    public void Without_tags_the_tags_text_is_empty()
    {
        Assert.Equal("", Row().TagsText);
    }

    [Fact]
    public void One_tag_is_shown_with_a_hash()
    {
        Assert.Equal("#work", Row(null, "work").TagsText);
    }

    [Fact]
    public void Several_tags_are_shown_with_a_hash_and_two_spaces_between_them()
    {
        Assert.Equal("#work  #home  #money", Row(null, "work", "home", "money").TagsText);
    }

    [Fact]
    public void Without_a_deadline_the_deadline_text_is_empty()
    {
        Assert.Equal("", Row().DeadlineText);
    }

    [Fact]
    public void A_deadline_is_formatted_by_the_deadline_formatter()
    {
        Assert.Equal("tomorrow 09:00", Row(TestClock.At("2026-10-14T09:00")).DeadlineText);
    }

    [Fact]
    public void Without_a_deadline_the_row_is_never_overdue()
    {
        Assert.False(Row().IsOverdue);
    }

    [Theory]
    [InlineData("2026-10-13T09:59", true)]
    [InlineData("2026-10-13T10:00", false)]
    [InlineData("2026-10-13T10:01", false)]
    public void A_row_is_overdue_only_when_its_deadline_is_before_now(string deadline, bool expected)
    {
        Assert.Equal(expected, Row(TestClock.At(deadline)).IsOverdue);
    }

    [Fact]
    public void A_new_row_is_not_being_edited_and_has_no_edit_text()
    {
        var row = Row();

        Assert.False(row.IsEditing);
        Assert.Equal("", row.EditText);
    }

    [Fact]
    public void Changing_IsEditing_raises_PropertyChanged_once()
    {
        var row = Row();
        var raised = new List<string?>();
        row.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        row.IsEditing = true;
        row.IsEditing = true;

        Assert.Equal(new[] { nameof(TaskRowViewModel.IsEditing) }, raised);
    }

    [Fact]
    public void Changing_EditText_raises_PropertyChanged_once()
    {
        var row = Row();
        var raised = new List<string?>();
        row.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        row.EditText = "call Bob";
        row.EditText = "call Bob";

        Assert.Equal(new[] { nameof(TaskRowViewModel.EditText) }, raised);
        Assert.Equal("call Bob", row.EditText);
    }
}
