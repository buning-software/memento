using System.IO; // explicit: this file is also linked into the WPF test project, whose implicit usings lack System.IO
using Memento.Core.Storage;

namespace Memento.Core.UnitTests.TestSupport;

/// <summary>A data folder of its own for one test, deleted afterwards. Nothing is created until the code under test does.</summary>
internal sealed class TempDataDirectory : IDisposable
{
    public MementoPaths Paths { get; } =
        new(Path.Combine(Path.GetTempPath(), "memento-tests", Guid.NewGuid().ToString("N")));

    public void Dispose()
    {
        if (Directory.Exists(Paths.DataDirectory))
            Directory.Delete(Paths.DataDirectory, recursive: true);
    }
}
