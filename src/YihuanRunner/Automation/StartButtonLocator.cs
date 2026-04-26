namespace YihuanRunner.Automation;

public static class StartButtonLocator
{
    public static ClientPoint? FindStartButtonCenter(
        IEnumerable<OcrLineHit> hits,
        RelativeRegion region,
        int clientWidth,
        int clientHeight)
    {
        foreach (OcrLineHit hit in hits)
        {
            if (!OcrTextMatcher.ContainsStartBusiness(hit.Text))
                continue;

            return new ClientPoint(
                region.ToClientX(hit.CenterXPct, clientWidth),
                region.ToClientY(hit.CenterYPct, clientHeight));
        }

        return null;
    }
}
