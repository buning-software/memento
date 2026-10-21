using Memento.UiTests.Application;
using NUnit.Framework;

namespace Memento.UiTests.Tests;

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

    private MementoApp Launch() => MementoApp.Launch(Data);
}
