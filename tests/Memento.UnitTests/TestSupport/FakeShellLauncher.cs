using Memento.Settings.Services;

namespace Memento.UnitTests.TestSupport;

internal sealed class FakeShellLauncher : IShellLauncher
{
    public List<string> Opened { get; } = [];

    public void Open(string target) => Opened.Add(target);
}
