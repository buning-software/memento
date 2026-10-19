using Memento.Core.Enumerations;
using Memento.Core.Settings.Services;
using Memento.Core.Tasks.Models;
using Memento.Core.Tasks.Parsing;
using Memento.Core.Tasks.Parsing.Models;
using Memento.Core.Tasks.Persistence;

namespace Memento.Core.Tasks.Services;

/// <summary>The task use cases. Writes throw <see cref="Storage.StorageException"/> when storage fails.</summary>
public sealed class TaskService(ITaskRepository repository, ISettingsStore settings, TimeProvider time)
{
    public IReadOnlyList<TaskItem> GetOpen(int limit) => repository.GetOpen(limit);

    /// <summary>What the line would become, for the live preview. Never touches storage.</summary>
    public ParsedTask Preview(string text) => TaskParser.Parse(text, Now, settings.Current.DefaultTime);

    public TaskChange Capture(string text)
    {
        text = text.Trim();
        var parsed = Preview(text);
        if (parsed.Title.Length == 0)
            return TaskChange.MissingTitle;

        repository.Add(new TaskItem(
            Guid.NewGuid().ToString(), parsed.Title, parsed.Deadline, parsed.Tags, Now, TaskItemStatus.Open, text));
        return TaskChange.Saved;
    }

    public TaskChange Edit(TaskItem task, string text)
    {
        text = text.Trim();
        var parsed = Preview(text);
        if (parsed.Title.Length == 0)
            return TaskChange.MissingTitle;

        // Unchanged text keeps the task as is, so relative dates ("tomorrow") are not re-resolved against today.
        if (text == task.EditText)
            return TaskChange.Unchanged;

        repository.Update(task with
        {
            Title = parsed.Title,
            Deadline = parsed.Deadline,
            Tags = parsed.Tags,
            OriginalText = text,
        });
        return TaskChange.Saved;
    }

    public void Complete(string id) => repository.SetStatus(id, TaskItemStatus.Completed);

    private DateTime Now => time.GetLocalNow().DateTime;
}
