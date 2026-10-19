using Microsoft.Extensions.DependencyInjection;
using Memento.Core.Hotkeys.Services;
using Memento.Core.Settings.Services;
using Memento.Core.Storage;
using Memento.Core.Tasks.Persistence;
using Memento.Core.Tasks.Services;
using Memento.Core.UnitTests.TestSupport;

namespace Memento.Core.UnitTests;

public sealed class ServiceCollectionExtensionsTests : IDisposable
{
    private readonly TempDataDirectory _data = new();

    public void Dispose() => _data.Dispose();

    [Fact]
    public void Every_core_service_resolves_once_a_hotkey_registrar_and_a_task_repository_are_provided()
    {
        using var provider = new ServiceCollection()
            .AddMementoCore(_data.Paths)
            .AddSingleton<IHotkeyRegistrar, FakeHotkeyRegistrar>()
            .AddSingleton<ITaskRepository, InMemoryTaskRepository>()
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        Assert.Same(_data.Paths, provider.GetRequiredService<MementoPaths>());
        Assert.IsType<JsonSettingsStore>(provider.GetRequiredService<ISettingsStore>());
        Assert.NotNull(provider.GetRequiredService<HotkeyManager>());
        Assert.Same(provider.GetRequiredService<TaskService>(), provider.GetRequiredService<TaskService>());
    }
}
