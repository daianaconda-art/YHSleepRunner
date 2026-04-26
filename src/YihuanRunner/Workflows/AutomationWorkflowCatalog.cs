namespace YihuanRunner.Workflows;

public static class AutomationWorkflowCatalog
{
    public static IReadOnlyList<AutomationWorkflowDefinition> CreateDefault(string repositoryRoot) =>
    [
        new AutomationWorkflowDefinition(
            Id: "store-special-2-8",
            DisplayName: "店长特供2-8",
            FileName: "powershell.exe",
            Arguments: ["-NoLogo", "-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-WindowStyle", "Hidden", "-File", @".\scripts\run-yihuan.ps1"],
            WorkingDirectory: repositoryRoot),
    ];
}
