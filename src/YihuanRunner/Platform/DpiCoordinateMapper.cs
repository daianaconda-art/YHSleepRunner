namespace YihuanRunner.Platform;

public static class DpiCoordinateMapper
{
    public static (int Width, int Height) ScaleLogicalClientSize(int logicalWidth, int logicalHeight, double scale)
    {
        return (
            Math.Max(1, (int)Math.Round(logicalWidth * scale, MidpointRounding.AwayFromZero)),
            Math.Max(1, (int)Math.Round(logicalHeight * scale, MidpointRounding.AwayFromZero)));
    }

    public static (int X, int Y) PhysicalClientPointToLogical(int physicalX, int physicalY, double scale)
    {
        if (scale <= 1.0)
            return (physicalX, physicalY);

        return (
            (int)Math.Round(physicalX / scale, MidpointRounding.AwayFromZero),
            (int)Math.Round(physicalY / scale, MidpointRounding.AwayFromZero));
    }
}
