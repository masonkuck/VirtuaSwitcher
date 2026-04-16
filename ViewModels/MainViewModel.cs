using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VirtuaSwitcher.Models;
using VirtuaSwitcher.Services;

namespace VirtuaSwitcher.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly DisplayService _displayService;
    private readonly SettingsService _settingsService;
    private readonly StartupService _startupService;
    private readonly HotkeyService _hotkeyService;

    public ObservableCollection<PresetViewModel> Presets { get; } = [];

    [ObservableProperty]
    private PresetViewModel? _selectedPreset;

    [ObservableProperty]
    private bool _launchOnStartup;

    [ObservableProperty]
    private string? _statusMessage;

    public event Action? PresetsChanged;

    public MainViewModel(
        DisplayService displayService,
        SettingsService settingsService,
        StartupService startupService,
        HotkeyService hotkeyService)
    {
        _displayService = displayService;
        _settingsService = settingsService;
        _startupService = startupService;
        _hotkeyService = hotkeyService;

        var settings = _settingsService.Load();
        foreach (var preset in settings.Presets)
            Presets.Add(WirePreset(new PresetViewModel(preset, _displayService)));

        LaunchOnStartup = _startupService.IsEnabled();
    }

    [RelayCommand]
    public void AddPreset()
    {
        var preset = new DisplayPreset { Name = "New Preset" };
        var vm = WirePreset(new PresetViewModel(preset, _displayService));
        Presets.Add(vm);
        SelectedPreset = vm;
        Save();
        PresetsChanged?.Invoke();
    }

    [RelayCommand]
    public void DeletePreset(PresetViewModel? vm)
    {
        vm ??= SelectedPreset;
        if (vm is null) return;

        Presets.Remove(vm);
        if (SelectedPreset == vm)
            SelectedPreset = Presets.LastOrDefault();

        Save();
        RehookHotkeys();
        PresetsChanged?.Invoke();
    }

    [RelayCommand]
    public void ApplyPreset(PresetViewModel? vm)
    {
        vm ??= SelectedPreset;
        if (vm is null) return;

        var error = _displayService.ApplyPreset(vm.GetModel());
        StatusMessage = error ?? $"Applied preset \"{vm.Name}\".";
    }

    public void ApplyPresetById(Guid id)
    {
        var vm = Presets.FirstOrDefault(p => p.Id == id);
        if (vm is not null)
            ApplyPreset(vm);
    }

    public void SavePreset(PresetViewModel vm)
    {
        vm.CommitChanges();
        Save();
        RehookHotkeys();
        PresetsChanged?.Invoke();
    }

    partial void OnLaunchOnStartupChanged(bool value)
    {
        if (value) _startupService.Enable();
        else       _startupService.Disable();
    }

    public void RehookHotkeys()
    {
        var warnings = _hotkeyService.RegisterPresetHotkeys(Presets.Select(p => p.GetModel()));
        if (warnings.Count > 0)
            StatusMessage = warnings[0];
    }

    private PresetViewModel WirePreset(PresetViewModel vm)
    {
        vm.RequestSave = () => SavePreset(vm);
        return vm;
    }

    private void Save()
    {
        var settings = new AppSettings
        {
            LaunchOnStartup = LaunchOnStartup,
            Presets = [..Presets.Select(p => p.GetModel())]
        };
        _settingsService.Save(settings);
    }
}
