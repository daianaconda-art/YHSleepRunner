namespace YihuanRunner.Automation;

public sealed class EntryRecoveryTracker(int requiredUnresolvedProbes)
{
    private readonly int _requiredUnresolvedProbes = Math.Max(1, requiredUnresolvedProbes);
    private int _unresolvedProbeCount;

    public bool Observe(bool resolvedScreen)
    {
        if (resolvedScreen)
        {
            _unresolvedProbeCount = 0;
            return false;
        }

        _unresolvedProbeCount++;
        if (_unresolvedProbeCount < _requiredUnresolvedProbes)
            return false;

        _unresolvedProbeCount = 0;
        return true;
    }
}
