using YihuanRunner.Platform;

namespace YihuanRunner.Tests;

public sealed class InputPrivilegeGuardTests
{
    [Theory]
    [InlineData(ProcessIntegrityLevel.Medium, ProcessIntegrityLevel.High, true)]
    [InlineData(ProcessIntegrityLevel.Medium, ProcessIntegrityLevel.System, true)]
    [InlineData(ProcessIntegrityLevel.High, ProcessIntegrityLevel.Medium, false)]
    [InlineData(ProcessIntegrityLevel.High, ProcessIntegrityLevel.High, false)]
    [InlineData(ProcessIntegrityLevel.Unknown, ProcessIntegrityLevel.High, false)]
    public void IsInputBlockedByIntegrity_detects_when_runner_is_lower_than_target(
        ProcessIntegrityLevel runner,
        ProcessIntegrityLevel target,
        bool expected)
    {
        Assert.Equal(expected, InputPrivilegeGuard.IsInputBlockedByIntegrity(runner, target));
    }

    [Fact]
    public void CreateBlockedInputMessage_tells_user_to_run_as_administrator()
    {
        string message = InputPrivilegeGuard.CreateBlockedInputMessage(
            ProcessIntegrityLevel.Medium,
            ProcessIntegrityLevel.High);

        Assert.Contains("管理员", message);
        Assert.Contains("SendInput", message);
    }
}
