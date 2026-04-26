using System.Runtime.InteropServices;

namespace YihuanRunner.Platform;

public static class WindowNative
{
    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool GetClientRect(IntPtr hWnd, out Rect rect);

    [DllImport("user32.dll")]
    private static extern bool ClientToScreen(IntPtr hWnd, ref Point point);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder text, int count);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder text, int count);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc proc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    public static void BringToFront(IntPtr hWnd)
    {
        SetForegroundWindow(hWnd);
    }

    public static bool TryGetClientSize(IntPtr hWnd, out int width, out int height)
    {
        width = 0;
        height = 0;
        if (!GetClientRect(hWnd, out Rect rect))
            return false;

        width = rect.Right - rect.Left;
        height = rect.Bottom - rect.Top;
        return width > 0 && height > 0;
    }

    public static bool TryClientToScreen(IntPtr hWnd, int x, int y, out int screenX, out int screenY)
    {
        var point = new Point { X = x, Y = y };
        bool ok = ClientToScreen(hWnd, ref point);
        screenX = point.X;
        screenY = point.Y;
        return ok;
    }

    public static IReadOnlyList<IntPtr> EnumerateTopLevelWindows()
    {
        var handles = new List<IntPtr>();
        EnumWindows((hWnd, _) =>
        {
            handles.Add(hWnd);
            return true;
        }, IntPtr.Zero);
        return handles;
    }

    public static bool IsVisible(IntPtr hWnd) => IsWindowVisible(hWnd);

    public static int GetProcessId(IntPtr hWnd)
    {
        GetWindowThreadProcessId(hWnd, out uint processId);
        return unchecked((int)processId);
    }

    public static string GetTitle(IntPtr hWnd)
    {
        var text = new System.Text.StringBuilder(512);
        GetWindowText(hWnd, text, text.Capacity);
        return text.ToString();
    }

    public static string GetClassName(IntPtr hWnd)
    {
        var text = new System.Text.StringBuilder(256);
        GetClassName(hWnd, text, text.Capacity);
        return text.ToString();
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
        public int X;
        public int Y;
    }
}
