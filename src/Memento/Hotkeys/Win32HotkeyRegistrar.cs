using System.Windows.Interop;
using Memento.Core.Enumerations;
using Memento.Core.Hotkeys.Models;
using Memento.Core.Hotkeys.Services;
using Memento.Native;

namespace Memento.Hotkeys;

/// <summary>
/// Global hotkeys through Win32 RegisterHotKey on an invisible message-only window, so they work while every
/// window is hidden. The window is created on first use, on the UI thread.
/// </summary>
public sealed class Win32HotkeyRegistrar : IHotkeyRegistrar, IDisposable
{
    private const int FirstId = 0x4252;

    private readonly Dictionary<int, Action> _handlers = [];
    private HwndSource? _source;
    private int _nextId = FirstId;

    public bool TryRegister(Hotkey hotkey, Action onPressed)
    {
        var source = _source ??= CreateMessageWindow();
        int id = _nextId++;
        // HotkeyModifiers has the MOD_* values, so the flags pass through unchanged.
        if (!NativeMethods.RegisterHotKey(source.Handle, id, (uint)hotkey.Modifiers | NativeMethods.MOD_NOREPEAT, hotkey.VirtualKey))
            return false;

        _handlers[id] = onPressed;
        return true;
    }

    public void UnregisterAll()
    {
        if (_source is null)
            return;

        foreach (int id in _handlers.Keys)
            NativeMethods.UnregisterHotKey(_source.Handle, id);
        _handlers.Clear();
    }

    public void Dispose()
    {
        UnregisterAll();
        _source?.Dispose();
        _source = null;
    }

    private HwndSource CreateMessageWindow()
    {
        var source = new HwndSource(new HwndSourceParameters("MementoHotkeys")
        {
            ParentWindow = NativeMethods.HWND_MESSAGE,
            Width = 0,
            Height = 0,
        });
        source.AddHook(WndProc);
        return source;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WM_HOTKEY && _handlers.TryGetValue(wParam.ToInt32(), out var handler))
        {
            handler();
            handled = true;
        }
        return IntPtr.Zero;
    }
}
