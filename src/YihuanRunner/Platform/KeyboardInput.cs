using System.Runtime.InteropServices;

namespace YihuanRunner.Platform;

public sealed class KeyboardInput
{
    public const ushort VirtualKeyF = 0x46;
    public const ushort ScanCodeF = 0x21;

    private const uint InputKeyboard = 1;
    private const uint KeyEventKeyUp = 0x0002;
    private const uint KeyEventScanCode = 0x0008;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, Input[] pInputs, int cbSize);

    public async Task PressFAsync(IntPtr hWnd, Random rng, CancellationToken cancellationToken)
    {
        WindowNative.BringToFront(hWnd);
        await Task.Delay(rng.Next(40, 91), cancellationToken);
        SendKey(ScanCodeF, keyUp: false);
        await Task.Delay(rng.Next(45, 101), cancellationToken);
        SendKey(ScanCodeF, keyUp: true);
    }

    private static void SendKey(ushort scanCode, bool keyUp)
    {
        var input = new Input
        {
            Type = InputKeyboard,
            Data = new InputUnion
            {
                Keyboard = new KeyboardInputData
                {
                    ScanCode = scanCode,
                    Flags = KeyEventScanCode | (keyUp ? KeyEventKeyUp : 0)
                }
            }
        };

        uint sent = SendInput(1, [input], Marshal.SizeOf<Input>());
        if (sent != 1)
            throw new InvalidOperationException($"SendInput failed for scan code 0x{scanCode:X}: sent={sent}, lastError={Marshal.GetLastWin32Error()}.");
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Input
    {
        public uint Type;
        public InputUnion Data;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)]
        public KeyboardInputData Keyboard;

        [FieldOffset(0)]
        public MouseInputData Mouse;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KeyboardInputData
    {
        public ushort VirtualKey;
        public ushort ScanCode;
        public uint Flags;
        public uint Time;
        public IntPtr ExtraInfo;
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
