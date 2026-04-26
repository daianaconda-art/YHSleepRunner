using YihuanRunner.Automation;

namespace YihuanRunner.Tests;

public sealed class StartButtonLocatorTests
{
    [Fact]
    public void FindStartButtonCenter_maps_ocr_hit_from_region_to_client_coordinates()
    {
        var region = new RelativeRegion(0.62, 0.70, 0.35, 0.25);
        var hits = new[]
        {
            new OcrLineHit("开始营业", 0.50, 0.72, 0.25, 0.18),
        };

        ClientPoint? point = StartButtonLocator.FindStartButtonCenter(
            hits,
            region,
            clientWidth: 1280,
            clientHeight: 720);

        Assert.Equal(new ClientPoint(1018, 634), point);
    }

    [Fact]
    public void FindStartButtonCenter_ignores_non_matching_lines()
    {
        var region = new RelativeRegion(0.62, 0.70, 0.35, 0.25);
        var hits = new[]
        {
            new OcrLineHit("暂无目标", 0.50, 0.50, 0.25, 0.18),
        };

        ClientPoint? point = StartButtonLocator.FindStartButtonCenter(
            hits,
            region,
            clientWidth: 1280,
            clientHeight: 720);

        Assert.Null(point);
    }
}
