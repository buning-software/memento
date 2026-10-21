using System.Windows.Interop;
using Memento.Native;

namespace Memento.Shell;

/// <summary>
/// The tray icon's right-click menu: the native Windows popup menu, which has the Windows 11 look and
/// follows the Windows light/dark setting once the process opts in to it.
/// </summary>
public sealed class TrayMenu : IDisposable
{
    private const int OpenId = 1;
    private const int ExitId = 2;

    private string _openText = "Open";
    private HwndSource? _owner;
    private bool _themed;

    public event EventHandler? OpenRequested;

    public event EventHandler? ExitRequested;

    /// <summary>Shows the capture hotkey in the Open item, e.g. "Open (Ctrl+Shift+Space)".</summary>
    public void SetHotkeyText(string hotkey) => _openText = $"Open ({hotkey})";

    /// <summary>Opens the menu above the mouse pointer and returns once it is closed.</summary>
    public void Show()
    {
        FollowSystemTheme();
        var owner = _owner ??= CreateOwner();

        var menu = NativeMethods.CreatePopupMenu();
        try
        {
            NativeMethods.AppendMenu(menu, NativeMethods.MF_STRING, OpenId, _openText);
            NativeMethods.AppendMenu(menu, NativeMethods.MF_SEPARATOR, 0, null);
            NativeMethods.AppendMenu(menu, NativeMethods.MF_STRING, ExitId, "Exit");

            NativeMethods.GetCursorPos(out var cursor);

            // The menu only closes on a click elsewhere when its owner is the foreground window.
            NativeMethods.SetForegroundWindow(owner.Handle);
            int chosen = NativeMethods.TrackPopupMenuEx(
                menu,
                NativeMethods.TPM_RIGHTBUTTON | NativeMethods.TPM_RETURNCMD | NativeMethods.TPM_BOTTOMALIGN,
                cursor.X,
                cursor.Y,
                owner.Handle,
                IntPtr.Zero);
            NativeMethods.PostMessage(owner.Handle, NativeMethods.WM_NULL, IntPtr.Zero, IntPtr.Zero); // lets the menu close cleanly

            switch (chosen)
            {
                case OpenId:
                    OpenRequested?.Invoke(this, EventArgs.Empty);
                    break;
                case ExitId:
                    ExitRequested?.Invoke(this, EventArgs.Empty);
                    break;
            }
        }
        finally
        {
            NativeMethods.DestroyMenu(menu);
        }
    }

    public void Dispose()
    {
        _owner?.Dispose();
        _owner = null;
    }

    /// <summary>Menus are light unless the process opts in to dark mode. Failing to is harmless: they just stay light.</summary>
    private void FollowSystemTheme()
    {
        if (_themed)
            return;

        _themed = true;
        try
        {
            NativeMethods.SetPreferredAppMode(NativeMethods.ALLOW_DARK_APP_MODE);
            NativeMethods.FlushMenuThemes();
        }
        catch (Exception ex) when (ex is EntryPointNotFoundException or DllNotFoundException)
        {
        }
    }

    /// <summary>An invisible top-level window to own the menu: never shown, so it never appears or takes the taskbar.</summary>
    private static HwndSource CreateOwner() => new(new HwndSourceParameters("MementoTrayMenu")
    {
        WindowStyle = unchecked((int)0x80000000), // WS_POPUP, not visible
        Width = 0,
        Height = 0,
    });
}
