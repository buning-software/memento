using System.Windows;

namespace Memento.Settings.Services;

/// <summary>Opens the settings window, or brings the open one to the front. A new window (and view model) per opening.</summary>
public sealed class SettingsWindowLauncher(Func<SettingsWindow> createWindow)
{
    private SettingsWindow? _window;

    /// <summary>Raised after the settings window has closed.</summary>
    public event EventHandler? Closed;

    /// <summary>The window is owned by <paramref name="owner"/>, so it always shows on top of it.</summary>
    public void Open(Window owner)
    {
        if (_window is null)
        {
            _window = createWindow();
            _window.Owner = owner;
            _window.Closed += (_, _) =>
            {
                _window = null;
                Closed?.Invoke(this, EventArgs.Empty);
            };
        }

        _window.Show();
        _window.Activate();
    }
}
