using System.Runtime.InteropServices;

namespace Memento.Native;

internal static class NativeMethods
{
    public const int WM_HOTKEY = 0x0312;

    /// <summary>Holding the keys down does not repeat the hotkey.</summary>
    public const uint MOD_NOREPEAT = 0x4000;

    /// <summary>Parent for a message-only window: invisible, never activated, receives WM_HOTKEY.</summary>
    public static readonly IntPtr HWND_MESSAGE = new(-3);

    public const int APPMODEL_ERROR_NO_PACKAGE = 15700;

    private const uint MONITOR_DEFAULTTONEAREST = 2;
    private const int MDT_EFFECTIVE_DPI = 0;

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    /// <summary>Returns <see cref="APPMODEL_ERROR_NO_PACKAGE"/> when the process has no package identity (it is not running from an MSIX).</summary>
    [DllImport("kernel32.dll")]
    public static extern int GetCurrentPackageFullName(ref uint packageFullNameLength, IntPtr packageFullName);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromPoint(NativePoint pt, uint flags);

    [DllImport("shcore.dll")]
    private static extern int GetDpiForMonitor(IntPtr hMonitor, int dpiType, out uint dpiX, out uint dpiY);

    /// <summary>DPI scale (1.0 = 96 dpi) of the monitor nearest to the given pixel position.</summary>
    public static double GetMonitorScale(int x, int y)
    {
        var monitor = MonitorFromPoint(new NativePoint { X = x, Y = y }, MONITOR_DEFAULTTONEAREST);
        return GetDpiForMonitor(monitor, MDT_EFFECTIVE_DPI, out uint dpiX, out _) == 0 ? dpiX / 96.0 : 1.0;
    }
}
