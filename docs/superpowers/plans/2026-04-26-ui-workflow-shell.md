# UI Workflow Shell Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a small warm-themed WinForms shell with two action buttons: start the configured OCR automation script and stop the running process.

**Architecture:** Keep the existing command-line OCR runner intact. Add a UI mode resolver, a workflow catalog for future buttons, a process-backed workflow controller, and a compact WinForms main window that depends on the controller instead of starting processes directly.

**Tech Stack:** .NET 8 Windows, WinForms, xUnit, PowerShell script invocation.

---

### Task 1: Entry Mode

**Files:**
- Create: `src/YihuanRunner/AppStartupMode.cs`
- Modify: `src/YihuanRunner/Program.cs`
- Test: `tests/YihuanRunner.Tests/AppStartupModeTests.cs`

- [ ] Write tests proving no args or `--ui` selects UI mode, while automation args select CLI mode.
- [ ] Run the new tests and verify they fail because the resolver does not exist.
- [ ] Add the resolver and update `Program.Main` so UI mode launches WinForms and CLI mode preserves the existing OCR runner.

### Task 2: Workflow Model

**Files:**
- Create: `src/YihuanRunner/Workflows/AutomationWorkflowDefinition.cs`
- Create: `src/YihuanRunner/Workflows/AutomationWorkflowCatalog.cs`
- Test: `tests/YihuanRunner.Tests/AutomationWorkflowCatalogTests.cs`

- [ ] Write tests for the single default workflow: label `店长特供2-8`, PowerShell executable, `-ExecutionPolicy Bypass -File .\scripts\run-yihuan.ps1`, and repository working directory.
- [ ] Run the new tests and verify they fail because the catalog does not exist.
- [ ] Implement the model and catalog with no image assets.

### Task 3: Process Controller

**Files:**
- Create: `src/YihuanRunner/Workflows/AutomationWorkflowController.cs`
- Test: `tests/YihuanRunner.Tests/AutomationWorkflowControllerTests.cs`

- [ ] Write tests for start, duplicate-start guard, stop, and runner exit state transitions using a fake process runner.
- [ ] Run the new tests and verify they fail because the controller does not exist.
- [ ] Implement the controller and a process runner that kills the PowerShell process tree on stop.

### Task 4: Warm UI

**Files:**
- Create: `src/YihuanRunner/Forms/RunnerTheme.cs`
- Create: `src/YihuanRunner/Forms/Controls/RoundedButton.cs`
- Create: `src/YihuanRunner/Forms/MainWindow.cs`
- Test: `tests/YihuanRunner.Tests/Forms/MainWindowTests.cs`
- Test helper: `tests/YihuanRunner.Tests/Forms/WinFormsTestHost.cs`

- [ ] Write tests that the window has exactly two content buttons and uses warm theme colors.
- [ ] Run the new tests and verify they fail because the form does not exist.
- [ ] Implement the form, using vector-drawn button icons only.

### Task 5: README and Verification

**Files:**
- Modify: `README.md`

- [ ] Rewrite README without naming the underlying title.
- [ ] Add usage notes and a disclaimer.
- [ ] Run `dotnet test` and `dotnet build`.
- [ ] Commit and push to `https://github.com/daianaconda-art/YHSleepRunner`.
