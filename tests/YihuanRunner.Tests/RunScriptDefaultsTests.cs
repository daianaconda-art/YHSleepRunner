using System.Text.RegularExpressions;

namespace YihuanRunner.Tests;

public sealed class RunScriptDefaultsTests
{
    [Fact]
    public void RunScript_uses_detected_hammer_defaults()
    {
        string repoRoot = FindRepoRoot();
        string script = File.ReadAllText(Path.Combine(repoRoot, "scripts", "run-yihuan.ps1"));

        Assert.Matches(new Regex(@"\[double\]\$HammerX\s*=\s*0\.0667\b"), script);
        Assert.Matches(new Regex(@"\[double\]\$HammerY\s*=\s*0\.4620\b"), script);
    }

    [Fact]
    public void RunScript_forwards_loop_count_to_runner()
    {
        string repoRoot = FindRepoRoot();
        string script = File.ReadAllText(Path.Combine(repoRoot, "scripts", "run-yihuan.ps1"));

        Assert.Matches(new Regex(@"\[int\]\$Loops\s*=\s*0\b"), script);
        Assert.Contains("--loops", script);
        Assert.Contains("$Loops", script);
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
