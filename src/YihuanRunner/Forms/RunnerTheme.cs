using System.Drawing;
using System.Drawing.Drawing2D;

namespace YihuanRunner.Forms;

public static class RunnerTheme
{
    public static readonly Color Bg = Color.FromArgb(247, 244, 239);
    public static readonly Color Panel = Color.FromArgb(251, 249, 246);
    public static readonly Color SurfaceHover = Color.FromArgb(244, 238, 230);
    public static readonly Color Accent = Color.FromArgb(235, 106, 42);
    public static readonly Color AccentSoft = Color.FromArgb(242, 138, 82);
    public static readonly Color Danger = Color.FromArgb(208, 116, 102);
    public static readonly Color TextPrimary = Color.FromArgb(43, 43, 43);
    public static readonly Color TextSecondary = Color.FromArgb(122, 116, 108);
    public static readonly Color Border = Color.FromArgb(232, 225, 216);
    public static readonly Color DisabledFill = Color.FromArgb(244, 239, 233);
    public static readonly Color DisabledFg = Color.FromArgb(183, 174, 165);

    public const string FontFamily = "Microsoft YaHei UI";

    public static Font BodyFont() => new(FontFamily, 10.5F, FontStyle.Regular);
    public static Font BoldFont(float size = 11.25F) => new(FontFamily, size, FontStyle.Bold);
    public static Font CaptionFont() => new(FontFamily, 9.5F, FontStyle.Regular);

    public static GraphicsPath BuildRoundedPath(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        if (radius <= 0)
        {
            path.AddRectangle(new Rectangle(rect.X, rect.Y, rect.Width - 1, rect.Height - 1));
            path.CloseFigure();
            return path;
        }

        int diameter = Math.Min(radius * 2, Math.Min(rect.Width, rect.Height) - 1);
        var bounds = new Rectangle(rect.X, rect.Y, rect.Width - 1, rect.Height - 1);
        path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}
