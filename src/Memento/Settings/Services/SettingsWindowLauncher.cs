namespace Memento.Settings.Services;

/// <summary>Opens the settings window, or brings the open one to the front. A new window (and view model) per opening.</summary>
public sealed class SettingsWindowLauncher(Func<SettingsWindow> createWindow)
{
    private SettingsWindow? _window;

    public void Open()
    {
        if (_window is null)
        {
            _window = createWindow();
            _window.Closed += (_, _) => _window = null;
        }

        _window.Show();
        _window.Activate();
    }
}
