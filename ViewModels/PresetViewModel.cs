using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VirtuaSwitcher.Models;
using VirtuaSwitcher.Services;

namespace VirtuaSwitcher.ViewModels;

public record AudioDeviceOption(string Id, string Name)
{
    public override string ToString() => Name;
}

public partial class PresetViewModel : ObservableObject
{
    private readonly DisplayPreset _preset;
    private readonly DisplayService _displayService;
    private readonly AudioService _audioService;

    public Guid Id => _preset.Id;

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private HotkeyBinding? _hotkey;

    [ObservableProperty]
    private List<string> _displaySummaries;

    [ObservableProperty]
    private bool _hasTopology;

    [ObservableProperty]
    private ObservableCollection<AudioDeviceOption> _audioDevices = [];

    [ObservableProperty]
    private AudioDeviceOption? _selectedAudioDevice;

    /// <summary>
    /// Called by MainViewModel so the preset can trigger a save after capture or hotkey change.
    /// </summary>
    public Action? RequestSave { get; set; }

    public PresetViewModel(DisplayPreset preset, DisplayService displayService, AudioService audioService)
    {
        _preset = preset;
        _displayService = displayService;
        _audioService = audioService;
        _name = preset.Name;
        _hotkey = preset.Hotkey;
        _displaySummaries = [..preset.DisplaySummaries];
        _hasTopology = preset.HasTopology;

        RefreshAudioDevices();
    }

    public void RefreshAudioDevices()
    {
        var devices = _audioService.GetPlaybackDevices();
        AudioDevices = new ObservableCollection<AudioDeviceOption>(
            devices.Select(d => new AudioDeviceOption(d.Id, d.Name)));

        SelectedAudioDevice = _preset.AudioDeviceId is not null
            ? AudioDevices.FirstOrDefault(d => d.Id == _preset.AudioDeviceId)
            : null;
    }

    partial void OnSelectedAudioDeviceChanged(AudioDeviceOption? value)
    {
        _preset.AudioDeviceId   = value?.Id;
        _preset.AudioDeviceName = value?.Name;
        RequestSave?.Invoke();
    }

    /// <summary>
    /// Captures the current display configuration into this preset and auto-saves.
    /// </summary>
    [RelayCommand]
    public void CaptureConfig()
    {
        var (blob, pathCount, modeCount) = _displayService.CaptureCurrentConfig();
        _preset.TopologyBlob = blob;
        _preset.PathCount = pathCount;
        _preset.ModeCount = modeCount;
        _preset.DisplaySummaries = _displayService.GetDisplaySummaries();

        DisplaySummaries = [.._preset.DisplaySummaries];
        HasTopology = _preset.HasTopology;

        CommitChanges();
        RequestSave?.Invoke();
    }

    /// <summary>
    /// Syncs editable fields back to the underlying model.
    /// </summary>
    public void CommitChanges()
    {
        _preset.Name = Name;
        _preset.Hotkey = Hotkey;
    }

    public DisplayPreset GetModel() => _preset;
}
