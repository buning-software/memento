using Memento.Core.Settings.Models;

namespace Memento.Core.Settings.Services;

public interface ISettingsStore
{
    AppSettings Current { get; }

    /// <summary>Applies the change, saves it and raises <see cref="Changed"/>. A change that changes nothing does neither.</summary>
    void Update(Func<AppSettings, AppSettings> change);

    /// <summary>Raised after an update, with the new settings. Lets the theme, tray and hotkey follow without coupling.</summary>
    event EventHandler<AppSettings>? Changed;
}
