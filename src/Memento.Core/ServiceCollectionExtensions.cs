using Microsoft.Extensions.DependencyInjection;
using Memento.Core.Hotkeys.Services;
using Memento.Core.Reminders.Services;
using Memento.Core.Settings.Services;
using Memento.Core.Storage;
using Memento.Core.Tasks.Persistence;
using Memento.Core.Tasks.Services;

namespace Memento.Core;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers Core. The host must also register an <see cref="IHotkeyRegistrar"/> and an <see cref="ITaskRepository"/>
    /// (the app takes the latter from Memento.EntityFramework).
    /// </summary>
    public static IServiceCollection AddMementoCore(this IServiceCollection services, MementoPaths paths)
    {
        services.AddSingleton(paths);
        services.AddSingleton(TimeProvider.System);

        services.AddSingleton<TaskService>();
        services.AddSingleton<DeadlineReminderService>();

        services.AddSingleton<ISettingsStore, JsonSettingsStore>();
        services.AddSingleton<HotkeyManager>();

        return services;
    }
}
