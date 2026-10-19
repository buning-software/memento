using System.IO; // explicit: this file is also linked into the WPF test project, whose implicit usings lack System.IO
using Memento.Core.Enumerations;
using Memento.Core.Storage;
using Memento.Core.Tasks.Models;
using Memento.Core.Tasks.Persistence;

namespace Memento.Core.UnitTests.TestSupport;

/// <summary>
/// Keeps tasks in a list (<see cref="Tasks"/> is in insertion order) and lists them in the repository's order.
/// Set <see cref="Fail"/> to simulate a broken database.
/// </summary>
internal sealed class InMemoryTaskRepository : ITaskRepository
{
    public List<TaskItem> Tasks { get; } = [];

    public bool Fail { get; set; }

    public IReadOnlyList<TaskItem> GetOpen(int limit)
    {
        ThrowIfFailing();
        return Tasks
            .Where(task => task.Status == TaskItemStatus.Open)
            .OrderBy(task => task.Deadline == null)
            .ThenBy(task => task.Deadline)
            .ThenByDescending(task => task.CreatedAt)
            .Take(limit)
            .ToList();
    }

    public void Add(TaskItem task)
    {
        ThrowIfFailing();
        Tasks.Add(task);
    }

    public void Update(TaskItem task)
    {
        ThrowIfFailing();
        int index = Tasks.FindIndex(t => t.Id == task.Id);
        if (index >= 0)
            Tasks[index] = Tasks[index] with
            {
                Title = task.Title,
                Deadline = task.Deadline,
                Tags = task.Tags,
                OriginalText = task.OriginalText,
            };
    }

    public void SetStatus(string id, TaskItemStatus status)
    {
        ThrowIfFailing();
        int index = Tasks.FindIndex(t => t.Id == id);
        if (index >= 0)
            Tasks[index] = Tasks[index] with { Status = status };
    }

    private void ThrowIfFailing()
    {
        if (Fail)
            throw new StorageException("The task database could not be used: disk full", new IOException("disk full"));
    }
}
