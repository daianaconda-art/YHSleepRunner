using YihuanRunner.Automation;

namespace YihuanRunner.Tests;

public sealed class RunnerOptionsTests
{
    [Fact]
    public void Parse_uses_safe_defaults_for_yihuan()
    {
        RunnerOptions options = RunnerOptions.Parse([]);

        Assert.Equal("HTGame", options.ProcessName);
        Assert.Equal("异环", options.TitleContains);
        Assert.Equal(new RelativeRegion(0.78, 0.88, 0.20, 0.12), options.StartButtonRegion);
        Assert.Equal(new RelativeRegion(0.50, 0.72, 0.22, 0.12), options.ClaimRewardRegion);
        Assert.Equal(TimeSpan.FromSeconds(45), options.BusinessDuration);
        Assert.Equal(new RelativePoint(0.0667, 0.4620), options.HammerPoint);
        Assert.Equal(250, options.MinHammerDelayMs);
        Assert.Equal(650, options.MaxHammerDelayMs);
        Assert.Equal(0, options.LoopCount);
    }

    [Fact]
    public void Parse_accepts_overrides()
    {
        RunnerOptions options = RunnerOptions.Parse([
            "--process", "CustomGame",
            "--title", "异环测试",
            "--hammer-x", "0.2",
            "--hammer-y", "0.4",
            "--duration-sec", "12",
            "--loading-ms", "3000",
            "--min-ms", "111",
            "--max-ms", "222",
            "--loops", "3",
            "--claim-region", "0.1,0.2,0.3,0.4",
            "--once",
            "--dry-run",
            "--snapshot", "probe.png"
        ]);

        Assert.Equal("CustomGame", options.ProcessName);
        Assert.Equal("异环测试", options.TitleContains);
        Assert.Equal(new RelativePoint(0.2, 0.4), options.HammerPoint);
        Assert.Equal(new RelativeRegion(0.1, 0.2, 0.3, 0.4), options.ClaimRewardRegion);
        Assert.Equal(TimeSpan.FromSeconds(12), options.BusinessDuration);
        Assert.Equal(TimeSpan.FromMilliseconds(3000), options.LoadingDelay);
        Assert.Equal(111, options.MinHammerDelayMs);
        Assert.Equal(222, options.MaxHammerDelayMs);
        Assert.Equal(3, options.LoopCount);
        Assert.True(options.RunOnce);
        Assert.True(options.DryRun);
        Assert.Equal("probe.png", options.SnapshotPath);
    }

    [Fact]
    public void Parse_rejects_negative_loop_count()
    {
        Assert.Throws<ArgumentException>(() => RunnerOptions.Parse(["--loops", "-1"]));
    }

    [Fact]
    public void Usage_uses_generic_public_copy()
    {
        Assert.DoesNotContain("异环", RunnerOptions.Usage);
        Assert.DoesNotContain("HTGame", RunnerOptions.Usage);
        Assert.Contains("YHSleepRunner", RunnerOptions.Usage);
        Assert.Contains("OCR", RunnerOptions.Usage);
    }
}
