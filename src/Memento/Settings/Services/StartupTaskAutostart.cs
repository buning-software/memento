using System.Runtime.InteropServices;
using Windows.ApplicationModel;

namespace Memento.Settings.Services;

/// <summary>
/// "Start with Windows" for the MSIX build, through the startup task declared in the package manifest.
/// The Run registry key does not work there: a packaged app's registry writes go to a private copy.
/// </summary>
internal sealed class StartupTaskAutostart : IAutostart
{
    /// <summary>Must match the TaskId of the startup task in Package.appxmanifest.</summary>
    internal const string TaskId = "MementoStartupTask";

    public bool IsEnabled() => IsOn(Run(async () => (await GetTask()).State));

    /// <exception cref="UnauthorizedAccessException">The user or a policy has turned the startup task off for good.</exception>
    public void SetEnabled(bool enabled) => Run(async () =>
    {
        var task = await GetTask();
        if (!enabled)
        {
            task.Disable();
            return 0;
        }

        // A task the user switched off in Task Manager (or a policy) cannot be switched back on from here.
        if (!IsOn(await task.RequestEnableAsync().AsTask()))
            throw new UnauthorizedAccessException("Windows does not allow Memento to start with Windows.");
        return 0;
    });

    private static bool IsOn(StartupTaskState state) => state is StartupTaskState.Enabled or StartupTaskState.EnabledByPolicy;

    private static async Task<StartupTask> GetTask() => await StartupTask.GetAsync(TaskId).AsTask();

    /// <summary>Runs on the thread pool, so waiting here never blocks on the UI thread's synchronization context.</summary>
    private static T Run<T>(Func<Task<T>> work)
    {
        try
        {
            return Task.Run(work).GetAwaiter().GetResult();
        }
        catch (Exception ex) when (ex is COMException or ArgumentException)
        {
            // A broken package (no such task) should read as "unavailable" in Settings, not as a crash.
            throw new UnauthorizedAccessException("The startup task is not available.", ex);
        }
    }
}
