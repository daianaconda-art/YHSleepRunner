namespace YihuanRunner.Tests;

public sealed class ProgramConsoleModeTests
{
    [Fact]
    public void Program_configures_console_encoding_through_safe_helper_for_windows_subsystem()
    {
        string repoRoot = FindRepoRoot();
        string program = File.ReadAllText(Path.Combine(repoRoot, "src", "YihuanRunner", "Program.cs"));

        Assert.Contains("ConsoleOutput.TryConfigureUtf8", program);
        Assert.DoesNotContain("Console.OutputEncoding = Encoding.UTF8", program);
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
