using Microsoft.EntityFrameworkCore;
using Memento.Core.Enumerations;
using Memento.Core.Tasks.Models;
using Memento.Core.Tasks.Persistence;
using Memento.EntityFramework.DbContext;
using Memento.EntityFramework.Internal;
using Memento.EntityFramework.Mapping;

namespace Memento.EntityFramework.Repositories;

/// <summary>Tasks in the SQLite Tasks table, one short-lived context per operation.</summary>
internal sealed class TaskRepository(IDbContextFactory<MementoDbContext> contexts) : ITaskRepository
{
    public IReadOnlyList<TaskItem> GetOpen(int limit) => Run(context => context.Tasks
        .AsNoTracking()
        .Where(task => task.Status == TaskItemStatus.Open)
        .OrderBy(task => task.Deadline == null)
        .ThenBy(task => task.Deadline)
        .ThenByDescending(task => task.CreatedAt)
        .Take(limit)
        .AsEnumerable()
        .Select(TaskMapper.ToItem)
        .ToList());

    public void Add(TaskItem task) => Run(context =>
    {
        context.Tasks.Add(TaskMapper.ToEntity(task));
        return context.SaveChanges();
    });

    public void Update(TaskItem task)
    {
        var entity = TaskMapper.ToEntity(task);
        Run(context => context.Tasks
            .Where(row => row.Id == entity.Id)
            .ExecuteUpdate(setters => setters
                .SetProperty(row => row.Title, entity.Title)
                .SetProperty(row => row.Deadline, entity.Deadline)
                .SetProperty(row => row.Tags, entity.Tags)
                .SetProperty(row => row.OriginalText, entity.OriginalText)));
    }

    public void SetStatus(string id, TaskItemStatus status) => Run(context => context.Tasks
        .Where(row => row.Id == id)
        .ExecuteUpdate(setters => setters.SetProperty(row => row.Status, status)));

    /// <summary>Creates a context for one operation; failures leave as <see cref="Core.Storage.StorageException"/>.</summary>
    private T Run<T>(Func<MementoDbContext, T> work) => StorageErrors.Wrap(() =>
    {
        using var context = contexts.CreateDbContext();
        return work(context);
    });
}
