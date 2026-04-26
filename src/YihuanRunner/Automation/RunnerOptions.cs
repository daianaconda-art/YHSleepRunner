using System.Globalization;

namespace YihuanRunner.Automation;

public sealed record RunnerOptions
{
    public string ProcessName { get; init; } = "HTGame";
    public string TitleContains { get; init; } = "异环";
    public RelativeRegion StartButtonRegion { get; init; } = new(0.78, 0.88, 0.20, 0.12);
    public RelativeRegion ClaimRewardRegion { get; init; } = new(0.50, 0.72, 0.22, 0.12);
    public RelativePoint HammerPoint { get; init; } = new(0.0667, 0.4620);
    public TimeSpan LoadingDelay { get; init; } = TimeSpan.FromSeconds(7);
    public TimeSpan BusinessDuration { get; init; } = TimeSpan.FromSeconds(45);
    public int MinHammerDelayMs { get; init; } = 250;
    public int MaxHammerDelayMs { get; init; } = 650;
    public bool RunOnce { get; init; }
    public bool DryRun { get; init; }
    public bool ProbeOnly { get; init; }
    public bool ShowHelp { get; init; }
    public string? SnapshotPath { get; init; }

    public static string Usage =>
        """
        YHSleepRunner - 本地 OCR 自动化启动器

        默认会寻找目标窗口，OCR 指定区域内的入口文本，点击后等待 loading，再按配置执行循环输入。

        常用:
          dotnet run --project .\src\YihuanRunner -- --probe --snapshot probe.png
          dotnet run --project .\src\YihuanRunner -- --once
          dotnet run --project .\src\YihuanRunner --

        参数:
          --process <name>          目标进程名
          --title <text>            窗口标题包含文本
          --probe                   只截图和 OCR，不点击
          --once                    只执行一次营业流程
          --dry-run                 打印将要点击的位置，不实际点击
          --snapshot <path>         保存本次屏幕截图
          --hammer-x <pct>          点击点 X 百分比，默认 0.0667
          --hammer-y <pct>          点击点 Y 百分比，默认 0.4620
          --duration-sec <seconds>  营业点击持续时间，默认 45
          --loading-ms <ms>         点开始后等待 loading 的时间，默认 7000
          --min-ms <ms>             点击最小随机间隔，默认 250
          --max-ms <ms>             点击最大随机间隔，默认 650
          --start-region x,y,w,h    入口 OCR 区域，默认 0.78,0.88,0.20,0.12
          --claim-region x,y,w,h    结算领取 OCR 区域，默认 0.50,0.72,0.22,0.12
        """;

    public static RunnerOptions Parse(IReadOnlyList<string> args)
    {
        var options = new RunnerOptions();

        for (int i = 0; i < args.Count; i++)
        {
            string arg = args[i];
            switch (arg)
            {
                case "--help":
                case "-h":
                    options = options with { ShowHelp = true };
                    break;
                case "--process":
                    options = options with { ProcessName = RequireValue(args, ref i, arg) };
                    break;
                case "--title":
                    options = options with { TitleContains = RequireValue(args, ref i, arg) };
                    break;
                case "--probe":
                    options = options with { ProbeOnly = true };
                    break;
                case "--once":
                    options = options with { RunOnce = true };
                    break;
                case "--dry-run":
                    options = options with { DryRun = true };
                    break;
                case "--snapshot":
                    options = options with { SnapshotPath = RequireValue(args, ref i, arg) };
                    break;
                case "--hammer-x":
                    options = options with { HammerPoint = options.HammerPoint with { X = ParseDouble(RequireValue(args, ref i, arg), arg) } };
                    break;
                case "--hammer-y":
                    options = options with { HammerPoint = options.HammerPoint with { Y = ParseDouble(RequireValue(args, ref i, arg), arg) } };
                    break;
                case "--duration-sec":
                    options = options with { BusinessDuration = TimeSpan.FromSeconds(ParseDouble(RequireValue(args, ref i, arg), arg)) };
                    break;
                case "--loading-ms":
                    options = options with { LoadingDelay = TimeSpan.FromMilliseconds(ParseInt(RequireValue(args, ref i, arg), arg)) };
                    break;
                case "--min-ms":
                    options = options with { MinHammerDelayMs = ParseInt(RequireValue(args, ref i, arg), arg) };
                    break;
                case "--max-ms":
                    options = options with { MaxHammerDelayMs = ParseInt(RequireValue(args, ref i, arg), arg) };
                    break;
                case "--start-region":
                    options = options with { StartButtonRegion = ParseRegion(RequireValue(args, ref i, arg), arg) };
                    break;
                case "--claim-region":
                    options = options with { ClaimRewardRegion = ParseRegion(RequireValue(args, ref i, arg), arg) };
                    break;
                default:
                    throw new ArgumentException($"未知参数 {arg}");
            }
        }

        _ = RandomDelayPlanner.NextDelay(new Random(1), options.MinHammerDelayMs, options.MaxHammerDelayMs);
        return options;
    }

    private static string RequireValue(IReadOnlyList<string> args, ref int index, string name)
    {
        if (index + 1 >= args.Count)
            throw new ArgumentException($"{name} 需要一个值");

        index++;
        return args[index];
    }

    private static int ParseInt(string value, string name)
    {
        return int.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
    }

    private static double ParseDouble(string value, string name)
    {
        return double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
    }

    private static RelativeRegion ParseRegion(string value, string name)
    {
        string[] parts = value.Split(',', StringSplitOptions.TrimEntries);
        if (parts.Length != 4)
            throw new ArgumentException($"{name} 需要 x,y,w,h 四个数字");

        return new RelativeRegion(
            ParseDouble(parts[0], name),
            ParseDouble(parts[1], name),
            ParseDouble(parts[2], name),
            ParseDouble(parts[3], name));
    }
}
