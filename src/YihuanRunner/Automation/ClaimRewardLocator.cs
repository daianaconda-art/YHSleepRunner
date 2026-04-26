namespace YihuanRunner.Automation;

public static class ClaimRewardLocator
{
    public static ClientPoint? FindClaimRewardCenter(
        IEnumerable<OcrLineHit> hits,
        RelativeRegion region,
        int clientWidth,
        int clientHeight)
    {
        foreach (OcrLineHit hit in hits)
        {
            if (!OcrTextMatcher.ContainsClaimReward(hit.Text))
                continue;

            return new ClientPoint(
                region.ToClientX(hit.CenterXPct, clientWidth),
                region.ToClientY(hit.CenterYPct, clientHeight));
        }

        return null;
    }
}
