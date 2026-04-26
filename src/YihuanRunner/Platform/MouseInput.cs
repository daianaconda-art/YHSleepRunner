using System.Runtime.InteropServices;
using YihuanRunner.Automation;

namespace YihuanRunner.Platform;

public sealed class MouseInput
{
    private const uint InputMouse = 0;
    private const uint MouseEventLeftDown = 0x0002;
    private const uint MouseEventLeftUp = 0x0004;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, Input[] pInputs, int cbSize);

    public async Task ClickClientAsync(
        IntPtr hWnd,
        ClientPoint point,
        Random rng,
        int jitterPixels,
        CancellationToken cancellationToken)
    {
        int x = point.X + rng.Next(-jitterPixels, jitterPixels + 1);
        int y = point.Y + rng.Next(-jitterPixels, jitterPixels + 1);
        double scale = DpiScaler.GetPrimaryScreenScale();
        (int logicalX, int logicalY) = DpiCoordinateMapper.PhysicalClientPointToLogical(x, y, scale);

        if (!WindowNative.TryClientToScreen(hWnd, logicalX, logicalY, out int screenX, out int screenY))
            throw new InvalidOperationException("Failed to map click point to screen.");

        WindowNative.BringToFront(hWnd);
        if (!SetCursorPos(screenX, screenY))
            throw new InvalidOperationException($"SetCursorPos failed: {Marshal.GetLastWin32Error()}.");

        await Task.Delay(rng.Next(25, 61), cancellationToken);
        Send(MouseEventLeftDown);
        await Task.Delay(rng.Next(35, 81), cancellationToken);
        Send(MouseEventLeftUp);
    }

    private static void Send(uint flags)
    {
        var input = new Input
        {
            Type = InputMouse,
            Mouse = new MouseInputData { Flags = flags }
        };
        uint sent = SendInput(1, [input], Marshal.SizeOf<Input>());
        if (sent != 1)
            throw new InvalidOperationException($"SendInput failed for mouse event 0x{flags:X}: sent={sent}, lastError={Marshal.GetLastWin32Error()}.");
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Input
    {
        public uint Type;
        public MouseInputData Mouse;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MouseInputData
    {
        public int Dx;
        public int Dy;
        public uint MouseData;
        public uint Flags;
        public uint Time;
        public IntPtr ExtraInfo;
    }
}
