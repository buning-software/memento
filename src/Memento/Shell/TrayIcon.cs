using System.Drawing;
using Forms = System.Windows.Forms;

namespace Memento.Shell;

/// <summary>The tray icon and its menu (<see cref="TrayMenu"/>): the only visible part of Memento while the overlay is hidden.</summary>
public sealed class TrayIcon : IDisposable
{
    private Forms.NotifyIcon? _notifyIcon;
    private Icon? _icon;
    private TrayMenu? _menu;

    /// <summary>Left click or the Open menu item.</summary>
    public event EventHandler? OpenRequested;

    public event EventHandler? ExitRequested;

    public void Show()
    {
        if (_notifyIcon is not null)
            return;

        _icon = LoadIcon();
        _menu = new TrayMenu();
        _menu.OpenRequested += (_, _) => OpenRequested?.Invoke(this, EventArgs.Empty);
        _menu.ExitRequested += (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty);

        _notifyIcon = new Forms.NotifyIcon
        {
            Icon = _icon,
            Text = "Memento",
            Visible = true,
        };
        _notifyIcon.BalloonTipClicked += (_, _) => OpenRequested?.Invoke(this, EventArgs.Empty);
        _notifyIcon.MouseClick += (_, e) =>
        {
            if (e.Button == Forms.MouseButtons.Left)
                OpenRequested?.Invoke(this, EventArgs.Empty);
            else if (e.Button == Forms.MouseButtons.Right)
                _menu.Show();
        };
    }

    /// <summary>Shows the capture hotkey in the Open item, e.g. "Open (Ctrl+Shift+Space)".</summary>
    public void SetHotkeyText(string hotkey) => _menu?.SetHotkeyText(hotkey);

    /// <summary>Shows a Windows notification from the tray icon; clicking it counts as <see cref="OpenRequested"/>.</summary>
    public void ShowNotification(string title, string text) =>
        _notifyIcon?.ShowBalloonTip(timeout: 10_000, title, text, Forms.ToolTipIcon.Info);

    /// <summary>The app logo: the icon the exe was built with (ApplicationIcon in Memento.csproj).</summary>
    private static Icon LoadIcon()
    {
        using var exeIcon = Icon.ExtractAssociatedIcon(Environment.ProcessPath!)
            ?? throw new InvalidOperationException("The app icon is missing from the executable.");
        return new Icon(exeIcon, Forms.SystemInformation.SmallIconSize);
    }

    public void Dispose()
    {
        if (_notifyIcon is null)
            return;

        _notifyIcon.Visible = false;
        _menu?.Dispose();
        _notifyIcon.Dispose();
        _icon?.Dispose();
        _notifyIcon = null;
    }
}
