using System.Diagnostics;

namespace YihuanRunner.Workflows;

public sealed class ProcessAutomationWorkflowRunner : IAutomationWorkflowProcessRunner
{
    private readonly object _sync = new();
    private Process? _process;
    private bool _disposed;

    public event Action<int?>? Exited;
    public event Action<string>? OutputReceived;

    public void Start(AutomationWorkflowDefinition workflow)
    {
        ThrowIfDisposed();

        lock (_sync)
        {
            if (_process is { HasExited: false })
                throw new InvalidOperationException("已有流程正在运行。");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = workflow.FileName,
            WorkingDirectory = workflow.WorkingDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        foreach (string argument in workflow.Arguments)
            startInfo.ArgumentList.Add(argument);

        var process = new Process
        {
            StartInfo = startInfo,
            EnableRaisingEvents = true,
        };

        process.OutputDataReceived += (_, eventArgs) =>
        {
            if (eventArgs.Data is not null)
                OutputReceived?.Invoke(eventArgs.Data);
        };
        process.ErrorDataReceived += (_, eventArgs) =>
        {
            if (eventArgs.Data is not null)
                OutputReceived?.Invoke(eventArgs.Data);
        };
        process.Exited += (_, _) =>
        {
            int? exitCode = null;
            try { exitCode = process.ExitCode; }
            catch { }

            lock (_sync)
            {
                if (ReferenceEquals(_process, process))
                    _process = null;
            }

            Exited?.Invoke(exitCode);
        };

        if (!process.Start())
            throw new InvalidOperationException("无法启动流程。");

        lock (_sync)
        {
            _process = process;
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
    }

    public async Task StopAsync()
    {
        ThrowIfDisposed();

        Process? process;
        lock (_sync)
        {
            process = _process;
        }

        if (process is null)
            return;

        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);

            await process.WaitForExitAsync().ConfigureAwait(false);
        }
        catch (InvalidOperationException)
        {
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        try
        {
            Process? process;
            lock (_sync)
            {
                process = _process;
                _process = null;
            }

            if (process is { HasExited: false })
                process.Kill(entireProcessTree: true);

            process?.Dispose();
        }
        catch
        {
            // Best-effort cleanup during shutdown.
        }

        _disposed = true;
    }
}
