using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Memento.Core.Hotkeys.Services;
using Memento.Core.Reminders.Services;
using Memento.Core.Settings.Services;
using Memento.Core.Storage;
using Memento.Core.Tasks.Persistence;
using Memento.Core.Tasks.Services;
using Memento.Core.UnitTests.TestSupport;
using static Memento.Core.UnitTests.TestSupport.TestClock;

namespace Memento.Core.UnitTests;

public sealed class ServiceCollectionExtensionsTests : IDisposable
{
    private readonly TempDataDirectory _data = new();

    public void Dispose() => _data.Dispose();

    [Fact]
    public void Every_core_service_resolves_once_a_hotkey_registrar_and_a_task_repository_are_provided()
    {
        using var provider = BuildProvider();

        Assert.Same(_data.Paths, provider.GetRequiredService<MementoPaths>());
        Assert.IsType<JsonSettingsStore>(provider.GetRequiredService<ISettingsStore>());
        Assert.NotNull(provider.GetRequiredService<HotkeyManager>());
        Assert.Same(provider.GetRequiredService<TaskService>(), provider.GetRequiredService<TaskService>());
    }

    [Fact]
    public void The_stateful_services_are_shared_so_every_consumer_sees_the_same_instance()
    {
        using var provider = BuildProvider();

        Assert.Same(provider.GetRequiredService<ISettingsStore>(), provider.GetRequiredService<ISettingsStore>());
        Assert.Same(provider.GetRequiredService<HotkeyManager>(), provider.GetRequiredService<HotkeyManager>());
        Assert.Same(provider.GetRequiredService<DeadlineReminderService>(), provider.GetRequiredService<DeadlineReminderService>());
    }

    [Fact]
    public void The_system_clock_is_used_unless_the_host_replaces_it()
    {
        using var provider = BuildProvider();

        Assert.Same(TimeProvider.System, provider.GetRequiredService<TimeProvider>());
    }

    [Fact]
    public void A_clock_registered_after_Core_replaces_the_system_clock()
    {
        var clock = new FakeTimeProvider(NowOffset);
        using var provider = new ServiceCollection()
            .AddMementoCore(_data.Paths)
            .AddSingleton<IHotkeyRegistrar, FakeHotkeyRegistrar>()
            .AddSingleton<ITaskRepository, InMemoryTaskRepository>()
            .AddSingleton<TimeProvider>(clock)
            .BuildServiceProvider();

        Assert.Same(clock, provider.GetRequiredService<TimeProvider>());
    }

    [Fact]
    public void Resolving_the_services_writes_nothing_to_disk()
    {
        using var provider = BuildProvider();

        provider.GetRequiredService<ISettingsStore>();
        provider.GetRequiredService<TaskService>();

        Assert.False(Directory.Exists(_data.Paths.DataDirectory));
    }

    private ServiceProvider BuildProvider() => new ServiceCollection()
        .AddMementoCore(_data.Paths)
        .AddSingleton<IHotkeyRegistrar, FakeHotkeyRegistrar>()
        .AddSingleton<ITaskRepository, InMemoryTaskRepository>()
        .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
}
