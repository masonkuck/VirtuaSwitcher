using System.Runtime.InteropServices;

namespace VirtuaSwitcher.Native;

internal static class CcdNative
{
    // -------------------------------------------------------------------------
    // Flags for QueryDisplayConfig
    // -------------------------------------------------------------------------
    internal const uint QDC_ALL_PATHS            = 0x00000001;
    internal const uint QDC_ONLY_ACTIVE_PATHS    = 0x00000002;
    internal const uint QDC_DATABASE_CURRENT     = 0x00000004;
    internal const uint QDC_VIRTUAL_MODE_AWARE   = 0x00000010;

    // -------------------------------------------------------------------------
    // Flags for SetDisplayConfig
    // -------------------------------------------------------------------------
    internal const uint SDC_TOPOLOGY_INTERNAL       = 0x00000001;
    internal const uint SDC_TOPOLOGY_CLONE          = 0x00000002;
    internal const uint SDC_TOPOLOGY_EXTEND         = 0x00000004;
    internal const uint SDC_TOPOLOGY_EXTERNAL       = 0x00000008;
    internal const uint SDC_USE_DATABASE_CURRENT    = 0x0000000F;
    internal const uint SDC_USE_SUPPLIED_DISPLAY_CONFIG = 0x00000020;
    internal const uint SDC_VALIDATE                = 0x00000040;
    internal const uint SDC_APPLY                   = 0x00000080;
    internal const uint SDC_NO_OPTIMIZATION         = 0x00000100;
    internal const uint SDC_SAVE_TO_DATABASE        = 0x00000200;
    internal const uint SDC_ALLOW_CHANGES           = 0x00000400;
    internal const uint SDC_PATH_PERSIST_IF_REQUIRED = 0x00000800;
    internal const uint SDC_FORCE_MODE_ENUMERATION  = 0x00001000;
    internal const uint SDC_ALLOW_PATH_ORDER_CHANGES = 0x00002000;
    internal const uint SDC_VIRTUAL_MODE_AWARE      = 0x00008000;

    // -------------------------------------------------------------------------
    // Win32 error codes
    // -------------------------------------------------------------------------
    internal const int ERROR_SUCCESS              = 0;
    internal const int ERROR_INSUFFICIENT_BUFFER  = 122;

    // -------------------------------------------------------------------------
    // DisplayConfigGetDeviceInfo types
    // -------------------------------------------------------------------------
    internal const uint DISPLAYCONFIG_DEVICE_INFO_GET_SOURCE_NAME  = 1;
    internal const uint DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_NAME  = 2;
    internal const uint DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_PREFERRED_MODE = 3;
    internal const uint DISPLAYCONFIG_DEVICE_INFO_GET_ADAPTER_NAME = 4;

    // -------------------------------------------------------------------------
    // Structs
    // -------------------------------------------------------------------------

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    internal struct LUID
    {
        public uint LowPart;
        public int  HighPart;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    internal struct DISPLAYCONFIG_PATH_SOURCE_INFO
    {
        public LUID   adapterId;
        public uint   id;
        public uint   modeInfoIdx;   // union: cloneGroupId / sourceModeInfoIdx
        public uint   statusFlags;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    internal struct DISPLAYCONFIG_PATH_TARGET_INFO
    {
        public LUID   adapterId;
        public uint   id;
        public uint   modeInfoIdx;   // union: desktopModeInfoIdx / targetModeInfoIdx
        public uint   outputTechnology;
        public uint   rotation;
        public uint   scaling;
        public DISPLAYCONFIG_RATIONAL refreshRate;
        public uint   scanLineOrdering;
        [MarshalAs(UnmanagedType.Bool)]
        public bool   targetAvailable;
        public uint   statusFlags;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    internal struct DISPLAYCONFIG_RATIONAL
    {
        public uint Numerator;
        public uint Denominator;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    internal struct DISPLAYCONFIG_PATH_INFO
    {
        public DISPLAYCONFIG_PATH_SOURCE_INFO sourceInfo;
        public DISPLAYCONFIG_PATH_TARGET_INFO targetInfo;
        public uint flags;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    internal struct DISPLAYCONFIG_2DREGION
    {
        public uint cx;
        public uint cy;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    internal struct DISPLAYCONFIG_VIDEO_SIGNAL_INFO
    {
        public ulong                  pixelRate;
        public DISPLAYCONFIG_RATIONAL hSyncFreq;
        public DISPLAYCONFIG_RATIONAL vSyncFreq;
        public DISPLAYCONFIG_2DREGION activeSize;
        public DISPLAYCONFIG_2DREGION totalSize;
        public uint                   videoStandard;
        public uint                   scanLineOrdering;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    internal struct DISPLAYCONFIG_TARGET_MODE
    {
        public DISPLAYCONFIG_VIDEO_SIGNAL_INFO targetVideoSignalInfo;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    internal struct POINTL
    {
        public int x;
        public int y;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    internal struct DISPLAYCONFIG_SOURCE_MODE
    {
        public uint  width;
        public uint  height;
        public uint  pixelFormat;
        public POINTL position;
    }

    // DISPLAYCONFIG_MODE_INFO contains a union; we use explicit layout
    [StructLayout(LayoutKind.Explicit, Pack = 4, Size = 64)]
    internal struct DISPLAYCONFIG_MODE_INFO
    {
        [FieldOffset(0)]  public uint  infoType;
        [FieldOffset(4)]  public uint  id;
        [FieldOffset(8)]  public LUID  adapterId;
        // union starts at offset 16
        [FieldOffset(16)] public DISPLAYCONFIG_TARGET_MODE targetMode;
        [FieldOffset(16)] public DISPLAYCONFIG_SOURCE_MODE sourceMode;
    }

    // -------------------------------------------------------------------------
    // DisplayConfigGetDeviceInfo header + target name
    // -------------------------------------------------------------------------

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    internal struct DISPLAYCONFIG_DEVICE_INFO_HEADER
    {
        public uint type;
        public uint size;
        public LUID adapterId;
        public uint id;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 4)]
    internal struct DISPLAYCONFIG_TARGET_DEVICE_NAME_FLAGS
    {
        public uint value;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 4)]
    internal struct DISPLAYCONFIG_TARGET_DEVICE_NAME
    {
        public DISPLAYCONFIG_DEVICE_INFO_HEADER header;
        public DISPLAYCONFIG_TARGET_DEVICE_NAME_FLAGS flags;
        public uint outputTechnology;
        public ushort edidManufactureId;
        public ushort edidProductCodeId;
        public uint connectorInstance;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string monitorFriendlyDeviceName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string monitorDevicePath;
    }

    // -------------------------------------------------------------------------
    // P/Invoke
    // -------------------------------------------------------------------------

    /// <summary>
    /// Retrieves required buffer sizes for QueryDisplayConfig — avoids passing null array pointers.
    /// </summary>
    [DllImport("user32.dll", SetLastError = false)]
    internal static extern int GetDisplayConfigBufferSizes(
        uint flags,
        out uint numPathArrayElements,
        out uint numModeInfoArrayElements);

    /// <summary>
    /// Fills pre-allocated path and mode buffers. Always pass real (non-null) buffers.
    /// </summary>
    [DllImport("user32.dll", SetLastError = false)]
    internal static extern int QueryDisplayConfig(
        uint flags,
        ref uint numPathArrayElements,
        IntPtr pathArray,
        ref uint numModeInfoArrayElements,
        IntPtr modeInfoArray,
        IntPtr currentTopologyId);

    [DllImport("user32.dll", SetLastError = false)]
    internal static extern int SetDisplayConfig(
        uint numPathArrayElements,
        IntPtr pathArray,
        uint numModeInfoArrayElements,
        IntPtr modeInfoArray,
        uint flags);

    [DllImport("user32.dll")]
    internal static extern int DisplayConfigGetDeviceInfo(
        ref DISPLAYCONFIG_TARGET_DEVICE_NAME requestPacket);
}
