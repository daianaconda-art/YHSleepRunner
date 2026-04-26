namespace YihuanRunner.Automation;

public readonly record struct ClientPoint(int X, int Y);

public readonly record struct RelativePoint(double X, double Y)
{
    public ClientPoint ToClientPoint(int clientWidth, int clientHeight)
    {
        return new ClientPoint(
            (int)Math.Round(clientWidth * X, MidpointRounding.AwayFromZero),
            (int)Math.Round(clientHeight * Y, MidpointRounding.AwayFromZero));
    }
}

public readonly record struct RelativeRegion(double X, double Y, double Width, double Height)
{
    public int ToClientX(double localXPct, int clientWidth)
    {
        return (int)Math.Round((X + (Width * localXPct)) * clientWidth, MidpointRounding.AwayFromZero);
    }

    public int ToClientY(double localYPct, int clientHeight)
    {
        return (int)Math.Round((Y + (Height * localYPct)) * clientHeight, MidpointRounding.AwayFromZero);
    }
}

public readonly record struct OcrLineHit(
    string Text,
    double CenterXPct,
    double CenterYPct,
    double WidthPct,
    double HeightPct);
