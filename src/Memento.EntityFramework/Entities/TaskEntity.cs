using Memento.Core.Enumerations;

namespace Memento.EntityFramework.Entities;

/// <summary>A row of the Tasks table, shaped as stored. Converted to and from TaskItem by TaskMapper.</summary>
public sealed class TaskEntity
{
    public required string Id { get; set; }

    public required string Title { get; set; }

    /// <summary>Local time.</summary>
    public DateTime? Deadline { get; set; }

    /// <summary>Local time.</summary>
    public DateTime CreatedAt { get; set; }

    public TaskItemStatus Status { get; set; }

    /// <summary>Comma separated ("admin,urgent"); null when the task has no tags.</summary>
    public string? Tags { get; set; }

    /// <summary>The line as typed, shown again when editing; null when it was not stored.</summary>
    public string? OriginalText { get; set; }
}
