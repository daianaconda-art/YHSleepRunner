namespace YihuanRunner.Tests;

public sealed class ProgramDpiModeTests
{
    [Fact]
    public void Program_uses_per_monitor_dpi_awareness_for_physical_click_coordinates()
    {
        string repoRoot = FindRepoRoot();
        string program = File.ReadAllText(Path.Combine(repoRoot, "src", "YihuanRunner", "Program.cs"));

        Assert.Contains("HighDpiMode.PerMonitorV2", program);
        Assert.DoesNotContain("HighDpiMode.DpiUnaware", program);
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
