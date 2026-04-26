using YihuanRunner;

namespace YihuanRunner.Tests;

public sealed class AppStartupModeTests
{
    [Fact]
    public void Resolve_opens_ui_when_no_arguments_are_provided()
    {
        Assert.Equal(AppStartupMode.Ui, AppStartupModeResolver.Resolve([]));
    }

    [Fact]
    public void Resolve_opens_ui_for_explicit_ui_argument()
    {
        Assert.Equal(AppStartupMode.Ui, AppStartupModeResolver.Resolve(["--ui"]));
    }

    [Fact]
    public void Resolve_keeps_existing_ocr_cli_for_automation_arguments()
    {
        Assert.Equal(AppStartupMode.Cli, AppStartupModeResolver.Resolve(["--process", "HTGame"]));
        Assert.Equal(AppStartupMode.Cli, AppStartupModeResolver.Resolve(["--probe"]));
        Assert.Equal(AppStartupMode.Cli, AppStartupModeResolver.Resolve(["--help"]));
    }
}
