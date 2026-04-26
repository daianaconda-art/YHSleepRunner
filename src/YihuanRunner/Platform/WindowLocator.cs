using System.Diagnostics;

namespace YihuanRunner.Platform;

public sealed record WindowInfo(
    IntPtr Handle,
    int ProcessId,
    string ProcessName,
    string Title,
    string ClassName,
    int ClientWidth,
    int ClientHeight);

public sealed class WindowLocator
{
    public WindowInfo? Find(string processName, string titleContains)
    {
        WindowInfo? byProcess = FindByProcess(processName);
        if (byProcess is not null)
            return byProcess;

        return FindByTitle(titleContains);
    }

    private static WindowInfo? FindByProcess(string processName)
    {
        if (string.IsNullOrWhiteSpace(processName))
            return null;

        WindowInfo? best = null;
        foreach (Process process in Process.GetProcessesByName(processName))
        {
            try
            {
                WindowInfo? candidate = BuildCandidate(process.MainWindowHandle, process);
                best = PickLarger(best, candidate);
            }
            finally
            {
                process.Dispose();
            }
        }

        return best;
    }

    private static WindowInfo? FindByTitle(string titleContains)
    {
        if (string.IsNullOrWhiteSpace(titleContains))
            return null;

        WindowInfo? best = null;
        foreach (IntPtr hWnd in WindowNative.EnumerateTopLevelWindows())
        {
            string title = WindowNative.GetTitle(hWnd);
            if (!title.Contains(titleContains, StringComparison.OrdinalIgnoreCase))
                continue;

            WindowInfo? candidate = BuildCandidate(hWnd, null);
            best = PickLarger(best, candidate);
        }

        return best;
    }

    private static WindowInfo? BuildCandidate(IntPtr hWnd, Process? process)
    {
        if (hWnd == IntPtr.Zero || !WindowNative.IsVisible(hWnd))
            return null;

        if (!WindowNative.TryGetClientSize(hWnd, out int width, out int height))
            return null;

        int pid = process?.Id ?? WindowNative.GetProcessId(hWnd);
        string processName = process?.ProcessName ?? TryGetProcessName(pid);
        return new WindowInfo(
            hWnd,
            pid,
            processName,
            WindowNative.GetTitle(hWnd),
            WindowNative.GetClassName(hWnd),
            width,
            height);
    }

    private static WindowInfo? PickLarger(WindowInfo? current, WindowInfo? candidate)
    {
        if (candidate is null)
            return current;
        if (current is null)
            return candidate;

        int currentArea = current.ClientWidth * current.ClientHeight;
        int candidateArea = candidate.ClientWidth * candidate.ClientHeight;
        return candidateArea > currentArea ? candidate : current;
    }

    private static string TryGetProcessName(int pid)
    {
        try
        {
            using Process process = Process.GetProcessById(pid);
            return process.ProcessName;
        }
        catch
        {
            return "";
        }
    }
}
