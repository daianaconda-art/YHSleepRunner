namespace YihuanRunner.Automation;

public static class RandomDelayPlanner
{
    public static TimeSpan NextDelay(Random rng, int minMs, int maxMs)
    {
        ArgumentNullException.ThrowIfNull(rng);
        if (minMs <= 0)
            throw new ArgumentOutOfRangeException(nameof(minMs), "Minimum delay must be positive.");
        if (maxMs < minMs)
            throw new ArgumentOutOfRangeException(nameof(maxMs), "Maximum delay must be greater than or equal to minimum delay.");

        return TimeSpan.FromMilliseconds(rng.Next(minMs, maxMs + 1));
    }
}
