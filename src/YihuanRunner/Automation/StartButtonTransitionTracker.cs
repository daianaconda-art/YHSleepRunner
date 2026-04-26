namespace YihuanRunner.Automation;

public sealed class StartButtonTransitionTracker(int requiredMissingProbes)
{
    private readonly int _requiredMissingProbes = Math.Max(1, requiredMissingProbes);
    private int _missingProbeCount;

    public bool Observe(bool startButtonVisible)
    {
        if (startButtonVisible)
        {
            _missingProbeCount = 0;
            return false;
        }

        _missingProbeCount++;
        return _missingProbeCount >= _requiredMissingProbes;
    }
}
