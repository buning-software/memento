using System.Runtime.InteropServices;

namespace Memento.UiTests.Application;

/// <summary>What WinUia's input does not cover: the F2 key, and holding a global hotkey so Memento finds it taken.</summary>
public static class NativeInput
{
    private const byte VkF2 = 0x71;
    private const uint KeyUp = 0x2;
    private const int ReservedHotkeyId = 0x4D45;

    private const uint ModAlt = 0x1;
    private const uint ModControl = 0x2;
    private const uint ModShift = 0x4;

    public const uint KeyX = 0x58;
    public const uint KeySpace = 0x20;

    public const uint CtrlAltShift = ModControl | ModAlt | ModShift;

    /// <summary>Presses and releases F2 (WinUia's <c>VirtualKey</c> has no F2).</summary>
    public static void PressF2()
    {
        keybd_event(VkF2, 0, 0, 0);
        keybd_event(VkF2, 0, KeyUp, 0);
    }

    /// <summary>
    /// Registers a global hotkey for this thread so another process cannot. Dispose it on the same thread, which a test
    /// method does by wrapping the code that needs the hotkey taken in a <c>using</c>.
    /// </summary>
    public static IDisposable ReserveHotkey(uint modifiers, uint virtualKey)
    {
        if (!RegisterHotKey(IntPtr.Zero, ReservedHotkeyId, modifiers, virtualKey))
            throw new InvalidOperationException("The hotkey is already taken, so the test cannot reserve it.");
        return new Reservation();
    }

    /// <summary>True when someone (the app under test, say) already holds the hotkey; the probe releases it again at once.</summary>
    public static bool IsHotkeyTaken(uint modifiers, uint virtualKey)
    {
        if (!RegisterHotKey(IntPtr.Zero, ReservedHotkeyId, modifiers, virtualKey))
            return true;
        UnregisterHotKey(IntPtr.Zero, ReservedHotkeyId);
        return false;
    }

    private sealed class Reservation : IDisposable
    {
        public void Dispose() => UnregisterHotKey(IntPtr.Zero, ReservedHotkeyId);
    }

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
