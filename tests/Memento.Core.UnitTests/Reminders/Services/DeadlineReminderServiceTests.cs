using Microsoft.Extensions.Time.Testing;
using Memento.Core.Enumerations;
using Memento.Core.Reminders.Models;
using Memento.Core.Reminders.Services;
using Memento.Core.Tasks.Models;
using Memento.Core.UnitTests.TestSupport;
using static Memento.Core.UnitTests.TestSupport.TestClock;

namespace Memento.Core.UnitTests.Reminders.Services;

public class DeadlineReminderServiceTests
{
    private readonly InMemoryTaskRepository _repository = new();
    private readonly InMemorySettingsStore _settings = new(); // reminders on, 15 minutes ahead
    private readonly FakeTimeProvider _time = new(NowOffset); // 10:00
    private readonly DeadlineReminderService _service;

    public DeadlineReminderServiceTests() => _service = new DeadlineReminderService(_repository, _settings, _time);

    private TaskItem AddTask(string id, DateTime? deadline, TaskItemStatus status = TaskItemStatus.Open)
    {
        var task = new TaskItem(id, id, deadline, [], Now, status);
        _repository.Add(task);
        return task;
    }

    [Fact]
    public void Task_is_due_once_its_deadline_is_within_the_lead_time()
    {
        AddTask("a", At("2026-10-13T10:15"));

        Assert.Equal("a", Assert.Single(_service.TakeDue()).Task.Id);
    }

    [Fact]
    public void Task_is_not_due_before_the_lead_time()
    {
        AddTask("a", At("2026-10-13T10:16"));

        Assert.Empty(_service.TakeDue());

        _time.Advance(TimeSpan.FromMinutes(1));
        Assert.Single(_service.TakeDue());
    }

    [Fact]
    public void Task_is_reminded_about_only_once()
    {
        AddTask("a", At("2026-10-13T10:10"));

        Assert.Single(_service.TakeDue());
        Assert.Empty(_service.TakeDue());

        _time.Advance(TimeSpan.FromMinutes(5));
        Assert.Empty(_service.TakeDue());
    }

    [Fact]
    public void Task_that_just_passed_its_deadline_is_still_due_but_an_old_one_is_not()
    {
        AddTask("just-passed", At("2026-10-13T09:50"));
        AddTask("long-overdue", At("2026-10-13T09:44"));

        Assert.Equal("just-passed", Assert.Single(_service.TakeDue()).Task.Id);
    }

    [Fact]
    public void Task_without_a_deadline_is_never_due()
    {
        AddTask("a", null);

        Assert.Empty(_service.TakeDue());
    }

    [Fact]
    public void Completed_task_is_not_due()
    {
        AddTask("a", At("2026-10-13T10:05"), TaskItemStatus.Completed);

        Assert.Empty(_service.TakeDue());
    }

    [Fact]
    public void Task_with_an_edited_deadline_is_reminded_about_again()
    {
        var task = AddTask("a", At("2026-10-13T10:05"));
        Assert.Single(_service.TakeDue());

        _repository.Update(task with { Deadline = At("2026-10-13T10:10") });

        Assert.Single(_service.TakeDue());
    }

    [Fact]
    public void Nothing_is_due_while_reminders_are_off()
    {
        _settings.Update(s => s with { RemindersEnabled = false });
        AddTask("a", At("2026-10-13T10:05"));

        Assert.Empty(_service.TakeDue());
    }

    [Fact]
    public void Lead_time_comes_from_the_settings()
    {
        _settings.Update(s => s with { ReminderLeadMinutes = 60 });
        AddTask("a", At("2026-10-13T11:00"));

        Assert.Single(_service.TakeDue());
    }

    [Fact]
    public void Task_gets_an_upcoming_reminder_and_then_a_due_now_reminder()
    {
        AddTask("a", At("2026-10-13T10:10"));

        Assert.Equal(ReminderKind.Upcoming, Assert.Single(_service.TakeDue()).Kind);
        Assert.Empty(_service.TakeDue());

        _time.Advance(TimeSpan.FromMinutes(10)); // the deadline is reached
        Assert.Equal(ReminderKind.DueNow, Assert.Single(_service.TakeDue()).Kind);
        Assert.Empty(_service.TakeDue());

        _time.Advance(TimeSpan.FromMinutes(5));
        Assert.Empty(_service.TakeDue());
    }

    [Fact]
    public void Due_now_reminder_comes_even_when_the_upcoming_one_was_never_handed_out()
    {
        AddTask("a", At("2026-10-13T10:00")); // reached right now

        var reminder = Assert.Single(_service.TakeDue());

        Assert.Equal(ReminderKind.DueNow, reminder.Kind);
        Assert.Empty(_service.TakeDue());
    }

    [Fact]
    public void Edited_deadline_gets_both_reminders_again()
    {
        var task = AddTask("a", At("2026-10-13T10:05"));
        _time.Advance(TimeSpan.FromMinutes(5));
        Assert.Equal(ReminderKind.DueNow, Assert.Single(_service.TakeDue()).Kind);

        _repository.Update(task with { Deadline = At("2026-10-13T10:15") });

        Assert.Equal(ReminderKind.Upcoming, Assert.Single(_service.TakeDue()).Kind);
    }

    [Fact]
    public void Task_noticed_late_after_its_deadline_still_gets_one_due_now_reminder()
    {
        AddTask("a", At("2026-10-13T09:50")); // the PC was asleep at 09:50

        Assert.Equal(ReminderKind.DueNow, Assert.Single(_service.TakeDue()).Kind);
        Assert.Empty(_service.TakeDue());
    }

    [Fact]
    public void Deadline_long_past_gets_no_reminder_at_all()
    {
        AddTask("a", At("2026-10-12T08:00"));

        Assert.Empty(_service.TakeDue());
        _time.Advance(TimeSpan.FromMinutes(30));
        Assert.Empty(_service.TakeDue());
    }
}
