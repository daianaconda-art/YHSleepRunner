using System.Text;

namespace YihuanRunner.Automation;

public static class OcrTextMatcher
{
    public static bool ContainsStartBusiness(string? text)
    {
        string normalized = Normalize(text);
        return normalized.Contains("开始营业", StringComparison.Ordinal);
    }

    public static bool ContainsClaimReward(string? text)
    {
        string normalized = Normalize(text);
        return normalized.Contains("领取", StringComparison.Ordinal);
    }

    public static string Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var sb = new StringBuilder(text.Length);
        foreach (char ch in text)
        {
            if (!char.IsWhiteSpace(ch))
                sb.Append(ch);
        }

        return sb.ToString();
    }
}
