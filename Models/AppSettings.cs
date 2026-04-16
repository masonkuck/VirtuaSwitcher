namespace VirtuaSwitcher.Models;

public class AppSettings
{
    public int SchemaVersion { get; set; } = 1;
    public bool LaunchOnStartup { get; set; }
    public List<DisplayPreset> Presets { get; set; } = [];
}
