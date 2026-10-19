using Memento.Core.Enumerations;
using Memento.Core.Storage;
using Memento.Core.Tasks.Models;

namespace Memento.Core.Tasks.Persistence;

/// <summary>Stores tasks. Implementations throw <see cref="Storage.StorageException"/> when storage fails.</summary>
public interface ITaskRepository
{
    /// <summary>Open tasks: earliest deadline first, tasks without a deadline last, newest <see cref="TaskItem.CreatedAt"/> first among equal deadlines.</summary>
    IReadOnlyList<TaskItem> GetOpen(int limit);

    void Add(TaskItem task);

    /// <summary>Replaces title, deadline, tags and original text of the task with the same id. An unknown id changes nothing.</summary>
    void Update(TaskItem task);

    /// <summary>Sets the status of the task with this id. An unknown id changes nothing.</summary>
    void SetStatus(string id, TaskItemStatus status);
}
