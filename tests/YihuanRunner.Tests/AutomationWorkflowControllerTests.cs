using YihuanRunner.Workflows;

namespace YihuanRunner.Tests;

public sealed class AutomationWorkflowControllerTests
{
    [Fact]
    public void Start_launches_workflow_and_moves_to_running_state()
    {
        var runner = new FakeWorkflowProcessRunner();
        using var controller = new AutomationWorkflowController(runner);
        var states = new List<AutomationWorkflowState>();
        controller.StateChanged += states.Add;

        bool started = controller.Start(SampleWorkflow());

        Assert.True(started);
        Assert.Equal(AutomationWorkflowState.Running, controller.State);
        Assert.Equal("店长特供2-8", controller.ActiveWorkflow?.DisplayName);
        Assert.Equal(1, runner.StartCount);
        Assert.Equal([AutomationWorkflowState.Running], states);
    }

    [Fact]
    public void Start_ignores_duplicate_request_while_running()
    {
        var runner = new FakeWorkflowProcessRunner();
        using var controller = new AutomationWorkflowController(runner);

        bool first = controller.Start(SampleWorkflow());
        bool second = controller.Start(SampleWorkflow());

        Assert.True(first);
        Assert.False(second);
        Assert.Equal(1, runner.StartCount);
    }

    [Fact]
    public async Task StopAsync_stops_runner_and_moves_back_to_stopped_state()
    {
        var runner = new FakeWorkflowProcessRunner();
        using var controller = new AutomationWorkflowController(runner);
        var states = new List<AutomationWorkflowState>();
        controller.StateChanged += states.Add;

        controller.Start(SampleWorkflow());
        await controller.StopAsync();

        Assert.Equal(AutomationWorkflowState.Stopped, controller.State);
        Assert.True(runner.StopCalled);
        Assert.Contains(AutomationWorkflowState.Stopping, states);
        Assert.Equal(AutomationWorkflowState.Stopped, states[^1]);
    }

    [Fact]
    public void Runner_exit_moves_controller_to_stopped_state()
    {
        var runner = new FakeWorkflowProcessRunner();
        using var controller = new AutomationWorkflowController(runner);

        controller.Start(SampleWorkflow());
        runner.RaiseExited(0);

        Assert.Equal(AutomationWorkflowState.Stopped, controller.State);
        Assert.Null(controller.ActiveWorkflow);
    }

    private static AutomationWorkflowDefinition SampleWorkflow() =>
        new(
            Id: "store-special-2-8",
            DisplayName: "店长特供2-8",
            FileName: "powershell",
            Arguments: ["-ExecutionPolicy", "Bypass", "-File", @".\scripts\run-yihuan.ps1"],
            WorkingDirectory: "C:\\repo");

    private sealed class FakeWorkflowProcessRunner : IAutomationWorkflowProcessRunner
    {
        public event Action<int?>? Exited;
        public event Action<string>? OutputReceived;

        public int StartCount { get; private set; }
        public bool StopCalled { get; private set; }

        public void Start(AutomationWorkflowDefinition workflow)
        {
            StartCount++;
            OutputReceived?.Invoke($"started {workflow.DisplayName}");
        }

        public Task StopAsync()
        {
            StopCalled = true;
            RaiseExited(null);
            return Task.CompletedTask;
        }

        public void RaiseExited(int? exitCode) => Exited?.Invoke(exitCode);

        public void Dispose()
        {
        }
    }
}
