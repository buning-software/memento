using System.Linq.Expressions;
using System.Reflection;
using WinUia;
using WinUia.Input;
using WinUia.Models;

namespace Memento.UiTests.Application;

/// <summary>The running Memento process. Its windows are page objects over the elements they hold.</summary>
public sealed class MementoApp : App
{
    private const string ExePathVariable = "MEMENTO_EXE";
    private const string ExeName = "Memento.exe";
    private const string OverlayTitle = "Memento";
    private const string SettingsTitle = "Memento settings";

    private static readonly TimeSpan Absent = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan ShowTimeout = TimeSpan.FromSeconds(10);

    /// <summary>The Memento.exe the tests drive: the MEMENTO_EXE variable, else the build next to the tests.</summary>
    public static string BuiltExecutable => Environment.GetEnvironmentVariable(ExePathVariable) is { Length: > 0 } overridden
        ? overridden
        : FindBuiltExecutable();

    protected override string ExecutablePath => BuiltExecutable;

    public static MementoApp Launch(TestDataDirectory data) => Launch<MementoApp>(data.LaunchOptions);

    protected override AppLaunchOptions DefaultOptions => new() { ShowPointer = true };

    public bool IsOverlayShown => TryFindOwnWindow(OverlayTitle, Absent) is not null;

    public int SettingsWindowCount => SettingsWindows().Count();

    public bool IsSettingsShown => TryFindSettingsWindow(Absent) is not null;

    /// <summary>Whether the settings window has the keyboard focus by itself, without the test giving it.</summary>
    public bool IsSettingsFocused => TryFindSettingsWindow(Absent)?.HasKeyboardFocus == true;

    /// <summary>
    /// Opens the capture overlay with the global shortcut, the way a user does. Without <paramref name="shortcut"/> it uses the
    /// seeded one, after waiting for Memento to register it: the shortcut toggles the overlay, so a press that arrives too early
    /// is lost and a second press could hide what the first one showed.
    /// </summary>
    public OverlayPage OpenOverlay(VirtualKey[]? shortcut = null)
    {
        if (shortcut is null && !Poll.Until(() => NativeInput.IsHotkeyTaken(NativeInput.CtrlAltShift, NativeInput.KeySpace), TimeSpan.FromSeconds(20)))
            throw new TimeoutException("Memento did not register its capture shortcut.");

        for (var attempt = 0; attempt < 2; attempt++)
        {
            Context.Input.SendKeys(shortcut ?? TestDataDirectory.CaptureShortcut);

            // Startup shows the overlay once, off-screen and transparent, to warm it up: only a window on a screen counts.
            // The first launch on a cold machine can take many seconds to show it; pressing again meanwhile would hide it.
            Element? window = null;
            if (Poll.Until(() => (window = TryFindOwnWindow(OverlayTitle, TimeSpan.FromMilliseconds(100))) is { IsOffscreen: false }, ShowTimeout))
                return new OverlayPage(window!);
        }

        return Overlay();
    }

    /// <summary>
    /// Ends Memento and waits until the process is gone. Closing alone returns while the process still holds its hotkey and
    /// single-instance name, which would make the next Memento on the same data folder or shortcut start in a conflict.
    /// </summary>
    public void Stop()
    {
        var process = Process;
        Close(TimeSpan.Zero);
        process.WaitForExit(TimeSpan.FromSeconds(10));
    }

    public OverlayPage Overlay() => new(FindOwnWindow(OverlayTitle));

    public SettingsPage Settings()
    {
        var window = TryFindSettingsWindow(Context.DefaultTimeout)
            ?? throw new UiaElementNotFoundException($"No window titled '{SettingsTitle}' in process {Process.Id}.");

        // Keys go to the foreground window; Windows does not always give it to a window opened from one that just hid.
        window.Focus();
        Poll.Until(() => window.HasKeyboardFocus, TimeSpan.FromSeconds(3));
        return new SettingsPage(window);
    }

    /// <summary>A message box of this process (the warning at startup, the database error), found by its window class.</summary>
    public Element? TryFindDialog(TimeSpan timeout)
    {
        var processId = Process.Id;
        return Context.GetRootElement().TryFind(
            e => e.ProcessId == processId && e.ClassName == "#32770",
            TreeScope.Children,
            timeout);
    }

    // App.FindWindow starts from MainWindow, which waits for a visible window; Memento's overlay is hidden until the shortcut,
    // so look for the windows on the desktop instead.
    private Element FindOwnWindow(string title) =>
        TryFindOwnWindow(title, Context.DefaultTimeout)
            ?? throw new UiaElementNotFoundException($"No window titled '{title}' in process {Process.Id}.");

    private Element? TryFindOwnWindow(string title, TimeSpan timeout)
    {
        var processId = Process.Id;
        return Context.GetRootElement().TryFind(
            e => e.ProcessId == processId && e.ControlType == ControlType.Window && e.Name == title,
            TreeScope.Children,
            timeout);
    }

    // The settings window is owned by the overlay, and UI Automation lists an owned window under its owner, not on the desktop.
    private IEnumerable<Element> SettingsWindows()
    {
        var processId = Process.Id;
        Expression<Func<Element, bool>> isSettings = e => e.ProcessId == processId && e.ControlType == ControlType.Window && e.Name == SettingsTitle;

        var windows = Context.GetRootElement().FindAll(isSettings, TreeScope.Children).ToList();
        if (TryFindOwnWindow(OverlayTitle, TimeSpan.FromMilliseconds(100)) is { } overlay)
            windows.AddRange(overlay.FindAll(isSettings, TreeScope.Children));
        return windows;
    }

    private Element? TryFindSettingsWindow(TimeSpan timeout)
    {
        Element? window = null;
        Poll.Until(() => (window = SettingsWindows().FirstOrDefault()) is not null, timeout);
        return window;
    }

    private static string FindBuiltExecutable()
    {
        var binDirectory = typeof(MementoApp).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(a => a.Key == "MementoBinDirectory").Value ?? string.Empty;

        var executables = Directory.Exists(binDirectory)
            ? Directory.EnumerateFiles(binDirectory, ExeName, SearchOption.AllDirectories).ToArray()
            : [];

        return executables.Length == 1
            ? executables[0]
            : throw new FileNotFoundException(
                $"Expected exactly one {ExeName} under '{binDirectory}' but found {executables.Length}. Build src/Memento in the same configuration, or set {ExePathVariable}.");
    }
}
