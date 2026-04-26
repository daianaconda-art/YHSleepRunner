using System.Runtime.InteropServices;

namespace YihuanRunner.Platform;

public static class DpiScaler
{
    private const int SmCxScreen = 0;
    private const int EnumCurrentSettings = -1;

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool EnumDisplaySettings(string? deviceName, int modeNum, ref DevMode devMode);

    public static double GetPrimaryScreenScale()
    {
        int logicalWidth = GetSystemMetrics(SmCxScreen);
        var devMode = new DevMode();
        devMode.Size = (short)Marshal.SizeOf<DevMode>();

        if (!EnumDisplaySettings(null, EnumCurrentSettings, ref devMode))
            return 1.0;

        int physicalWidth = devMode.PelsWidth;
        if (logicalWidth <= 0 || physicalWidth <= logicalWidth)
            return 1.0;

        return (double)physicalWidth / logicalWidth;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct DevMode
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DeviceName;
        public short SpecVersion;
        public short DriverVersion;
        public short Size;
        public short DriverExtra;
        public int Fields;
        public int PositionX;
        public int PositionY;
        public int DisplayOrientation;
        public int DisplayFixedOutput;
        public short Color;
        public short Duplex;
        public short YResolution;
        public short TTOption;
        public short Collate;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string FormName;
        public short LogPixels;
        public int BitsPerPel;
        public int PelsWidth;
        public int PelsHeight;
        public int DisplayFlags;
        public int DisplayFrequency;
        public int ICMMethod;
        public int ICMIntent;
        public int MediaType;
        public int DitherType;
        public int Reserved1;
        public int Reserved2;
        public int PanningWidth;
        public int PanningHeight;
    }
}
