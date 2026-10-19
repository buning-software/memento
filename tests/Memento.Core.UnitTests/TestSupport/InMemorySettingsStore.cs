using Memento.Core.Settings.Models;
using Memento.Core.Settings.Services;

namespace Memento.Core.UnitTests.TestSupport;

/// <summary>Keeps settings in memory. Like the real store, an update that changes nothing is neither counted nor raised.</summary>
internal sealed class InMemorySettingsStore(AppSettings? initial = null) : ISettingsStore
{
    public AppSettings Current { get; private set; } = initial ?? new AppSettings();

    public int UpdateCount { get; private set; }

    public event EventHandler<AppSettings>? Changed;

    public void Update(Func<AppSettings, AppSettings> change)
    {
        var updated = change(Current);
        if (updated == Current)
            return;

        Current = updated;
        UpdateCount++;
        Changed?.Invoke(this, Current);
    }
}
