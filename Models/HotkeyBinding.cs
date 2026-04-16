using System.Windows.Input;

namespace VirtuaSwitcher.Models;

public class HotkeyBinding
{
    public ModifierKeys Modifiers { get; set; }
    public Key Key { get; set; }

    public int VirtualKey => KeyInterop.VirtualKeyFromKey(Key);

    public uint NativeModifiers
    {
        get
        {
            uint mods = Native.HotkeyNative.MOD_NOREPEAT;
            if (Modifiers.HasFlag(ModifierKeys.Alt))     mods |= Native.HotkeyNative.MOD_ALT;
            if (Modifiers.HasFlag(ModifierKeys.Control)) mods |= Native.HotkeyNative.MOD_CONTROL;
            if (Modifiers.HasFlag(ModifierKeys.Shift))   mods |= Native.HotkeyNative.MOD_SHIFT;
            if (Modifiers.HasFlag(ModifierKeys.Windows)) mods |= Native.HotkeyNative.MOD_WIN;
            return mods;
        }
    }

    public override string ToString()
    {
        var parts = new List<string>();
        if (Modifiers.HasFlag(ModifierKeys.Control)) parts.Add("Ctrl");
        if (Modifiers.HasFlag(ModifierKeys.Alt))     parts.Add("Alt");
        if (Modifiers.HasFlag(ModifierKeys.Shift))   parts.Add("Shift");
        if (Modifiers.HasFlag(ModifierKeys.Windows)) parts.Add("Win");
        parts.Add(Key.ToString());
        return string.Join("+", parts);
    }
}
