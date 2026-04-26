using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace YihuanRunner.Forms.Controls;

public sealed class RoundedButton : Button
{
    public enum ButtonVariant
    {
        Primary,
        Secondary,
    }

    public enum LeadingIcon
    {
        None,
        Play,
        Stop,
    }

    private bool _hover;
    private bool _pressed;

    public ButtonVariant Variant { get; set; } = ButtonVariant.Primary;
    public LeadingIcon Icon { get; set; } = LeadingIcon.None;
    public Color AccentColor { get; set; } = RunnerTheme.Accent;
    public int CornerRadius { get; set; } = 12;

    public RoundedButton()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        FlatAppearance.MouseOverBackColor = Color.Transparent;
        FlatAppearance.MouseDownBackColor = Color.Transparent;
        BackColor = Color.Transparent;
        DoubleBuffered = true;
        SetStyle(ControlStyles.UserPaint
                 | ControlStyles.AllPaintingInWmPaint
                 | ControlStyles.OptimizedDoubleBuffer
                 | ControlStyles.ResizeRedraw
                 | ControlStyles.SupportsTransparentBackColor, true);
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        _hover = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hover = false;
        _pressed = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseDown(MouseEventArgs mevent)
    {
        _pressed = true;
        Invalidate();
        base.OnMouseDown(mevent);
    }

    protected override void OnMouseUp(MouseEventArgs mevent)
    {
        _pressed = false;
        Invalidate();
        base.OnMouseUp(mevent);
    }

    protected override void OnPaint(PaintEventArgs pevent)
    {
        var g = pevent.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        ResolveColors(out Color fill, out Color border, out Color fg);

        using (var path = RunnerTheme.BuildRoundedPath(rect, CornerRadius))
        using (var brush = new SolidBrush(fill))
        {
            g.FillPath(brush, path);
            if (border.A > 0)
            {
                using var pen = new Pen(border, 1);
                g.DrawPath(pen, path);
            }
        }

        if (Enabled && Variant == ButtonVariant.Primary && !_pressed)
        {
            var highlight = new Rectangle(2, 1, Math.Max(1, Width - 4), Math.Max(1, Height / 2));
            using var brush = new LinearGradientBrush(
                highlight,
                Color.FromArgb(36, 255, 255, 255),
                Color.FromArgb(0, 255, 255, 255),
                LinearGradientMode.Vertical);
            using var path = RunnerTheme.BuildRoundedPath(highlight, Math.Max(2, CornerRadius - 1));
            g.FillPath(brush, path);
        }

        DrawContent(g, fg);
    }

    private void ResolveColors(out Color fill, out Color border, out Color fg)
    {
        if (!Enabled)
        {
            fill = RunnerTheme.DisabledFill;
            border = RunnerTheme.Border;
            fg = RunnerTheme.DisabledFg;
            return;
        }

        if (Variant == ButtonVariant.Primary)
        {
            fill = _pressed ? Darken(AccentColor, 0.14F)
                : _hover ? RunnerTheme.AccentSoft
                : AccentColor;
            border = Darken(AccentColor, 0.18F);
            fg = Color.White;
            return;
        }

        fill = _pressed ? Color.FromArgb(245, 241, 235)
            : _hover ? Color.FromArgb(249, 246, 241)
            : RunnerTheme.Panel;
        border = RunnerTheme.Border;
        fg = ForeColor;
    }

    private void DrawContent(Graphics g, Color fg)
    {
        const int iconSize = 10;
        int iconWidth = Icon == LeadingIcon.None ? 0 : iconSize + 8;
        var textSize = TextRenderer.MeasureText(g, Text, Font);
        int contentWidth = iconWidth + textSize.Width;
        int startX = Math.Max(0, (Width - contentWidth) / 2);
        int centerY = Height / 2;

        if (Icon != LeadingIcon.None)
        {
            DrawLeadingIcon(g, fg, new Point(startX, centerY - iconSize / 2), iconSize);
            startX += iconWidth;
        }

        var textRect = new Rectangle(startX, 0, Math.Max(1, Width - startX), Height);
        TextRenderer.DrawText(
            g,
            Text,
            Font,
            textRect,
            fg,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
    }

    private void DrawLeadingIcon(Graphics g, Color fg, Point origin, int size)
    {
        using var brush = new SolidBrush(fg);
        if (Icon == LeadingIcon.Play)
        {
            var points = new Point[]
            {
                new Point(origin.X, origin.Y),
                new Point(origin.X + size, origin.Y + size / 2),
                new Point(origin.X, origin.Y + size),
            };
            g.FillPolygon(brush, points);
            return;
        }

        if (Icon == LeadingIcon.Stop)
            g.FillRectangle(brush, new Rectangle(origin.X, origin.Y, size, size));
    }

    private static Color Darken(Color color, float amount)
    {
        amount = Math.Clamp(amount, 0F, 1F);
        return Color.FromArgb(
            color.A,
            (int)(color.R * (1 - amount)),
            (int)(color.G * (1 - amount)),
            (int)(color.B * (1 - amount)));
    }
}
