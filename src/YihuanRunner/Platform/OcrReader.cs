using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using YihuanRunner.Automation;

namespace YihuanRunner.Platform;

public sealed class OcrReader(OcrEngine engine)
{
    public static OcrReader CreateDefault()
    {
        OcrEngine? engine = OcrEngine.TryCreateFromLanguage(new Language("zh-Hans"))
            ?? OcrEngine.TryCreateFromUserProfileLanguages();

        if (engine is null)
            throw new InvalidOperationException("Windows OCR engine is unavailable.");

        Console.WriteLine($"OCR: {engine.RecognizerLanguage.LanguageTag}");
        return new OcrReader(engine);
    }

    public async Task<IReadOnlyList<OcrLineHit>> RecognizeRegionLinesAsync(
        Bitmap frame,
        RelativeRegion region,
        CancellationToken cancellationToken)
    {
        using Bitmap crop = Crop(frame, region);
        using SoftwareBitmap bitmap = ToSoftwareBitmap(crop);
        OcrResult result = await engine.RecognizeAsync(bitmap).AsTask(cancellationToken);

        var hits = new List<OcrLineHit>();
        foreach (OcrLine line in result.Lines)
        {
            OcrLineHit? hit = ToHit(line, crop.Width, crop.Height);
            if (hit is not null)
                hits.Add(hit.Value);
        }

        return hits;
    }

    private static Bitmap Crop(Bitmap source, RelativeRegion region)
    {
        int x = ClampToRange((int)Math.Round(source.Width * region.X), 0, source.Width - 1);
        int y = ClampToRange((int)Math.Round(source.Height * region.Y), 0, source.Height - 1);
        int width = ClampToRange((int)Math.Round(source.Width * region.Width), 1, source.Width - x);
        int height = ClampToRange((int)Math.Round(source.Height * region.Height), 1, source.Height - y);
        return source.Clone(new Rectangle(x, y, width, height), PixelFormat.Format32bppArgb);
    }

    private static OcrLineHit? ToHit(OcrLine line, int width, int height)
    {
        if (line.Words.Count == 0)
        {
            if (string.IsNullOrWhiteSpace(line.Text))
                return null;

            return new OcrLineHit(line.Text, 0.5, 0.5, 1.0, 1.0);
        }

        double minX = double.MaxValue;
        double minY = double.MaxValue;
        double maxX = double.MinValue;
        double maxY = double.MinValue;
        foreach (OcrWord word in line.Words)
        {
            Windows.Foundation.Rect rect = word.BoundingRect;
            minX = Math.Min(minX, rect.X);
            minY = Math.Min(minY, rect.Y);
            maxX = Math.Max(maxX, rect.X + rect.Width);
            maxY = Math.Max(maxY, rect.Y + rect.Height);
        }

        if (minX >= maxX || minY >= maxY)
            return null;

        return new OcrLineHit(
            line.Text,
            ((minX + maxX) * 0.5) / width,
            ((minY + maxY) * 0.5) / height,
            (maxX - minX) / width,
            (maxY - minY) / height);
    }

    private static SoftwareBitmap ToSoftwareBitmap(Bitmap source)
    {
        using var formatted = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppPArgb);
        using (Graphics graphics = Graphics.FromImage(formatted))
        {
            graphics.DrawImage(source, 0, 0, source.Width, source.Height);
        }

        var rect = new Rectangle(0, 0, formatted.Width, formatted.Height);
        BitmapData data = formatted.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppPArgb);
        try
        {
            int rowBytes = formatted.Width * 4;
            byte[] bytes = new byte[rowBytes * formatted.Height];
            if (data.Stride == rowBytes)
            {
                Marshal.Copy(data.Scan0, bytes, 0, bytes.Length);
            }
            else
            {
                for (int y = 0; y < formatted.Height; y++)
                {
                    IntPtr sourceRow = IntPtr.Add(data.Scan0, y * data.Stride);
                    Marshal.Copy(sourceRow, bytes, y * rowBytes, rowBytes);
                }
            }

            return SoftwareBitmap.CreateCopyFromBuffer(
                bytes.AsBuffer(),
                BitmapPixelFormat.Bgra8,
                formatted.Width,
                formatted.Height,
                BitmapAlphaMode.Premultiplied);
        }
        finally
        {
            formatted.UnlockBits(data);
        }
    }

    private static int ClampToRange(int value, int min, int max)
    {
        return Math.Min(Math.Max(value, min), max);
    }
}
