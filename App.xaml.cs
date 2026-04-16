using System.Drawing;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VirtuaSwitcher.Services;
using VirtuaSwitcher.ViewModels;
using VirtuaSwitcher.Views;
using WinForms = System.Windows.Forms;

namespace VirtuaSwitcher;

public partial class App : System.Windows.Application
{
    private IHost? _host;
    private WinForms.NotifyIcon? _notifyIcon;
    private System.Threading.Mutex? _mutex;
    private MainWindow? _mainWindow;
    private MainViewModel? _mainViewModel;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Single-instance guard
        _mutex = new System.Threading.Mutex(true, "Global\\VirtuaSwitcher", out bool createdNew);
        if (!createdNew)
        {
            System.Windows.MessageBox.Show(
                "VirtuaSwitcher is already running. Check the system tray.",
                "VirtuaSwitcher",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            Shutdown();
            return;
        }

        // DI host
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton<SettingsService>();
                services.AddSingleton<DisplayService>();
                services.AddSingleton<StartupService>();
                services.AddSingleton<HotkeyService>();
                services.AddSingleton<MainViewModel>();
                services.AddSingleton<MainWindow>();
            })
            .Build();

        await _host.StartAsync();

        _mainViewModel = _host.Services.GetRequiredService<MainViewModel>();
        _mainWindow    = _host.Services.GetRequiredService<MainWindow>();

        // Hook hotkey HWND initialization
        _mainWindow.HotkeyHandleReady += (_, hwnd) =>
        {
            var hotkeyService = _host.Services.GetRequiredService<HotkeyService>();
            hotkeyService.Initialize(hwnd);
            _mainViewModel.RehookHotkeys();
        };

        // Wire hotkey trigger → apply preset
        var hotkeyService = _host.Services.GetRequiredService<HotkeyService>();
        hotkeyService.HotkeyTriggered += (_, presetId) =>
        {
            Dispatcher.Invoke(() =>
            {
                _mainViewModel.ApplyPresetById(presetId);
                ShowBalloon(_mainViewModel.StatusMessage ?? "Preset applied.");
            });
        };

        // Rebuild tray menu when presets change
        _mainViewModel.PresetsChanged += BuildContextMenu;

        // Build tray icon
        BuildTrayIcon();

        // Keep window hidden but ensure it gets an HWND so hotkeys work
        _mainWindow.Show();
        _mainWindow.Hide();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        _notifyIcon?.Dispose();
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();

        if (_host is not null)
        {
            await _host.StopAsync(TimeSpan.FromSeconds(3));
            _host.Dispose();
        }

        base.OnExit(e);
    }

    // -------------------------------------------------------------------------
    // Tray icon
    // -------------------------------------------------------------------------

    private void BuildTrayIcon()
    {
        _notifyIcon = new WinForms.NotifyIcon
        {
            Icon = CreatePlaceholderIcon(),
            Text = "VirtuaSwitcher",
            Visible = true
        };

        _notifyIcon.DoubleClick += (_, _) => ShowSettingsWindow();

        BuildContextMenu();
    }

    private void BuildContextMenu()
    {
        if (_notifyIcon is null || _mainViewModel is null) return;

        var menu = new WinForms.ContextMenuStrip();

        foreach (var preset in _mainViewModel.Presets)
        {
            var presetCopy = preset; // capture for closure
            var item = new WinForms.ToolStripMenuItem(preset.Name);
            item.Click += (_, _) =>
            {
                _mainViewModel.ApplyPreset(presetCopy);
                ShowBalloon(_mainViewModel.StatusMessage ?? "Preset applied.");
            };
            menu.Items.Add(item);
        }

        if (_mainViewModel.Presets.Count > 0)
            menu.Items.Add(new WinForms.ToolStripSeparator());

        var settingsItem = new WinForms.ToolStripMenuItem("Settings...");
        settingsItem.Click += (_, _) => ShowSettingsWindow();
        menu.Items.Add(settingsItem);

        menu.Items.Add(new WinForms.ToolStripSeparator());

        var exitItem = new WinForms.ToolStripMenuItem("Exit");
        exitItem.Click += (_, _) => Shutdown();
        menu.Items.Add(exitItem);

        _notifyIcon.ContextMenuStrip = menu;
    }

    private void ShowSettingsWindow()
    {
        if (_mainWindow is null) return;
        _mainWindow.Show();
        _mainWindow.WindowState = WindowState.Normal;
        _mainWindow.Activate();
    }

    private void ShowBalloon(string message)
    {
        _notifyIcon?.ShowBalloonTip(
            timeout: 2000,
            tipTitle: "VirtuaSwitcher",
            tipText: message,
            tipIcon: WinForms.ToolTipIcon.Info);
    }

    // -------------------------------------------------------------------------
    // Placeholder icon (blue square with white "VS" text)
    // -------------------------------------------------------------------------

    private static Icon CreatePlaceholderIcon()
    {
        using var bmp = new Bitmap(32, 32);
        using var g = Graphics.FromImage(bmp);
        g.Clear(System.Drawing.Color.FromArgb(26, 115, 232));

        using var font = new Font("Segoe UI", 10, System.Drawing.FontStyle.Bold, GraphicsUnit.Pixel);
        using var brush = new SolidBrush(System.Drawing.Color.White);
        g.DrawString("VS", font, brush, 4, 8);

        var hicon = bmp.GetHicon();
        return Icon.FromHandle(hicon);
    }
}
