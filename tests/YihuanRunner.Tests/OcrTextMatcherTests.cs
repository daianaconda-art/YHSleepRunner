using YihuanRunner.Automation;

namespace YihuanRunner.Tests;

public sealed class OcrTextMatcherTests
{
    [Theory]
    [InlineData("开始营业")]
    [InlineData("开 始 营 业")]
    [InlineData("开\n始\n营\n业")]
    [InlineData("  开始  营业  ")]
    public void ContainsStartBusiness_accepts_ocr_spacing_noise(string text)
    {
        Assert.True(OcrTextMatcher.ContainsStartBusiness(text));
    }

    [Theory]
    [InlineData("")]
    [InlineData("暂停营业")]
    [InlineData("开始任务")]
    [InlineData("营业时间")]
    public void ContainsStartBusiness_rejects_unrelated_text(string text)
    {
        Assert.False(OcrTextMatcher.ContainsStartBusiness(text));
    }

    [Theory]
    [InlineData("领取")]
    [InlineData("领 取")]
    [InlineData("领\n取")]
    public void ContainsClaimReward_accepts_ocr_spacing_noise(string text)
    {
        Assert.True(OcrTextMatcher.ContainsClaimReward(text));
    }

    [Theory]
    [InlineData("")]
    [InlineData("退出")]
    [InlineData("消耗10")]
    [InlineData("挑战成功")]
    public void ContainsClaimReward_rejects_unrelated_text(string text)
    {
        Assert.False(OcrTextMatcher.ContainsClaimReward(text));
    }
}
