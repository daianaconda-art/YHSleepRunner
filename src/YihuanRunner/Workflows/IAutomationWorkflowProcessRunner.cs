namespace YihuanRunner.Workflows;

public interface IAutomationWorkflowProcessRunner : IDisposable
{
    event Action<int?>? Exited;
    event Action<string>? OutputReceived;

    void Start(AutomationWorkflowDefinition workflow);
    Task StopAsync();
}
