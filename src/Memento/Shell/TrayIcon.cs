using System.Drawing;
using Forms = System.Windows.Forms;

namespace Memento.Shell;

/// <summary>The tray icon and its menu: the only visible part of Memento while the overlay is hidden.</summary>
public sealed class TrayIcon : IDisposable
{
    private Forms.NotifyIcon? _notifyIcon;
    private Icon? _icon;
    private Forms.ToolStripItem? _openItem;

    /// <summary>Left click or the Open menu item.</summary>
    public event EventHandler? OpenRequested;

    public event EventHandler? ExitRequested;

    public void Show()
    {
        if (_notifyIcon is not null)
            return;

        _icon = LoadIcon();
        var menu = new Forms.ContextMenuStrip();
        _openItem = menu.Items.Add("Open", null, (_, _) => OpenRequested?.Invoke(this, EventArgs.Empty));
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty));

        _notifyIcon = new Forms.NotifyIcon
        {
            Icon = _icon,
            Text = "Memento",
            ContextMenuStrip = menu,
            Visible = true,
        };
        _notifyIcon.MouseClick += (_, e) =>
        {
            if (e.Button == Forms.MouseButtons.Left)
                OpenRequested?.Invoke(this, EventArgs.Empty);
        };
    }

    /// <summary>Shows the capture hotkey in the Open item, e.g. "Open (Ctrl+Shift+Space)".</summary>
    public void SetHotkeyText(string hotkey)
    {
        if (_openItem is not null)
            _openItem.Text = $"Open ({hotkey})";
    }

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
        _notifyIcon.ContextMenuStrip?.Dispose();
        _notifyIcon.Dispose();
        _icon?.Dispose();
        _notifyIcon = null;
    }
}
