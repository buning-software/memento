using Microsoft.Data.Sqlite;
using Memento.Core.Enumerations;
using Memento.Core.Storage;
using Memento.Core.Tasks.Models;
using Memento.Core.Tasks.Persistence;
using Memento.EntityFramework.UnitTests.TestSupport;
using static Memento.Core.UnitTests.TestSupport.TestClock;

namespace Memento.EntityFramework.UnitTests.Repositories;

public sealed class TaskRepositoryTests : IDisposable
{
    private readonly TempDatabase _database = new();
    private readonly ITaskRepository _repository;

    public TaskRepositoryTests()
    {
        _database.Initializer.Initialize();
        _repository = _database.Repository;
    }

    public void Dispose() => _database.Dispose();

    private static TaskItem NewTask(string title, DateTime? deadline = null, DateTime? createdAt = null, params string[] tags) =>
        new(Guid.NewGuid().ToString(), title, deadline, tags, createdAt ?? Now, OriginalText: title);

    [Fact]
    public void An_added_task_comes_back_with_every_field()
    {
        var task = new TaskItem("id-1", "call Anna", new DateTime(2026, 10, 14, 15, 0, 0), ["work", "phone"], Now,
            TaskItemStatus.Open, "call Anna tomorrow 3pm #work #phone");

        _repository.Add(task);

        var stored = Assert.Single(_repository.GetOpen(10));
        Assert.Equal(task.Id, stored.Id);
        Assert.Equal(task.Title, stored.Title);
        Assert.Equal(task.Deadline, stored.Deadline);
        Assert.Equal(task.Tags, stored.Tags);
        Assert.Equal(task.CreatedAt, stored.CreatedAt);
        Assert.Equal(TaskItemStatus.Open, stored.Status);
        Assert.Equal(task.OriginalText, stored.OriginalText);
    }

    [Fact]
    public void A_task_without_deadline_or_tags_comes_back_without_them()
    {
        _repository.Add(NewTask("buy milk"));

        var stored = Assert.Single(_repository.GetOpen(10));
        Assert.Null(stored.Deadline);
        Assert.Empty(stored.Tags);
    }

    [Fact]
    public void Open_tasks_are_ordered_by_deadline_then_undated_newest_first()
    {
        _repository.Add(NewTask("later", deadline: new DateTime(2026, 10, 15, 9, 0, 0)));
        _repository.Add(NewTask("sooner", deadline: new DateTime(2026, 10, 14, 9, 0, 0)));
        _repository.Add(NewTask("undated old", createdAt: Now.AddHours(-1)));
        _repository.Add(NewTask("undated new", createdAt: Now));

        var titles = _repository.GetOpen(10).Select(t => t.Title);

        Assert.Equal(new[] { "sooner", "later", "undated new", "undated old" }, titles);
    }

    [Fact]
    public void Open_tasks_with_the_same_deadline_are_newest_first_even_within_a_second()
    {
        var deadline = new DateTime(2026, 10, 14, 9, 0, 0);
        _repository.Add(NewTask("old", deadline, createdAt: Now));
        _repository.Add(NewTask("newer", deadline, createdAt: Now.AddMilliseconds(300)));
        _repository.Add(NewTask("newest", deadline, createdAt: Now.AddMilliseconds(600)));

        var titles = _repository.GetOpen(10).Select(t => t.Title);

        Assert.Equal(new[] { "newest", "newer", "old" }, titles);
    }

    [Fact]
    public void GetOpen_returns_at_most_the_limit()
    {
        for (int i = 0; i < 5; i++)
            _repository.Add(NewTask($"task {i}"));

        Assert.Equal(3, _repository.GetOpen(3).Count);
    }

    [Fact]
    public void A_completed_task_is_no_longer_open()
    {
        var task = NewTask("done soon");
        _repository.Add(task);

        _repository.SetStatus(task.Id, TaskItemStatus.Completed);

        Assert.Empty(_repository.GetOpen(10));
        Assert.Equal((long)TaskItemStatus.Completed, _database.Scalar($"SELECT Status FROM Tasks WHERE Id = '{task.Id}';"));
    }

    [Fact]
    public void Update_replaces_title_deadline_tags_and_original_text()
    {
        var task = NewTask("call Anna", new DateTime(2026, 10, 14, 15, 0, 0), null, "work");
        _repository.Add(task);

        _repository.Update(task with { Title = "call Bob", Deadline = null, Tags = [], OriginalText = "call Bob" });

        var stored = Assert.Single(_repository.GetOpen(10));
        Assert.Equal("call Bob", stored.Title);
        Assert.Null(stored.Deadline);
        Assert.Empty(stored.Tags);
        Assert.Equal("call Bob", stored.OriginalText);
        Assert.Equal(task.CreatedAt, stored.CreatedAt);
    }

    [Fact]
    public void Update_stores_a_new_deadline()
    {
        var task = NewTask("call Anna");
        _repository.Add(task);
        var deadline = new DateTime(2026, 10, 16, 14, 30, 0);

        _repository.Update(task with { Deadline = deadline });

        Assert.Equal(deadline, Assert.Single(_repository.GetOpen(10)).Deadline);
    }

    [Fact]
    public void Update_and_SetStatus_of_an_unknown_id_change_nothing()
    {
        var task = NewTask("call Anna");
        _repository.Add(task);

        _repository.Update(NewTask("unknown"));
        _repository.SetStatus("unknown", TaskItemStatus.Completed);

        Assert.Equal(task, Assert.Single(_repository.GetOpen(10)) with { Tags = task.Tags });
    }

    [Fact]
    public void A_database_failure_surfaces_as_a_storage_exception()
    {
        var task = NewTask("twice");
        _repository.Add(task);

        var error = Assert.Throws<StorageException>(() => _repository.Add(task)); // duplicate primary key

        Assert.IsType<SqliteException>(error.GetBaseException());
    }
}
