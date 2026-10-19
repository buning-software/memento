using Memento.Core.Tasks.Models;
using Memento.EntityFramework.Entities;

namespace Memento.EntityFramework.Mapping;

/// <summary>Converts between Core's TaskItem and the stored row. Tags are joined with commas; no tags is NULL.</summary>
internal static class TaskMapper
{
    public static TaskItem ToItem(TaskEntity entity) => new(
        Id: entity.Id,
        Title: entity.Title,
        Deadline: entity.Deadline,
        Tags: entity.Tags?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? [],
        CreatedAt: entity.CreatedAt,
        Status: entity.Status,
        OriginalText: entity.OriginalText);

    public static TaskEntity ToEntity(TaskItem item) => new()
    {
        Id = item.Id,
        Title = item.Title,
        Deadline = item.Deadline,
        CreatedAt = item.CreatedAt,
        Status = item.Status,
        Tags = item.Tags.Count > 0 ? string.Join(',', item.Tags) : null,
        OriginalText = item.OriginalText,
    };
}
