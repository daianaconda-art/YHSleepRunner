using YihuanRunner.Automation;

namespace YihuanRunner.Tests;

public sealed class StartButtonTransitionTrackerTests
{
    [Fact]
    public void Observe_requires_configured_consecutive_missing_start_button_probes()
    {
        var tracker = new StartButtonTransitionTracker(requiredMissingProbes: 2);

        Assert.False(tracker.Observe(startButtonVisible: true));
        Assert.False(tracker.Observe(startButtonVisible: false));
        Assert.True(tracker.Observe(startButtonVisible: false));
    }

    [Fact]
    public void Observe_resets_missing_count_when_start_button_reappears()
    {
        var tracker = new StartButtonTransitionTracker(requiredMissingProbes: 2);

        Assert.False(tracker.Observe(startButtonVisible: false));
        Assert.False(tracker.Observe(startButtonVisible: true));
        Assert.False(tracker.Observe(startButtonVisible: false));
        Assert.True(tracker.Observe(startButtonVisible: false));
    }
}
