namespace YihuanRunner.Workflows;

public sealed class AutomationWorkflowController : IAutomationWorkflowController
{
    private readonly object _sync = new();
    private readonly IAutomationWorkflowProcessRunner _runner;
    private bool _disposed;
    private AutomationWorkflowState _state = AutomationWorkflowState.Idle;
    private AutomationWorkflowDefinition? _activeWorkflow;

    public AutomationWorkflowController()
        : this(new ProcessAutomationWorkflowRunner())
    {
    }

    public AutomationWorkflowController(IAutomationWorkflowProcessRunner runner)
    {
        _runner = runner;
        _runner.Exited += OnRunnerExited;
        _runner.OutputReceived += OnRunnerOutputReceived;
    }

    public AutomationWorkflowState State
    {
        get { lock (_sync) return _state; }
    }

    public AutomationWorkflowDefinition? ActiveWorkflow
    {
        get { lock (_sync) return _activeWorkflow; }
    }

    public event Action<AutomationWorkflowState>? StateChanged;
    public event Action<string>? ActivityChanged;

    public bool Start(AutomationWorkflowDefinition workflow)
    {
        ThrowIfDisposed();

        lock (_sync)
        {
            if (_state is AutomationWorkflowState.Running or AutomationWorkflowState.Stopping)
                return false;

            _activeWorkflow = workflow;
            SetStateLocked(AutomationWorkflowState.Running);
        }

        try
        {
            _runner.Start(workflow);
            ActivityChanged?.Invoke($"运行中: {workflow.DisplayName}");
            return true;
        }
        catch (Exception ex)
        {
            lock (_sync)
            {
                _activeWorkflow = null;
                SetStateLocked(AutomationWorkflowState.Failed);
            }

            ActivityChanged?.Invoke($"启动失败: {ex.Message}");
            return false;
        }
    }

    public async Task StopAsync()
    {
        ThrowIfDisposed();

        bool shouldStop;
        lock (_sync)
        {
            shouldStop = _state is AutomationWorkflowState.Running;
            if (shouldStop)
                SetStateLocked(AutomationWorkflowState.Stopping);
        }

        if (!shouldStop)
            return;

        ActivityChanged?.Invoke("正在停止...");
        await _runner.StopAsync().ConfigureAwait(false);

        lock (_sync)
        {
            if (_state == AutomationWorkflowState.Stopping)
            {
                _activeWorkflow = null;
                SetStateLocked(AutomationWorkflowState.Stopped);
            }
        }
    }

    private void OnRunnerExited(int? exitCode)
    {
        lock (_sync)
        {
            bool requestedStop = _state == AutomationWorkflowState.Stopping;
            _activeWorkflow = null;
            SetStateLocked(requestedStop || exitCode == 0
                ? AutomationWorkflowState.Stopped
                : AutomationWorkflowState.Failed);
        }

        ActivityChanged?.Invoke(CreateExitActivity(exitCode));
    }

    private void OnRunnerOutputReceived(string line)
    {
        if (!string.IsNullOrWhiteSpace(line))
            ActivityChanged?.Invoke(line);
    }

    private void SetStateLocked(AutomationWorkflowState next)
    {
        if (_state == next)
            return;

        _state = next;
        StateChanged?.Invoke(next);
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    private static string CreateExitActivity(int? exitCode)
    {
        return exitCode switch
        {
            0 or null => "已停止",
            3 => "权限不足: 请右键以管理员身份运行启动器，或用普通权限启动目标窗口。",
            _ => $"流程退出: {exitCode}",
        };
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        try
        {
            if (State is AutomationWorkflowState.Running or AutomationWorkflowState.Stopping)
                _runner.StopAsync().GetAwaiter().GetResult();
        }
        catch
        {
            // Best-effort cleanup while the application is closing.
        }
        finally
        {
            _runner.Exited -= OnRunnerExited;
            _runner.OutputReceived -= OnRunnerOutputReceived;
            _runner.Dispose();
            _disposed = true;
        }
    }
}
