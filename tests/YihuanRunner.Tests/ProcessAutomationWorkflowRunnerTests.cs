using YihuanRunner.Workflows;

namespace YihuanRunner.Tests;

public sealed class ProcessAutomationWorkflowRunnerTests
{
    [Fact]
    public void BuildStartInfo_hides_child_process_window_and_redirects_output()
    {
        var workflow = new AutomationWorkflowDefinition(
            Id: "flow",
            DisplayName: "Flow",
            FileName: "powershell.exe",
            Arguments: ["-WindowStyle", "Hidden", "-File", @".\scripts\run-yihuan.ps1"],
            WorkingDirectory: "C:\\repo");

        var startInfo = ProcessAutomationWorkflowRunner.BuildStartInfo(workflow);

        Assert.False(startInfo.UseShellExecute);
        Assert.True(startInfo.CreateNoWindow);
        Assert.True(startInfo.RedirectStandardOutput);
        Assert.True(startInfo.RedirectStandardError);
        Assert.Contains("-WindowStyle", startInfo.ArgumentList);
        Assert.Contains("Hidden", startInfo.ArgumentList);
    }
}
