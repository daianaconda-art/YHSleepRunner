using System.Drawing;

namespace YihuanRunner.Platform;

public sealed class CapturedFrame(Bitmap bitmap, int clientWidth, int clientHeight) : IDisposable
{
    public Bitmap Bitmap { get; } = bitmap;
    public int ClientWidth { get; } = clientWidth;
    public int ClientHeight { get; } = clientHeight;

    public void Dispose()
    {
        Bitmap.Dispose();
    }
}

public sealed class ScreenCaptureService
{
    public CapturedFrame CaptureClient(IntPtr hWnd)
    {
        if (!WindowNative.TryGetClientSize(hWnd, out int logicalWidth, out int logicalHeight))
            throw new InvalidOperationException("Target client area is empty.");

        if (!WindowNative.TryClientToScreen(hWnd, 0, 0, out int screenX, out int screenY))
            throw new InvalidOperationException("Failed to map client origin to screen.");

        double scale = DpiScaler.GetPrimaryScreenScale();
        (int width, int height) = DpiCoordinateMapper.ScaleLogicalClientSize(logicalWidth, logicalHeight, scale);
        var bitmap = new Bitmap(width, height);
        using Graphics graphics = Graphics.FromImage(bitmap);
        graphics.CopyFromScreen(screenX, screenY, 0, 0, new Size(width, height));
        return new CapturedFrame(bitmap, width, height);
    }
}
