using Memento.Core.Hotkeys.Models;
using Memento.Core.Hotkeys.Services;

namespace Memento.Core.UnitTests.TestSupport;

/// <summary>Stands in for Win32: hotkeys in <see cref="TakenByOtherApps"/> cannot be registered.</summary>
internal sealed class FakeHotkeyRegistrar : IHotkeyRegistrar
{
    public HashSet<Hotkey> TakenByOtherApps { get; } = [];

    public Dictionary<Hotkey, Action> Registered { get; } = [];

    public bool TryRegister(Hotkey hotkey, Action onPressed)
    {
        if (TakenByOtherApps.Contains(hotkey) || Registered.ContainsKey(hotkey))
            return false;

        Registered[hotkey] = onPressed;
        return true;
    }

    public void UnregisterAll() => Registered.Clear();
}
