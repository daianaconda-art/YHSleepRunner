using YihuanRunner.Platform;

namespace YihuanRunner.Tests;

public sealed class DpiCoordinateMapperTests
{
    [Fact]
    public void ScaleLogicalClientSize_returns_physical_capture_size()
    {
        Assert.Equal((1920, 1080), DpiCoordinateMapper.ScaleLogicalClientSize(1280, 720, 1.5));
    }

    [Fact]
    public void PhysicalClientPointToLogical_converts_physical_ocr_point_to_dpi_virtualized_client_point()
    {
        Assert.Equal((1152, 672), DpiCoordinateMapper.PhysicalClientPointToLogical(1728, 1008, 1.5));
    }
}
