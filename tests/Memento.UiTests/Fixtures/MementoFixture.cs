using Memento.UiTests.Application;
using WinUia;

namespace Memento.UiTests.Fixtures;

/// <summary>Launches a fresh Memento on an empty data folder for every test and closes it afterwards.</summary>
public abstract class MementoFixture
{
    protected TestDataDirectory Data { get; private set; } = null!;

    protected MementoApp App { get; private set; } = null!;

    /// <summary>More settings.json properties to start with, each after a comma: <c>, "DefaultTime": "08:00"</c>.</summary>
    protected virtual string ExtraSettings => "";

    [SetUp]
    public void LaunchApp()
    {
        Data = new TestDataDirectory(ExtraSettings);
        App = Launch();
    }

    [TearDown]
    public void CloseApp()
    {
        App.Stop();
        App.Dispose();
        Data.Dispose();
    }

    /// <summary>Closes the running Memento and starts a new one on the same data folder.</summary>
    protected void Restart()
    {
        App.Stop();
        App.Dispose();
        App = Launch();
    }

    /// <summary>Fails the test unless <paramref name="condition"/> becomes true within WinUia's default timeout.</summary>
    protected static void Eventually(Func<bool> condition, string because = "") =>
        Assert.That(Poll.Until(condition), Is.True, because);

    private MementoApp Launch() => MementoApp.Launch(Data);
}
