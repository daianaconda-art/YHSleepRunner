namespace YihuanRunner.Tests;

public sealed class PostClaimKeyboardStepTests
{
    [Fact]
    public void KeyboardInput_defines_the_f_virtual_key_used_after_claiming_rewards()
    {
        Assert.Equal(0x46, YihuanRunner.Platform.KeyboardInput.VirtualKeyF);
        Assert.Equal(0x21, YihuanRunner.Platform.KeyboardInput.ScanCodeF);
    }

    [Fact]
    public void Program_wires_keyboard_input_into_business_loop_runner()
    {
        string repoRoot = FindRepoRoot();
        string program = File.ReadAllText(Path.Combine(repoRoot, "src", "YihuanRunner", "Program.cs"));

        Assert.Contains("new KeyboardInput()", program);
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
