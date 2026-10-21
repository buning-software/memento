using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Memento.Core;
using Memento.Core.Storage;
using Memento.Shell;
using Memento.Shell.Services;

namespace Memento;

public static class Program
{
#if DEBUG
    // Debug builds keep their own data and may run next to an installed Memento.
    private const string DataFolderName = "Memento.Dev";
    private const string SingleInstanceName = @"Local\Memento.Dev.SingleInstance";
#else
    private const string DataFolderName = "Memento";
    private const string SingleInstanceName = @"Local\Memento.SingleInstance";
#endif

    /// <summary>
    /// Points Memento at another data folder (the UI tests use a temp one). That instance gets its own single-instance
    /// name too, so it never trips over a Memento that is already running.
    /// </summary>
    private const string DataDirectoryVariable = "MEMENTO_DATA_DIR";

    private static bool _crashing;

    [STAThread]
    public static int Main()
    {
        var paths = Environment.GetEnvironmentVariable(DataDirectoryVariable) is { Length: > 0 } dataDirectory
            ? new MementoPaths(System.IO.Path.GetFullPath(dataDirectory))
            : MementoPaths.ForCurrentUser(DataFolderName);
        var instanceName = paths.DataDirectory == MementoPaths.ForCurrentUser(DataFolderName).DataDirectory
            ? SingleInstanceName
            : $@"Local\Memento.SingleInstance.{paths.DataDirectory.Replace('\\', '/').ToLowerInvariant()}";

        using var instance = SingleInstanceGuard.TryAcquire(instanceName);
        if (instance is null)
            return 0; // already running: that instance stays in the tray

        var services = new ServiceCollection()
            .AddMementoCore(paths)
            .AddMementoApp()
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });

        var app = new App();
        app.Startup += (_, _) => services.GetRequiredService<AppStartup>().Run();
        app.Exit += (_, _) => services.Dispose(); // disposes the tray icon, so no ghost icon stays behind
        app.DispatcherUnhandledException += (_, e) => OnUnhandledException(app, services, e);
        return app.Run();
    }

    /// <summary>
    /// Last resort: explain, then shut down cleanly (exit code 1) so Exit still disposes the tray icon.
    /// Only the first failure is reported; a failure while reporting it cannot loop back here.
    /// </summary>
    private static void OnUnhandledException(App app, IServiceProvider services, DispatcherUnhandledExceptionEventArgs e)
    {
        e.Handled = true;
        if (_crashing)
            return;
        _crashing = true;

        try
        {
            services.GetRequiredService<IDialogService>()
                .ShowError($"Memento ran into an unexpected problem and will close.\n\n{e.Exception.Message}");
        }
        catch (Exception)
        {
            // Nothing left to tell the user with; still shut down.
        }

        app.Shutdown(1);
    }
}
