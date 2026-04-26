namespace YihuanRunner.Workflows;

public sealed record AutomationWorkflowDefinition(
    string Id,
    string DisplayName,
    string FileName,
    IReadOnlyList<string> Arguments,
    string WorkingDirectory);
