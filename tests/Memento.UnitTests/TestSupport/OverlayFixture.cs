using Microsoft.Extensions.Time.Testing;
using Memento.Core.Tasks.Models;
using Memento.Core.Tasks.Services;
using Memento.Core.UnitTests.TestSupport;
using Memento.Overlay.Tasks.Services;
using Memento.Overlay.ViewModels;

namespace Memento.UnitTests.TestSupport;

/// <summary>An overlay view model on the real TaskService, with in-memory storage and a fixed clock.</summary>
internal sealed class OverlayFixture
{
    private readonly FakeTimeProvider _time = new(TestClock.NowOffset);

    public InMemoryTaskRepository Repository { get; } = new();

    public OverlayViewModel CreateViewModel()
    {
        var tasks = new TaskService(Repository, new InMemorySettingsStore(), _time);
        var viewModel = new OverlayViewModel(tasks, new DeadlineFormatter(_time));
        viewModel.Refresh();
        return viewModel;
    }

    public TaskItem AddTask(string title, DateTime? deadline = null, params string[] tags)
    {
        var task = new TaskItem(Guid.NewGuid().ToString(), title, deadline, tags, TestClock.Now);
        Repository.Tasks.Add(task);
        return task;
    }
}
