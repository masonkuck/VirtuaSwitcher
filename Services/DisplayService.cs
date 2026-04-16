using System.Runtime.InteropServices;
using VirtuaSwitcher.Models;
using VirtuaSwitcher.Native;

namespace VirtuaSwitcher.Services;

public class DisplayService
{
    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Captures the current display topology and returns a base64 blob
    /// along with path/mode counts for later deserialization.
    /// </summary>
    public (string blob, int pathCount, int modeCount) CaptureCurrentConfig()
    {
        // QDC_ONLY_ACTIVE_PATHS: captures only live paths (no invalid mode indices).
        // SetDisplayConfig requires all supplied paths to be valid/active.
        uint flags = CcdNative.QDC_ONLY_ACTIVE_PATHS;
        var (pathBytes, modeBytes, pathCount, modeCount) = QueryConfigRaw(flags);

        byte[] combined = new byte[pathBytes.Length + modeBytes.Length];
        Buffer.BlockCopy(pathBytes, 0, combined, 0, pathBytes.Length);
        Buffer.BlockCopy(modeBytes, 0, combined, pathBytes.Length, modeBytes.Length);

        return (Convert.ToBase64String(combined), pathCount, modeCount);
    }

    /// <summary>
    /// Applies a stored preset. Returns null on success, error message on failure.
    /// </summary>
    public string? ApplyPreset(DisplayPreset preset)
    {
        if (!preset.HasTopology)
            return "Preset has no captured display configuration.";

        int pathSize = Marshal.SizeOf<CcdNative.DISPLAYCONFIG_PATH_INFO>();
        int modeSize = Marshal.SizeOf<CcdNative.DISPLAYCONFIG_MODE_INFO>();

        byte[] combined = Convert.FromBase64String(preset.TopologyBlob);
        int pathBytes = preset.PathCount * pathSize;
        int modeBytes = preset.ModeCount * modeSize;

        if (combined.Length < pathBytes + modeBytes)
            return "Preset topology data is corrupt or incompatible.";

        IntPtr pathPtr = Marshal.AllocHGlobal(pathBytes);
        IntPtr modePtr = Marshal.AllocHGlobal(modeBytes);
        try
        {
            Marshal.Copy(combined, 0, pathPtr, pathBytes);
            Marshal.Copy(combined, pathBytes, modePtr, modeBytes);

            // SDC_SAVE_TO_DATABASE omitted: combining with SDC_ALLOW_CHANGES can cause
            // error 87 on some drivers. The OS persists the active config automatically.
            uint applyFlags = CcdNative.SDC_APPLY
                            | CcdNative.SDC_USE_SUPPLIED_DISPLAY_CONFIG
                            | CcdNative.SDC_ALLOW_CHANGES;

            int result = CcdNative.SetDisplayConfig(
                (uint)preset.PathCount, pathPtr,
                (uint)preset.ModeCount, modePtr,
                applyFlags);

            if (result != CcdNative.ERROR_SUCCESS)
                return $"SetDisplayConfig failed (error {result}).";

            return null;
        }
        finally
        {
            Marshal.FreeHGlobal(pathPtr);
            Marshal.FreeHGlobal(modePtr);
        }
    }

    /// <summary>
    /// Returns a description for each active monitor including name, resolution, refresh rate,
    /// and whether it is the primary display.
    /// </summary>
    public List<string> GetDisplaySummaries()
    {
        var summaries = new List<string>();
        try
        {
            uint flags = CcdNative.QDC_ONLY_ACTIVE_PATHS;
            var (pathBytes, modeBytes, pathCount, modeCount) = QueryConfigRaw(flags);
            if (pathCount == 0) return summaries;

            var paths = ParseStructArray<CcdNative.DISPLAYCONFIG_PATH_INFO>(pathBytes, pathCount);
            var modes = ParseStructArray<CcdNative.DISPLAYCONFIG_MODE_INFO>(modeBytes, modeCount);

            foreach (var path in paths)
            {
                // Friendly monitor name
                var nameInfo = new CcdNative.DISPLAYCONFIG_TARGET_DEVICE_NAME
                {
                    header = new CcdNative.DISPLAYCONFIG_DEVICE_INFO_HEADER
                    {
                        type = CcdNative.DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_NAME,
                        size = (uint)Marshal.SizeOf<CcdNative.DISPLAYCONFIG_TARGET_DEVICE_NAME>(),
                        adapterId = path.targetInfo.adapterId,
                        id = path.targetInfo.id
                    }
                };
                CcdNative.DisplayConfigGetDeviceInfo(ref nameInfo);
                string name = !string.IsNullOrWhiteSpace(nameInfo.monitorFriendlyDeviceName)
                    ? nameInfo.monitorFriendlyDeviceName
                    : $"Display {summaries.Count + 1}";

                // Resolution and primary status from source mode
                string resolution = "";
                bool isPrimary = false;
                uint srcIdx = path.sourceInfo.modeInfoIdx;
                if (srcIdx != 0xFFFFFFFF && srcIdx < modes.Length)
                {
                    var src = modes[srcIdx].sourceMode;
                    resolution = $"{src.width}×{src.height}";
                    isPrimary = src.position.x == 0 && src.position.y == 0;
                }

                // Refresh rate from target mode
                string refresh = "";
                uint tgtIdx = path.targetInfo.modeInfoIdx;
                if (tgtIdx != 0xFFFFFFFF && tgtIdx < modes.Length)
                {
                    var freq = modes[tgtIdx].targetMode.targetVideoSignalInfo.vSyncFreq;
                    if (freq.Denominator > 0)
                    {
                        int hz = (int)Math.Round((double)freq.Numerator / freq.Denominator);
                        refresh = $"@ {hz}Hz";
                    }
                }

                var parts = new List<string> { name };
                if (!string.IsNullOrEmpty(resolution)) parts.Add(resolution);
                if (!string.IsNullOrEmpty(refresh)) parts.Add(refresh);

                string summary = string.Join(" — ", parts);
                if (isPrimary) summary += " [Primary]";
                summaries.Add(summary);
            }
        }
        catch
        {
            // Return what we have
        }
        return summaries;
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Queries display configuration using GetDisplayConfigBufferSizes for the size step
    /// (avoids the null-array pattern which some drivers reject) then QueryDisplayConfig
    /// with real buffers. Retries without QDC_VIRTUAL_MODE_AWARE if the driver rejects it.
    /// </summary>
    private static (byte[] pathBytes, byte[] modeBytes, int pathCount, int modeCount)
        QueryConfigRaw(uint flags)
    {
        int pathSize = Marshal.SizeOf<CcdNative.DISPLAYCONFIG_PATH_INFO>();
        int modeSize = Marshal.SizeOf<CcdNative.DISPLAYCONFIG_MODE_INFO>();

        // Use GetDisplayConfigBufferSizes to get required counts without null-array hacks
        uint pathCount;
        uint modeCount;
        int result = CcdNative.GetDisplayConfigBufferSizes(flags, out pathCount, out modeCount);

        // Some drivers reject QDC_VIRTUAL_MODE_AWARE — retry without it
        if (result == 87 && (flags & CcdNative.QDC_VIRTUAL_MODE_AWARE) != 0)
        {
            flags &= ~CcdNative.QDC_VIRTUAL_MODE_AWARE;
            result = CcdNative.GetDisplayConfigBufferSizes(flags, out pathCount, out modeCount);
        }

        if (result != CcdNative.ERROR_SUCCESS)
            throw new InvalidOperationException($"GetDisplayConfigBufferSizes failed: {result}");

        if (pathCount == 0 || modeCount == 0)
            throw new InvalidOperationException("GetDisplayConfigBufferSizes returned zero-size buffers.");

        // Allocate and zero-init buffers
        IntPtr pathPtr = Marshal.AllocHGlobal((int)pathCount * pathSize);
        IntPtr modePtr = Marshal.AllocHGlobal((int)modeCount * modeSize);
        try
        {
            for (int i = 0; i < (int)pathCount * pathSize; i++) Marshal.WriteByte(pathPtr, i, 0);
            for (int i = 0; i < (int)modeCount * modeSize; i++) Marshal.WriteByte(modePtr, i, 0);

            result = CcdNative.QueryDisplayConfig(
                flags,
                ref pathCount, pathPtr,
                ref modeCount, modePtr,
                IntPtr.Zero);

            if (result != CcdNative.ERROR_SUCCESS)
                throw new InvalidOperationException($"QueryDisplayConfig failed: {result}");

            byte[] pathBytes = new byte[(int)pathCount * pathSize];
            byte[] modeBytes = new byte[(int)modeCount * modeSize];
            Marshal.Copy(pathPtr, pathBytes, 0, pathBytes.Length);
            Marshal.Copy(modePtr, modeBytes, 0, modeBytes.Length);

            return (pathBytes, modeBytes, (int)pathCount, (int)modeCount);
        }
        finally
        {
            Marshal.FreeHGlobal(pathPtr);
            Marshal.FreeHGlobal(modePtr);
        }
    }

    private static T[] ParseStructArray<T>(byte[] bytes, int count) where T : struct
    {
        int size = Marshal.SizeOf<T>();
        var result = new T[count];
        IntPtr tmp = Marshal.AllocHGlobal(size);
        try
        {
            for (int i = 0; i < count; i++)
            {
                Marshal.Copy(bytes, i * size, tmp, size);
                result[i] = Marshal.PtrToStructure<T>(tmp)!;
            }
        }
        finally
        {
            Marshal.FreeHGlobal(tmp);
        }
        return result;
    }
}
