using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace MailCraftAI.Services.SystemIntegration;

public class GlobalHotkeyService : IGlobalHotkeyService
{
    private const int WmHotkey = 0x0312;
    private const int HotkeyId = 9001;

    // Modifiers: MOD_ALT = 0x0001, MOD_CONTROL = 0x0002
    private const uint ModControl = 0x0002;
    private const uint ModAlt = 0x0001;
    private const uint VkR = 0x52; // Virtual key 'R'

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private IntPtr _windowHandle = IntPtr.Zero;
    private HwndSource? _source;

    public event EventHandler? HotkeyPressed;
    public bool IsRegistered { get; private set; }

    public bool Register(Window window)
    {
        if (IsRegistered) return true;

        var helper = new WindowInteropHelper(window);
        _windowHandle = helper.EnsureHandle();

        _source = HwndSource.FromHwnd(_windowHandle);
        _source?.AddHook(HwndHook);

        IsRegistered = RegisterHotKey(_windowHandle, HotkeyId, ModControl | ModAlt, VkR);
        return IsRegistered;
    }

    public void Unregister()
    {
        if (!IsRegistered) return;

        if (_windowHandle != IntPtr.Zero)
        {
            UnregisterHotKey(_windowHandle, HotkeyId);
            _source?.RemoveHook(HwndHook);
        }

        IsRegistered = false;
    }

    private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmHotkey && wParam.ToInt32() == HotkeyId)
        {
            HotkeyPressed?.Invoke(this, EventArgs.Empty);
            handled = true;
        }

        return IntPtr.Zero;
    }

    public void Dispose()
    {
        Unregister();
    }
}
