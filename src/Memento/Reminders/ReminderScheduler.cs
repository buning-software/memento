using System.Windows.Threading;
using Memento.Core.Enumerations;
using Memento.Core.Reminders.Models;
using Memento.Core.Reminders.Services;
using Memento.Core.Storage;
using Memento.Overlay.Tasks.Services;
using Memento.Shell;

namespace Memento.Reminders;

/// <summary>
/// Checks the open tasks every half minute and shows a Windows notification for each deadline that is near or reached,
/// or one notification listing them when several are due at once (as after a restart).
/// </summary>
public sealed class ReminderScheduler(DeadlineReminderService reminders, TrayIcon tray, DeadlineFormatter deadlines)
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(30);

    private DispatcherTimer? _timer;

    public void Start()
    {
        if (_timer is not null)
            return;

        _timer = new DispatcherTimer { Interval = CheckInterval };
        _timer.Tick += (_, _) => Check();
        _timer.Start();
        Check();
    }

    /// <summary>A broken database is not worth interrupting the user for: the next check tries again.</summary>
    private void Check()
    {
        try
        {
            var due = reminders.TakeDue();
            if (due.Count == 1)
                tray.ShowNotification(due[0].Task.Title, Describe(due[0]));
            else if (due.Count > 1)
                tray.ShowNotification($"{due.Count} tasks are due", string.Join('\n', due.Select(r => $"{r.Task.Title} ({Describe(r).ToLowerInvariant()})")));
        }
        catch (StorageException)
        {
        }
    }

    private string Describe(DeadlineReminder reminder)
    {
        string deadline = deadlines.Format(reminder.Task.Deadline!.Value);
        return reminder.Kind switch
        {
            ReminderKind.DueNow => $"Due now ({deadline})",
            _ => $"Due {deadline}",
        };
    }
}
