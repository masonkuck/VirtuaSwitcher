using System.Windows.Interop;
using VirtuaSwitcher.Models;
using VirtuaSwitcher.Native;

namespace VirtuaSwitcher.Services;

public class HotkeyService : IDisposable
{
    public event EventHandler<Guid>? HotkeyTriggered;

    private IntPtr _hwnd;
    private HwndSource? _source;
    private readonly Dictionary<int, Guid> _idToPreset = [];
    private int _nextId = 1;
    private bool _initialized;

    public void Initialize(IntPtr hwnd)
    {
        _hwnd = hwnd;
        _source = HwndSource.FromHwnd(hwnd);
        _source?.AddHook(WndProc);
        _initialized = true;
    }

    public List<string> RegisterPresetHotkeys(IEnumerable<DisplayPreset> presets)
    {
        if (!_initialized) return [];

        UnregisterAll();

        var warnings = new List<string>();
        foreach (var preset in presets)
        {
            if (preset.Hotkey is null) continue;

            int id = _nextId++;
            bool ok = HotkeyNative.RegisterHotKey(
                _hwnd,
                id,
                preset.Hotkey.NativeModifiers,
                (uint)preset.Hotkey.VirtualKey);

            if (ok)
                _idToPreset[id] = preset.Id;
            else
                warnings.Add($"Could not register hotkey {preset.Hotkey} for preset \"{preset.Name}\" — it may be in use by another application.");
        }
        return warnings;
    }

    public void UnregisterAll()
    {
        foreach (var id in _idToPreset.Keys)
            HotkeyNative.UnregisterHotKey(_hwnd, id);

        _idToPreset.Clear();
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == HotkeyNative.WM_HOTKEY)
        {
            int id = wParam.ToInt32();
            if (_idToPreset.TryGetValue(id, out var presetId))
            {
                HotkeyTriggered?.Invoke(this, presetId);
                handled = true;
            }
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        UnregisterAll();
        _source?.RemoveHook(WndProc);
    }
}
