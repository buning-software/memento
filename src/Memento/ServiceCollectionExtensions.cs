using Microsoft.Extensions.DependencyInjection;
using Memento.Core.Hotkeys.Services;
using Memento.EntityFramework;
using Memento.Hotkeys;
using Memento.Overlay;
using Memento.Overlay.Tasks.Services;
using Memento.Overlay.ViewModels;
using Memento.Settings;
using Memento.Settings.Services;
using Memento.Settings.ViewModels;
using Memento.Shell;
using Memento.Shell.Services;
using Memento.Theme;

namespace Memento;

public static class ServiceCollectionExtensions
{
    /// <summary>Registers the WPF app: shell, windows, view models, Windows adapters and the SQLite data layer.</summary>
    public static IServiceCollection AddMementoApp(this IServiceCollection services)
    {
        // Shell
        services.AddSingleton<AppStartup>();
        services.AddSingleton<TrayIcon>();
        services.AddSingleton<SettingsWindowLauncher>();

        // Storage: the tasks database (Memento.EntityFramework).
        services.AddMementoEntityFramework();

        // Platform
        services.AddSingleton<IHotkeyRegistrar, Win32HotkeyRegistrar>();
        services.AddSingleton<IAutostart>(_ => PackageIdentity.IsPackaged ? new StartupTaskAutostart() : new RegistryAutostart());
        services.AddSingleton<IShellLauncher, ShellLauncher>();

        // Presentation
        services.AddSingleton<IDialogService, MessageBoxDialogService>();
        services.AddSingleton<DeadlineFormatter>();
        services.AddSingleton<ThemeController>();

        // Overlay: one window for the life of the app, only shown and hidden.
        services.AddSingleton<OverlayViewModel>();
        services.AddSingleton<OverlayWindow>();

        // Settings: a fresh window and view model each time it opens, so it always shows the current settings.
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<SettingsWindow>();
        services.AddSingleton<Func<SettingsWindow>>(provider => () => provider.GetRequiredService<SettingsWindow>());

        return services;
    }
}
