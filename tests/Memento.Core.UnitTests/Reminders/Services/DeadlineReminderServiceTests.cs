using Microsoft.Extensions.Time.Testing;
using Memento.Core.Enumerations;
using Memento.Core.Reminders.Models;
using Memento.Core.Reminders.Services;
using Memento.Core.Storage;
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

    [Theory]
    [InlineData(16, null)]                       // one minute before the lead time starts
    [InlineData(15, ReminderKind.Upcoming)]      // exactly the lead time before the deadline
    [InlineData(1, ReminderKind.Upcoming)]
    [InlineData(0, ReminderKind.DueNow)]         // exactly at the deadline
    [InlineData(-1, ReminderKind.DueNow)]
    [InlineData(-15, ReminderKind.DueNow)]       // exactly the lead time after the deadline
    [InlineData(-16, null)]                      // one minute too late
    public void The_kind_depends_on_how_far_the_deadline_is_from_now_at_the_exact_boundaries(int minutesUntilDeadline, ReminderKind? expected)
    {
        AddTask("a", Now.AddMinutes(minutesUntilDeadline));

        var due = _service.TakeDue();

        Assert.Equal(expected, due.Select(reminder => (ReminderKind?)reminder.Kind).SingleOrDefault());
    }

    [Fact]
    public void Deadlines_one_second_outside_the_window_are_not_due()
    {
        AddTask("early", Now.AddMinutes(15).AddSeconds(1));
        AddTask("late", Now.AddMinutes(-15).AddSeconds(-1));

        Assert.Empty(_service.TakeDue());
    }

    [Fact]
    public void Without_a_lead_time_only_the_moment_of_the_deadline_is_due()
    {
        _settings.Update(s => s with { ReminderLeadMinutes = 0 });
        AddTask("a", Now.AddMinutes(1));

        Assert.Empty(_service.TakeDue());

        _time.Advance(TimeSpan.FromMinutes(1));
        Assert.Equal(ReminderKind.DueNow, Assert.Single(_service.TakeDue()).Kind);

        _time.Advance(TimeSpan.FromSeconds(1));
        Assert.Empty(_service.TakeDue());
    }

    [Fact]
    public void An_upcoming_reminder_followed_by_a_long_sleep_gives_no_due_now_reminder()
    {
        AddTask("a", At("2026-10-13T10:10"));
        Assert.Equal(ReminderKind.Upcoming, Assert.Single(_service.TakeDue()).Kind);

        _time.Advance(TimeSpan.FromHours(2));

        Assert.Empty(_service.TakeDue());
    }

    [Fact]
    public void Every_task_in_its_window_gets_its_own_reminder()
    {
        AddTask("soon", At("2026-10-13T10:05"));
        AddTask("now", At("2026-10-13T10:00"));
        AddTask("later", At("2026-10-13T12:00"));

        var due = _service.TakeDue();

        Assert.Equal(new[] { "now", "soon" }, due.Select(reminder => reminder.Task.Id).Order());
    }

    [Fact]
    public void Reminders_that_were_off_are_handed_out_once_they_are_switched_on()
    {
        _settings.Update(s => s with { RemindersEnabled = false });
        AddTask("a", At("2026-10-13T10:05"));
        Assert.Empty(_service.TakeDue());

        _settings.Update(s => s with { RemindersEnabled = true });

        Assert.Single(_service.TakeDue());
    }

    [Fact]
    public void A_task_that_is_completed_and_reopened_is_reminded_about_again()
    {
        AddTask("a", At("2026-10-13T10:05"));
        Assert.Single(_service.TakeDue());

        _repository.SetStatus("a", TaskItemStatus.Completed);
        Assert.Empty(_service.TakeDue());

        _repository.SetStatus("a", TaskItemStatus.Open);
        Assert.Single(_service.TakeDue());
    }

    [Fact]
    public void Without_open_tasks_nothing_is_due()
    {
        Assert.Empty(_service.TakeDue());
    }

    [Fact]
    public void Broken_storage_is_not_swallowed()
    {
        AddTask("a", At("2026-10-13T10:05"));
        _repository.Fail = true;

        Assert.Throws<StorageException>(() => _service.TakeDue());
    }

    [Fact]
    public void Disabled_reminders_do_not_touch_a_broken_storage()
    {
        _settings.Update(s => s with { RemindersEnabled = false });
        _repository.Fail = true;

        Assert.Empty(_service.TakeDue());
    }

    [Fact]
    public void After_a_storage_failure_the_next_call_hands_out_what_was_due()
    {
        AddTask("a", At("2026-10-13T10:05"));
        _repository.Fail = true;
        Assert.Throws<StorageException>(() => _service.TakeDue());

        _repository.Fail = false;

        Assert.Equal("a", Assert.Single(_service.TakeDue()).Task.Id);
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
