namespace YihuanRunner.Workflows;

public interface IAutomationWorkflowController : IDisposable
{
    AutomationWorkflowState State { get; }
    AutomationWorkflowDefinition? ActiveWorkflow { get; }

    event Action<AutomationWorkflowState>? StateChanged;
    event Action<string>? ActivityChanged;

    bool Start(AutomationWorkflowDefinition workflow);
    Task StopAsync();
}

public enum AutomationWorkflowState
{
    Idle,
    Running,
    Stopping,
    Stopped,
    Failed,
}
