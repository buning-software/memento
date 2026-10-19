using System.Globalization;
using Memento.Core.Enumerations;

namespace Memento.Core.Tasks.Models;

/// <summary>A captured task as it is stored.</summary>
/// <param name="OriginalText">The line as it was typed; null when it was not stored.</param>
public sealed record TaskItem(
    string Id,
    string Title,
    DateTime? Deadline,
    IReadOnlyList<string> Tags,
    DateTime CreatedAt,
    TaskItemStatus Status = TaskItemStatus.Open,
    string? OriginalText = null)
{
    /// <summary>
    /// What the inline editor starts with: the line as typed. Without it, an equivalent line is rebuilt
    /// that the parser reads back into the same title, deadline and tags.
    /// </summary>
    public string EditText => !string.IsNullOrWhiteSpace(OriginalText)
        ? OriginalText
        : string.Join(' ', new[] { Title, Deadline?.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) }
            .Concat(Tags.Select(tag => "#" + tag))
            .Where(part => !string.IsNullOrEmpty(part)));
}
