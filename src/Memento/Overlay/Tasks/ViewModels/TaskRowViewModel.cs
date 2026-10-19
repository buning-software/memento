using CommunityToolkit.Mvvm.ComponentModel;
using Memento.Core.Tasks.Models;
using Memento.Overlay.Tasks.Services;

namespace Memento.Overlay.Tasks.ViewModels;

/// <summary>One row of the overlay's task list. While <see cref="IsEditing"/>, the title is replaced by an editor.</summary>
public sealed partial class TaskRowViewModel(TaskItem item, DeadlineFormatter deadlines) : ObservableObject
{
    public TaskItem Item { get; } = item;

    public string Title => Item.Title;

    public string TagsText => string.Join("  ", Item.Tags.Select(tag => "#" + tag));

    public string DeadlineText => Item.Deadline is { } deadline ? deadlines.Format(deadline) : string.Empty;

    public bool IsOverdue => Item.Deadline is { } deadline && deadlines.IsOverdue(deadline);

    [ObservableProperty]
    public partial bool IsEditing { get; set; }

    [ObservableProperty]
    public partial string EditText { get; set; } = "";
}
