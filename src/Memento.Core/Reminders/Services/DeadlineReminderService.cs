using Memento.Core.Enumerations;
using Memento.Core.Reminders.Models;
using Memento.Core.Settings.Services;
using Memento.Core.Tasks.Persistence;

namespace Memento.Core.Reminders.Services;

/// <summary>
/// Decides which notifications are due for the open tasks, and remembers what it handed out so nothing repeats.
/// A task gets two: an <see cref="ReminderKind.Upcoming"/> one from the lead time before its deadline, and a
/// <see cref="ReminderKind.DueNow"/> one when the deadline is reached. A deadline that is only noticed late
/// (the PC was asleep) still gets its due-now one, up to the lead time after the deadline; after that, and for
/// deadlines that passed while Memento was not running, there is nothing: no overdue reminders.
/// What was handed out is kept in memory: after a restart, a task still in its window is reminded about again.
/// </summary>
public sealed class DeadlineReminderService(ITaskRepository repository, ISettingsStore settings, TimeProvider time)
{
    /// <summary>Task id to the deadline and kind last handed out, so an edited deadline starts over.</summary>
    private readonly Dictionary<string, (DateTime Deadline, ReminderKind Kind)> _reminded = [];

    /// <summary>The notifications to show now. Calling it again does not return them a second time.</summary>
    public IReadOnlyList<DeadlineReminder> TakeDue()
    {
        var current = settings.Current;
        if (!current.RemindersEnabled)
            return [];

        var now = time.GetLocalNow().DateTime;
        var lead = TimeSpan.FromMinutes(current.ReminderLeadMinutes);
        var open = repository.GetOpen(int.MaxValue);

        // Completed or deleted tasks no longer need remembering.
        var openIds = open.Select(task => task.Id).ToHashSet();
        foreach (var id in _reminded.Keys.Where(id => !openIds.Contains(id)).ToList())
            _reminded.Remove(id);

        var due = new List<DeadlineReminder>();
        foreach (var task in open)
        {
            if (task.Deadline is not { } deadline || KindFor(now, deadline, lead) is not { } kind)
                continue;

            // Already handed out for this deadline: only a due-now one may follow an upcoming one.
            if (_reminded.TryGetValue(task.Id, out var last) && last.Deadline == deadline
                && (kind == ReminderKind.Upcoming || last.Kind == ReminderKind.DueNow))
                continue;

            _reminded[task.Id] = (deadline, kind);
            due.Add(new DeadlineReminder(task, kind));
        }
        return due;
    }

    private static ReminderKind? KindFor(DateTime now, DateTime deadline, TimeSpan lead)
    {
        if (now < deadline - lead)
            return null;
        if (now < deadline)
            return ReminderKind.Upcoming;
        return now <= deadline + lead ? ReminderKind.DueNow : null;
    }
}
