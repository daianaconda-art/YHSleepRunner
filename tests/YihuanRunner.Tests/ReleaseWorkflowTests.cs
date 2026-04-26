namespace YihuanRunner.Tests;

public sealed class ReleaseWorkflowTests
{
    [Fact]
    public void Release_workflow_builds_zip_and_uploads_it_to_github_release()
    {
        string repoRoot = FindRepoRoot();
        string workflow = File.ReadAllText(Path.Combine(repoRoot, ".github", "workflows", "release.yml"));

        Assert.Contains("tags:", workflow);
        Assert.Contains("'v*'", workflow);
        Assert.Contains("contents: write", workflow);
        Assert.Contains("dotnet test YihuanRunner.sln", workflow);
        Assert.Contains("dotnet publish src/YihuanRunner/YihuanRunner.csproj", workflow);
        Assert.Contains("YHSleepRunner-win-x64.zip", workflow);
        Assert.Contains("gh release", workflow);
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
