using Microsoft.Extensions.Time.Testing;
using Memento.Core.Enumerations;
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
    public void Capture_without_a_title_saves_nothing()
    {
        Assert.Equal(TaskChange.MissingTitle, _service.Capture("#work tomorrow"));
        Assert.Empty(_repository.Tasks);
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
}
