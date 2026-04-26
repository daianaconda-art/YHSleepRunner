using YihuanRunner.Automation;

namespace YihuanRunner.Tests;

public sealed class ClaimRewardLocatorTests
{
    [Fact]
    public void FindClaimRewardCenter_maps_ocr_hit_from_region_to_client_coordinates()
    {
        var region = new RelativeRegion(0.50, 0.72, 0.22, 0.12);
        var hits = new[]
        {
            new OcrLineHit("领取", 0.47, 0.47, 0.12, 0.20),
        };

        ClientPoint? point = ClaimRewardLocator.FindClaimRewardCenter(
            hits,
            region,
            clientWidth: 1920,
            clientHeight: 1080);

        Assert.Equal(new ClientPoint(1159, 839), point);
    }

    [Fact]
    public void FindClaimRewardCenter_ignores_exit_button_text()
    {
        var region = new RelativeRegion(0.50, 0.72, 0.22, 0.12);
        var hits = new[]
        {
            new OcrLineHit("退出", 0.47, 0.47, 0.12, 0.20),
        };

        ClientPoint? point = ClaimRewardLocator.FindClaimRewardCenter(
            hits,
            region,
            clientWidth: 1920,
            clientHeight: 1080);

        Assert.Null(point);
    }
}
