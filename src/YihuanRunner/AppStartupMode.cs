namespace YihuanRunner;

public enum AppStartupMode
{
    Ui,
    Cli,
}

public static class AppStartupModeResolver
{
    public static AppStartupMode Resolve(IReadOnlyList<string> args)
    {
        if (args.Count == 0)
            return AppStartupMode.Ui;

        if (args.Count == 1 && string.Equals(args[0], "--ui", StringComparison.OrdinalIgnoreCase))
            return AppStartupMode.Ui;

        return AppStartupMode.Cli;
    }
}
