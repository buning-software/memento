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
    public void Without_tasks_nothing_is_open()
    {
        Assert.Empty(_repository.GetOpen(10));
    }

    [Fact]
    public void Dated_tasks_come_before_undated_ones_however_old_or_new()
    {
        _repository.Add(NewTask("undated newest", createdAt: Now.AddDays(1)));
        _repository.Add(NewTask("dated far away", deadline: new DateTime(2099, 12, 31, 23, 59, 0), createdAt: Now.AddDays(-30)));

        var titles = _repository.GetOpen(10).Select(t => t.Title);

        Assert.Equal(new[] { "dated far away", "undated newest" }, titles);
    }

    [Fact]
    public void Undated_tasks_with_the_same_creation_time_are_all_listed()
    {
        _repository.Add(NewTask("one"));
        _repository.Add(NewTask("two"));

        Assert.Equal(2, _repository.GetOpen(10).Count);
    }

    [Fact]
    public void Open_tasks_are_ordered_by_deadline_then_newest_when_dated_and_undated_are_mixed()
    {
        var deadline = new DateTime(2026, 10, 14, 9, 0, 0);
        _repository.Add(NewTask("undated", createdAt: Now.AddHours(2)));
        _repository.Add(NewTask("tie old", deadline, Now.AddHours(-2)));
        _repository.Add(NewTask("earlier", deadline.AddHours(-1), Now.AddHours(-5)));
        _repository.Add(NewTask("tie new", deadline, Now.AddHours(-1)));

        var titles = _repository.GetOpen(10).Select(t => t.Title);

        Assert.Equal(new[] { "earlier", "tie new", "tie old", "undated" }, titles);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(5, 5)]
    [InlineData(100, 5)]
    public void GetOpen_returns_at_most_the_limit_and_never_more_than_exist(int limit, int expected)
    {
        for (int i = 0; i < 5; i++)
            _repository.Add(NewTask($"task {i}"));

        Assert.Equal(expected, _repository.GetOpen(limit).Count);
    }

    [Fact]
    public void The_limit_keeps_the_first_tasks_in_listing_order()
    {
        _repository.Add(NewTask("third", deadline: new DateTime(2026, 10, 16, 9, 0, 0)));
        _repository.Add(NewTask("first", deadline: new DateTime(2026, 10, 14, 9, 0, 0)));
        _repository.Add(NewTask("second", deadline: new DateTime(2026, 10, 15, 9, 0, 0)));

        var titles = _repository.GetOpen(2).Select(t => t.Title);

        Assert.Equal(new[] { "first", "second" }, titles);
    }

    [Fact]
    public void Deleted_tasks_are_not_open_either()
    {
        var task = NewTask("gone");
        _repository.Add(task);

        _repository.SetStatus(task.Id, TaskItemStatus.Deleted);

        Assert.Empty(_repository.GetOpen(10));
    }

    [Fact]
    public void A_task_set_back_to_open_is_listed_again()
    {
        var task = NewTask("again");
        _repository.Add(task);
        _repository.SetStatus(task.Id, TaskItemStatus.Completed);

        _repository.SetStatus(task.Id, TaskItemStatus.Open);

        Assert.Equal("again", Assert.Single(_repository.GetOpen(10)).Title);
    }

    [Fact]
    public void A_task_added_as_completed_is_not_open()
    {
        _repository.Add(new TaskItem("id-1", "done", null, [], Now, TaskItemStatus.Completed));

        Assert.Empty(_repository.GetOpen(10));
    }

    [Fact]
    public void SetStatus_changes_only_the_status()
    {
        var task = new TaskItem("id-1", "call Anna", new DateTime(2026, 10, 14, 15, 0, 0), ["work"], Now, OriginalText: "call Anna 3pm #work");
        _repository.Add(task);

        _repository.SetStatus(task.Id, TaskItemStatus.Completed);

        Assert.Equal("call Anna", _database.Scalar("SELECT Title FROM Tasks WHERE Id = 'id-1';"));
        Assert.Equal("work", _database.Scalar("SELECT Tags FROM Tasks WHERE Id = 'id-1';"));
        Assert.Equal("call Anna 3pm #work", _database.Scalar("SELECT OriginalText FROM Tasks WHERE Id = 'id-1';"));
        Assert.Equal(1, _database.Count("SELECT COUNT(*) FROM Tasks WHERE Id = 'id-1' AND Deadline IS NOT NULL;"));
    }

    [Fact]
    public void Update_leaves_the_status_alone()
    {
        var task = NewTask("call Anna");
        _repository.Add(task);
        _repository.SetStatus(task.Id, TaskItemStatus.Completed);

        _repository.Update(task with { Title = "call Bob" });

        Assert.Equal((long)TaskItemStatus.Completed, _database.Scalar($"SELECT Status FROM Tasks WHERE Id = '{task.Id}';"));
        Assert.Empty(_repository.GetOpen(10));
    }

    [Fact]
    public void Update_changes_only_the_task_with_that_id()
    {
        var first = NewTask("first");
        var second = NewTask("second");
        _repository.Add(first);
        _repository.Add(second);

        _repository.Update(first with { Title = "renamed" });

        var stored = _repository.GetOpen(10).ToDictionary(t => t.Id, t => t.Title);
        Assert.Equal("renamed", stored[first.Id]);
        Assert.Equal("second", stored[second.Id]);
    }

    [Fact]
    public void Tags_keep_their_order()
    {
        _repository.Add(NewTask("tagged", tags: ["zebra", "apple", "mango"]));

        Assert.Equal(new[] { "zebra", "apple", "mango" }, Assert.Single(_repository.GetOpen(10)).Tags);
    }

    [Fact]
    public void A_task_without_stored_text_comes_back_without_it()
    {
        _repository.Add(new TaskItem("id-1", "old row", null, [], Now));

        Assert.Null(Assert.Single(_repository.GetOpen(10)).OriginalText);
    }

    [Fact]
    public void Update_can_clear_the_original_text()
    {
        var task = NewTask("call Anna");
        _repository.Add(task);

        _repository.Update(task with { OriginalText = null });

        Assert.Null(Assert.Single(_repository.GetOpen(10)).OriginalText);
    }

    [Theory]
    [InlineData("it's a trap")]
    [InlineData("Robert'); DROP TABLE Tasks;--")]
    [InlineData("100% done_ [x]")]
    [InlineData("tandarts éè ✓ \U0001F600")]
    public void Titles_with_quotes_wildcards_and_unicode_are_stored_as_typed(string title)
    {
        _repository.Add(NewTask(title));

        Assert.Equal(title, Assert.Single(_repository.GetOpen(10)).Title);
        Assert.Equal(1, _database.Count("SELECT COUNT(*) FROM Tasks;"));
    }

    [Fact]
    public void Deadlines_keep_their_minute_precision()
    {
        var deadline = new DateTime(2026, 12, 31, 23, 59, 0);
        _repository.Add(NewTask("late", deadline));

        Assert.Equal(deadline, Assert.Single(_repository.GetOpen(10)).Deadline);
    }

    [Fact]
    public void Using_the_repository_before_the_database_is_initialized_surfaces_as_a_storage_exception()
    {
        using var uninitialized = new TempDatabase();
        var repository = uninitialized.Repository;
        var task = NewTask("too early");

        Assert.Throws<StorageException>(() => repository.GetOpen(10));
        Assert.Throws<StorageException>(() => repository.Add(task));
        Assert.Throws<StorageException>(() => repository.Update(task));
        Assert.Throws<StorageException>(() => repository.SetStatus(task.Id, TaskItemStatus.Completed));
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
