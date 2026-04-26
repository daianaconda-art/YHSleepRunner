namespace YihuanRunner.Automation;

public sealed class ClaimCompletionCounter
{
    public ClaimCompletionCounter(int targetClaims)
    {
        if (targetClaims < 0)
            throw new ArgumentOutOfRangeException(nameof(targetClaims), "领取次数不能小于 0。");

        TargetClaims = targetClaims;
    }

    public int TargetClaims { get; }
    public int CompletedClaims { get; private set; }

    public bool RecordClaimAndShouldStop()
    {
        CompletedClaims++;
        return TargetClaims > 0 && CompletedClaims >= TargetClaims;
    }
}
