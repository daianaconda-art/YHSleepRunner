using YihuanRunner.Automation;

namespace YihuanRunner.Tests;

public sealed class ClaimCompletionCounterTests
{
    [Fact]
    public void RecordClaimAndShouldStop_never_stops_when_loop_count_is_zero()
    {
        var counter = new ClaimCompletionCounter(0);

        Assert.False(counter.RecordClaimAndShouldStop());
        Assert.False(counter.RecordClaimAndShouldStop());
        Assert.Equal(2, counter.CompletedClaims);
    }

    [Fact]
    public void RecordClaimAndShouldStop_stops_when_claim_count_reaches_target()
    {
        var counter = new ClaimCompletionCounter(2);

        Assert.False(counter.RecordClaimAndShouldStop());
        Assert.True(counter.RecordClaimAndShouldStop());
        Assert.Equal(2, counter.CompletedClaims);
    }
}
