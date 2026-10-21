using System.Diagnostics;
using Memento.UiTests.Application;
using NUnit.Framework;
using WinUia;
using static WinUia.NUnit.UiAssertions;

namespace Memento.UiTests.Tests;

/// <summary>What happens before the first shortcut press. Each test starts Memento itself, because the fixture's launch is too early.</summary>
public class StartupTests
{
    [Test]
    public void SecondLaunch_ExitsAndLeavesTheFirstRunning()
    {
        using var data = new TestDataDirectory();
        using var first = MementoApp.Launch(data);
        first.OpenOverlay();

        var startInfo = new ProcessStartInfo(MementoApp.BuiltExecutable) { UseShellExecute = false };
        startInfo.Environment["MEMENTO_DATA_DIR"] = data.Root;
        using var second = Process.Start(startInfo)!;

        Assert.That(second.WaitForExit(TimeSpan.FromSeconds(10)), Is.True, "the second launch quits at once");
        Assert.That(second.ExitCode, Is.EqualTo(0));
        Assert.That(first.Process.HasExited, Is.False, "the first Memento keeps running");

        first.Stop();
    }

    [Test]
    public void ShortcutAnotherApplicationHolds_IsReportedAtStartup()
    {
        using var data = new TestDataDirectory();
        using var taken = NativeInput.ReserveHotkey(NativeInput.CtrlAltShift, NativeInput.KeySpace);

        using var app = MementoApp.Launch(data);

        var dialog = app.TryFindDialog(TimeSpan.FromSeconds(15));
        Assert.That(dialog, Is.Not.Null, "Memento shows a warning");
        Assert.That(UiText.In(dialog!), Has.Some.Contain(" in use by "), "the message says a shortcut is taken");

        app.Stop();
    }

    [Test]
    public void UnreadableDatabase_ShowsAnErrorAndExits()
    {
        using var data = new TestDataDirectory();
        File.WriteAllText(data.DatabaseFile, "this is not a SQLite database");

        using var app = MementoApp.Launch(data);

        var dialog = app.TryFindDialog(TimeSpan.FromSeconds(15));
        Assert.That(dialog, Is.Not.Null, "Memento explains the problem");
        Assert.That(UiText.In(dialog!), Has.Some.Contain("The database could not be initialized"));

        dialog!.Find(e => e.ControlType == ControlType.Button).Click(); // OK
        Eventually(() => app.Process.HasExited, "Memento exits after the message is dismissed");
        Assert.That(app.Process.ExitCode, Is.EqualTo(1));
    }
}
