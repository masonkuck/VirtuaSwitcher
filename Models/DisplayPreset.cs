namespace VirtuaSwitcher.Models;

public class DisplayPreset
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public HotkeyBinding? Hotkey { get; set; }

    /// <summary>
    /// Base64-encoded raw bytes of DISPLAYCONFIG_PATH_INFO[] followed by
    /// DISPLAYCONFIG_MODE_INFO[], captured via QueryDisplayConfig.
    /// </summary>
    public string TopologyBlob { get; set; } = string.Empty;

    /// <summary>
    /// Count of path entries encoded in TopologyBlob (needed for deserialization).
    /// </summary>
    public int PathCount { get; set; }

    /// <summary>
    /// Count of mode info entries encoded in TopologyBlob (needed for deserialization).
    /// </summary>
    public int ModeCount { get; set; }

    /// <summary>
    /// Human-readable monitor names, populated at capture time for display in the UI.
    /// Not used by SetDisplayConfig.
    /// </summary>
    public List<string> DisplaySummaries { get; set; } = [];

    public bool HasTopology => !string.IsNullOrEmpty(TopologyBlob);
}
