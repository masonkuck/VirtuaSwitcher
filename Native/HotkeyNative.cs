using System.Runtime.InteropServices;

namespace VirtuaSwitcher.Native;

internal static class HotkeyNative
{
    internal const int WM_HOTKEY = 0x0312;

    // Modifier flags for RegisterHotKey
    internal const uint MOD_ALT        = 0x0001;
    internal const uint MOD_CONTROL    = 0x0002;
    internal const uint MOD_SHIFT      = 0x0004;
    internal const uint MOD_WIN        = 0x0008;
    internal const uint MOD_NOREPEAT   = 0x4000;

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
