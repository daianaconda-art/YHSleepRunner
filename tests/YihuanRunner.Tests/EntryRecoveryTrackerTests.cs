using YihuanRunner.Automation;

namespace YihuanRunner.Tests;

public sealed class EntryRecoveryTrackerTests
{
    [Fact]
    public void Observe_requests_recovery_after_configured_unresolved_probes()
    {
        var tracker = new EntryRecoveryTracker(requiredUnresolvedProbes: 2);

        Assert.False(tracker.Observe(resolvedScreen: false));
        Assert.True(tracker.Observe(resolvedScreen: false));
    }

    [Fact]
    public void Observe_resets_after_start_or_claim_screen_is_seen()
    {
        var tracker = new EntryRecoveryTracker(requiredUnresolvedProbes: 2);

        Assert.False(tracker.Observe(resolvedScreen: false));
        Assert.False(tracker.Observe(resolvedScreen: true));
        Assert.False(tracker.Observe(resolvedScreen: false));
        Assert.True(tracker.Observe(resolvedScreen: false));
    }
}
