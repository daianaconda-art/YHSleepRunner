using System.Text.RegularExpressions;

namespace YihuanRunner.Tests;

public sealed class PublishPackagingTests
{
    [Fact]
    public void Project_publishes_with_public_launcher_name()
    {
        string repoRoot = FindRepoRoot();
        string project = File.ReadAllText(Path.Combine(repoRoot, "src", "YihuanRunner", "YihuanRunner.csproj"));

        Assert.Contains("<AssemblyName>YHSleepRunner</AssemblyName>", project);
    }

    [Fact]
    public void RunScript_prefers_packaged_executable_before_dotnet_run()
    {
        string repoRoot = FindRepoRoot();
        string script = File.ReadAllText(Path.Combine(repoRoot, "scripts", "run-yihuan.ps1"));

        Assert.Contains("\"YHSleepRunner.exe\"", script);
        Assert.Matches(new Regex(@"Test-Path\s+-LiteralPath\s+\$publishedRunner"), script);
        Assert.Matches(new Regex(@"&\s+\$publishedRunner\s+@runnerArgs"), script);
        Assert.Matches(new Regex(@"dotnet\s+run\s+--project\s+\$project\s+--\s+@runnerArgs"), script);
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
