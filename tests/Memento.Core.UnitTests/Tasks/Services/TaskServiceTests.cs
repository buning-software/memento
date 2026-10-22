using Microsoft.Extensions.Time.Testing;
using Memento.Core.Enumerations;
using Memento.Core.Storage;
using Memento.Core.Tasks.Models;
using Memento.Core.Tasks.Services;
using Memento.Core.UnitTests.TestSupport;
using static Memento.Core.UnitTests.TestSupport.TestClock;

namespace Memento.Core.UnitTests.Tasks.Services;

public class TaskServiceTests
{
    private readonly InMemoryTaskRepository _repository = new();
    private readonly InMemorySettingsStore _settings = new();
    private readonly FakeTimeProvider _time = new(NowOffset);
    private readonly TaskService _service;

    public TaskServiceTests() => _service = new TaskService(_repository, _settings, _time);

    [Fact]
    public void Capture_saves_the_parsed_task_with_the_line_as_typed()
    {
        var change = _service.Capture("  call Anna tomorrow 3pm #work ");

        Assert.Equal(TaskChange.Saved, change);
        var task = Assert.Single(_repository.Tasks);
        Assert.True(Guid.TryParse(task.Id, out _));
        Assert.Equal("call Anna", task.Title);
        Assert.Equal(At("2026-10-14T15:00"), task.Deadline);
        Assert.Equal(new[] { "work" }, task.Tags);
        Assert.Equal(Now, task.CreatedAt);
        Assert.Equal(TaskItemStatus.Open, task.Status);
        Assert.Equal("call Anna tomorrow 3pm #work", task.OriginalText);
    }

    [Fact]
    public void Capture_uses_the_default_time_from_the_settings()
    {
        _settings.Update(s => s with { DefaultTime = new TimeOnly(8, 30) });

        _service.Capture("x tomorrow");

        Assert.Equal(At("2026-10-14T08:30"), Assert.Single(_repository.Tasks).Deadline);
    }

    [Fact]
    public void Preview_parses_without_saving()
    {
        var preview = _service.Preview("x tomorrow");

        Assert.Equal(At("2026-10-14T09:00"), preview.Deadline);
        Assert.Empty(_repository.Tasks);
    }

    [Fact]
    public void Edit_with_new_text_reparses_and_keeps_id_and_creation_time()
    {
        _service.Capture("call Anna tomorrow");
        var original = _repository.Tasks[0];

        var change = _service.Edit(original, "call Bob friday #home");

        Assert.Equal(TaskChange.Saved, change);
        var edited = _repository.Tasks[0];
        Assert.Equal(original.Id, edited.Id);
        Assert.Equal(original.CreatedAt, edited.CreatedAt);
        Assert.Equal("call Bob", edited.Title);
        Assert.Equal(At("2026-10-16T09:00"), edited.Deadline);
        Assert.Equal(new[] { "home" }, edited.Tags);
        Assert.Equal("call Bob friday #home", edited.OriginalText);
    }

    [Fact]
    public void Edit_with_unchanged_text_keeps_the_task_as_is()
    {
        _service.Capture("x tomorrow");       // on Tuesday: due Wednesday
        _time.Advance(TimeSpan.FromDays(1));  // now it is Wednesday
        var task = _repository.Tasks[0];

        var change = _service.Edit(task, " x tomorrow ");

        Assert.Equal(TaskChange.Unchanged, change);
        Assert.Equal(At("2026-10-14T09:00"), _repository.Tasks[0].Deadline);
    }

    [Fact]
    public void Edit_to_a_line_without_a_title_is_refused()
    {
        _service.Capture("call Anna");
        var task = _repository.Tasks[0];

        Assert.Equal(TaskChange.MissingTitle, _service.Edit(task, "#work"));
        Assert.Equal("call Anna", _repository.Tasks[0].Title);
    }

    [Fact]
    public void Complete_removes_the_task_from_the_open_list()
    {
        _service.Capture("call Anna");

        _service.Complete(_repository.Tasks[0].Id);

        Assert.Empty(_service.GetOpen(10));
        Assert.Equal(TaskItemStatus.Completed, _repository.Tasks[0].Status);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(" \t ")]
    [InlineData("#work")]
    [InlineData("#work tomorrow")]
    [InlineData("tomorrow 15:00")]
    public void Capture_of_a_line_without_a_title_saves_nothing(string text)
    {
        Assert.Equal(TaskChange.MissingTitle, _service.Capture(text));
        Assert.Empty(_repository.Tasks);
    }

    [Fact]
    public void Capture_of_a_line_without_a_title_does_not_touch_a_broken_storage()
    {
        _repository.Fail = true;

        Assert.Equal(TaskChange.MissingTitle, _service.Capture("   "));
    }

    [Fact]
    public void Capture_of_a_plain_line_has_no_deadline_and_no_tags()
    {
        _service.Capture("buy milk");

        var task = Assert.Single(_repository.Tasks);
        Assert.Null(task.Deadline);
        Assert.Empty(task.Tags);
    }

    [Fact]
    public void Capture_gives_every_task_its_own_id()
    {
        _service.Capture("one");
        _service.Capture("two");

        Assert.NotEqual(_repository.Tasks[0].Id, _repository.Tasks[1].Id);
    }

    [Fact]
    public void Capture_stamps_the_creation_time_from_the_clock()
    {
        _time.Advance(TimeSpan.FromHours(5));

        _service.Capture("late one");

        Assert.Equal(Now.AddHours(5), Assert.Single(_repository.Tasks).CreatedAt);
    }

    [Fact]
    public void Capture_with_broken_storage_throws_and_saves_nothing()
    {
        _repository.Fail = true;

        Assert.Throws<StorageException>(() => _service.Capture("call Anna"));

        _repository.Fail = false;
        Assert.Empty(_repository.Tasks);
    }

    [Fact]
    public void Complete_with_broken_storage_throws_and_leaves_the_task_open()
    {
        _service.Capture("call Anna");
        string id = _repository.Tasks[0].Id;
        _repository.Fail = true;

        Assert.Throws<StorageException>(() => _service.Complete(id));

        _repository.Fail = false;
        Assert.Equal(TaskItemStatus.Open, _repository.Tasks[0].Status);
    }

    [Fact]
    public void Edit_with_broken_storage_throws_and_keeps_the_task()
    {
        _service.Capture("call Anna");
        var task = _repository.Tasks[0];
        _repository.Fail = true;

        Assert.Throws<StorageException>(() => _service.Edit(task, "call Bob"));

        _repository.Fail = false;
        Assert.Equal("call Anna", _repository.Tasks[0].Title);
    }

    [Fact]
    public void Preview_and_unchanged_or_title_less_edits_work_with_broken_storage()
    {
        _service.Capture("call Anna #work");
        var task = _repository.Tasks[0];
        _repository.Fail = true;

        Assert.Equal("call Anna", _service.Preview("call Anna #work").Title);
        Assert.Equal(TaskChange.Unchanged, _service.Edit(task, "call Anna #work"));
        Assert.Equal(TaskChange.MissingTitle, _service.Edit(task, "  "));
    }

    [Theory]
    [InlineData("call Anna #work", "call Anna #home")]
    [InlineData("call Anna #work", "call Anna")]
    [InlineData("call Anna", "call Anna #work")]
    [InlineData("call Anna 14:00", "call Anna 15:00")]
    [InlineData("call Anna", "call Anna 15:00")]
    [InlineData("call Anna 15:00", "call Anna")]
    [InlineData("call Anna", "Call Anna")]
    public void Edit_that_changes_only_one_part_is_saved(string captured, string edited)
    {
        _service.Capture(captured);
        var task = _repository.Tasks[0];

        var change = _service.Edit(task, edited);

        Assert.Equal(TaskChange.Saved, change);
        Assert.Equal(edited, _repository.Tasks[0].OriginalText);
    }

    [Fact]
    public void Edit_changing_only_the_tags_keeps_title_and_deadline()
    {
        _service.Capture("call Anna 14:00 #work");
        var task = _repository.Tasks[0];

        _service.Edit(task, "call Anna 14:00 #home");

        var edited = _repository.Tasks[0];
        Assert.Equal("call Anna", edited.Title);
        Assert.Equal(task.Deadline, edited.Deadline);
        Assert.Equal(new[] { "home" }, edited.Tags);
    }

    [Fact]
    public void Edit_removing_the_deadline_clears_it()
    {
        _service.Capture("call Anna tomorrow");
        var task = _repository.Tasks[0];

        _service.Edit(task, "call Anna");

        Assert.Null(_repository.Tasks[0].Deadline);
    }

    [Fact]
    public void Edit_of_a_whitespace_only_line_is_refused()
    {
        _service.Capture("call Anna");
        var task = _repository.Tasks[0];

        Assert.Equal(TaskChange.MissingTitle, _service.Edit(task, "   "));
        Assert.Equal("call Anna", _repository.Tasks[0].Title);
    }

    [Fact]
    public void Edit_of_a_task_without_stored_text_compares_against_the_rebuilt_edit_text()
    {
        var stored = new TaskItem("1", "pay rent", At("2026-10-14T16:45"), ["home"], Now);
        _repository.Tasks.Add(stored);

        Assert.Equal(TaskChange.Unchanged, _service.Edit(stored, stored.EditText));
        Assert.Equal(stored, _repository.Tasks[0]);
    }

    [Fact]
    public void Edit_of_a_task_without_stored_text_saves_the_new_line_as_its_text()
    {
        var stored = new TaskItem("1", "pay rent", At("2026-10-14T16:45"), ["home"], Now);
        _repository.Tasks.Add(stored);

        var change = _service.Edit(stored, "pay rent 2026-10-15 16:45 #home");

        Assert.Equal(TaskChange.Saved, change);
        Assert.Equal(At("2026-10-15T16:45"), _repository.Tasks[0].Deadline);
        Assert.Equal("pay rent 2026-10-15 16:45 #home", _repository.Tasks[0].OriginalText);
    }

    [Fact]
    public void Complete_of_an_unknown_id_does_nothing()
    {
        _service.Capture("call Anna");

        _service.Complete("no-such-id");

        Assert.Equal(TaskItemStatus.Open, Assert.Single(_repository.Tasks).Status);
    }

    [Fact]
    public void GetOpen_is_empty_without_tasks()
    {
        Assert.Empty(_service.GetOpen(10));
    }

    [Fact]
    public void GetOpen_returns_at_most_the_limit()
    {
        _service.Capture("one");
        _service.Capture("two");
        _service.Capture("three");

        Assert.Equal(2, _service.GetOpen(2).Count);
        Assert.Equal(3, _service.GetOpen(3).Count);
        Assert.Equal(3, _service.GetOpen(100).Count);
        Assert.Empty(_service.GetOpen(0));
    }

    [Fact]
    public void GetOpen_leaves_out_completed_tasks_and_keeps_the_others()
    {
        _service.Capture("done one");
        _service.Capture("open one");
        _service.Complete(_repository.Tasks[0].Id);

        Assert.Equal("open one", Assert.Single(_service.GetOpen(10)).Title);
    }

    [Fact]
    public void Preview_follows_a_change_of_the_default_time_in_the_settings()
    {
        Assert.Equal(At("2026-10-14T09:00"), _service.Preview("x tomorrow").Deadline);

        _settings.Update(s => s with { DefaultTime = new TimeOnly(7, 15) });

        Assert.Equal(At("2026-10-14T07:15"), _service.Preview("x tomorrow").Deadline);
    }

    [Fact]
    public void Preview_follows_the_clock()
    {
        _time.Advance(TimeSpan.FromDays(3));

        Assert.Equal(At("2026-10-17T09:00"), _service.Preview("x tomorrow").Deadline);
    }
}
