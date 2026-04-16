using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using VirtuaSwitcher.Models;
using VirtuaSwitcher.ViewModels;

namespace VirtuaSwitcher.Views;

public partial class MainWindow : Window
{
    private bool _recordingHotkey;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        HotkeyHandleReady?.Invoke(this, new WindowInteropHelper(this).Handle);
    }

    public event EventHandler<IntPtr>? HotkeyHandleReady;

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        // Save current preset before hiding, in case the user edited the name without blurring
        SaveCurrent();
        e.Cancel = true;
        Hide();
    }

    // -------------------------------------------------------------------------
    // Auto-save helpers
    // -------------------------------------------------------------------------

    private void SaveCurrent()
    {
        if (DataContext is MainViewModel vm && vm.SelectedPreset is { } preset)
            vm.SavePreset(preset);
    }

    private void NameBox_LostFocus(object sender, RoutedEventArgs e) => SaveCurrent();

    private void Save_Click(object sender, RoutedEventArgs e) => SaveCurrent();

    // -------------------------------------------------------------------------
    // Hotkey recorder
    // -------------------------------------------------------------------------

    private void HotkeyBox_GotFocus(object sender, RoutedEventArgs e)
    {
        _recordingHotkey = true;
        // Highlight the box to show it's in recording mode — do NOT set .Text
        // (setting Text breaks the OneWay binding permanently)
        if (sender is System.Windows.Controls.TextBox tb)
            tb.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(232, 240, 254));
    }

    private void HotkeyBox_LostFocus(object sender, RoutedEventArgs e)
    {
        _recordingHotkey = false;
        if (sender is System.Windows.Controls.TextBox tb)
            tb.Background = System.Windows.Media.Brushes.White;
    }

    private void HotkeyBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (!_recordingHotkey) return;
        e.Handled = true;

        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key is Key.LeftCtrl or Key.RightCtrl
                or Key.LeftAlt or Key.RightAlt
                or Key.LeftShift or Key.RightShift
                or Key.LWin or Key.RWin
                or Key.Tab or Key.Escape)
            return;

        var modifiers = Keyboard.Modifiers;
        if (modifiers == ModifierKeys.None) return;

        var binding = new HotkeyBinding { Modifiers = modifiers, Key = key };

        if (DataContext is MainViewModel mainVm && mainVm.SelectedPreset is { } preset)
        {
            preset.Hotkey = binding; // PropertyChanged fires → OneWay binding updates TextBox
            mainVm.SavePreset(preset);
        }

        _recordingHotkey = false;
        if (sender is System.Windows.Controls.TextBox box)
            box.Background = System.Windows.Media.Brushes.White;
    }

    private void ClearHotkey_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel mainVm && mainVm.SelectedPreset is { } preset)
        {
            preset.Hotkey = null;
            mainVm.SavePreset(preset);
        }
    }
}
