using YihuanRunner.Workflows;

namespace YihuanRunner.Tests;

public sealed class AutomationWorkflowCatalogTests
{
    [Fact]
    public void CreateDefault_exposes_store_special_2_to_8_power_shell_workflow()
    {
        string repoRoot = FindRepoRoot();

        AutomationWorkflowDefinition workflow = Assert.Single(AutomationWorkflowCatalog.CreateDefault(repoRoot));

        Assert.Equal("store-special-2-8", workflow.Id);
        Assert.Equal("店长特供2-8", workflow.DisplayName);
        Assert.Equal("powershell", workflow.FileName);
        Assert.Equal(repoRoot, workflow.WorkingDirectory);
        Assert.Equal(
            ["-ExecutionPolicy", "Bypass", "-File", @".\scripts\run-yihuan.ps1"],
            workflow.Arguments);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "YihuanRunner.sln")))
                return dir.FullName;

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }
}
