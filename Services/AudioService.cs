using AudioSwitcher.AudioApi;
using AudioSwitcher.AudioApi.CoreAudio;

namespace VirtuaSwitcher.Services;

public class AudioService
{
    private readonly CoreAudioController _controller = new();

    public List<(string Id, string Name)> GetPlaybackDevices()
    {
        try
        {
            return _controller
                .GetPlaybackDevices(DeviceState.Active)
                .OrderBy(d => d.FullName)
                .Select(d => (d.Id.ToString(), d.FullName))
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    public string? GetDefaultPlaybackDeviceId()
    {
        try
        {
            return _controller.DefaultPlaybackDevice?.Id.ToString();
        }
        catch
        {
            return null;
        }
    }

    public string? SetDefaultPlaybackDevice(string deviceId)
    {
        try
        {
            var device = _controller.GetPlaybackDevices(DeviceState.Active)
                .FirstOrDefault(d => d.Id.ToString() == deviceId);

            if (device is null)
                return $"Audio device not found.";

            device.SetAsDefault();
            return null;
        }
        catch (Exception ex)
        {
            return $"Failed to set audio device: {ex.Message}";
        }
    }
}
