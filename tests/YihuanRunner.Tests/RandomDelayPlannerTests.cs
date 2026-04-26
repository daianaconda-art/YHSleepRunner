using YihuanRunner.Automation;

namespace YihuanRunner.Tests;

public sealed class RandomDelayPlannerTests
{
    [Fact]
    public void NextDelay_returns_value_inside_inclusive_range()
    {
        var rng = new Random(1234);

        for (int i = 0; i < 100; i++)
        {
            TimeSpan delay = RandomDelayPlanner.NextDelay(rng, 250, 650);

            Assert.InRange((int)delay.TotalMilliseconds, 250, 650);
        }
    }

    [Fact]
    public void NextDelay_rejects_invalid_ranges()
    {
        var rng = new Random(1234);

        Assert.Throws<ArgumentOutOfRangeException>(() => RandomDelayPlanner.NextDelay(rng, 700, 650));
    }
}
