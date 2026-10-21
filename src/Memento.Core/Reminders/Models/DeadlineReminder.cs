using Memento.Core.Enumerations;
using Memento.Core.Tasks.Models;

namespace Memento.Core.Reminders.Models;

/// <summary>One notification to show: which task, and whether its deadline is near or reached.</summary>
public sealed record DeadlineReminder(TaskItem Task, ReminderKind Kind);
