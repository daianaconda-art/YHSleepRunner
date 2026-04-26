using System.Windows.Forms;
using YihuanRunner.Forms.Controls;
using YihuanRunner.Workflows;

namespace YihuanRunner.Forms;

public sealed class MainWindow : Form
{
    private const int Pad = 18;
    private const int ButtonHeight = 46;
    private const int ButtonGap = 10;

    private readonly IReadOnlyList<AutomationWorkflowDefinition> _workflows;
    private readonly IAutomationWorkflowController _controller;
    private readonly Label _statusLabel;
    private readonly List<RoundedButton> _workflowButtons = [];
    private readonly RoundedButton _stopButton;

    public MainWindow()
        : this(
            AutomationWorkflowCatalog.CreateDefault(RepositoryPaths.FindRepositoryRoot()),
            new AutomationWorkflowController())
    {
    }

    public MainWindow(
        IReadOnlyList<AutomationWorkflowDefinition> workflows,
        IAutomationWorkflowController controller)
    {
        _workflows = workflows;
        _controller = controller;

        Text = "YHSleepRunner";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = true;
        ClientSize = new Size(390, ComputeClientHeight(_workflows.Count));
        BackColor = RunnerTheme.Bg;
        ForeColor = RunnerTheme.TextPrimary;
        Font = RunnerTheme.BodyFont();

        var title = new Label
        {
            AutoSize = false,
            Text = "YHSleepRunner",
            Font = RunnerTheme.BoldFont(15F),
            ForeColor = RunnerTheme.TextPrimary,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleLeft,
            Location = new Point(Pad, 14),
            Size = new Size(ClientSize.Width - Pad * 2, 30),
        };

        _statusLabel = new Label
        {
            AutoSize = false,
            Text = "空闲",
            Font = RunnerTheme.CaptionFont(),
            ForeColor = RunnerTheme.TextSecondary,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleLeft,
            Location = new Point(Pad, 48),
            Size = new Size(ClientSize.Width - Pad * 2, 24),
        };

        var panel = new Panel
        {
            BackColor = RunnerTheme.Panel,
            Location = new Point(Pad, 86),
            Size = new Size(ClientSize.Width - Pad * 2, ComputePanelHeight(_workflows.Count)),
        };
        panel.Paint += (_, eventArgs) =>
        {
            eventArgs.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var path = RunnerTheme.BuildRoundedPath(new Rectangle(0, 0, panel.Width, panel.Height), 12);
            using var fill = new SolidBrush(RunnerTheme.Panel);
            using var border = new Pen(RunnerTheme.Border, 1);
            eventArgs.Graphics.FillPath(fill, path);
            eventArgs.Graphics.DrawPath(border, path);
        };

        foreach (AutomationWorkflowDefinition workflow in _workflows)
        {
            var button = new RoundedButton
            {
                Name = "ActionButton",
                Text = workflow.DisplayName,
                Tag = workflow,
                Variant = RoundedButton.ButtonVariant.Primary,
                Icon = RoundedButton.LeadingIcon.Play,
                AccentColor = RunnerTheme.Accent,
                Font = RunnerTheme.BoldFont(11.25F),
                ForeColor = Color.White,
                Height = ButtonHeight,
            };
            button.Click += OnWorkflowButtonClick;
            _workflowButtons.Add(button);
            panel.Controls.Add(button);
        }

        _stopButton = new RoundedButton
        {
            Name = "ActionButton",
            Text = "停止",
            Variant = RoundedButton.ButtonVariant.Secondary,
            Icon = RoundedButton.LeadingIcon.Stop,
            AccentColor = RunnerTheme.Danger,
            Font = RunnerTheme.BoldFont(11.25F),
            ForeColor = RunnerTheme.TextSecondary,
            Height = ButtonHeight,
        };
        _stopButton.Click += async (_, _) => await StopWorkflowAsync();
        panel.Controls.Add(_stopButton);

        Controls.Add(title);
        Controls.Add(_statusLabel);
        Controls.Add(panel);

        _controller.StateChanged += OnControllerStateChanged;
        _controller.ActivityChanged += OnActivityChanged;
        LayoutActionButtons(panel);
        ApplyState(_controller.State);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _controller.Dispose();
        base.OnFormClosing(e);
    }

    private void OnWorkflowButtonClick(object? sender, EventArgs eventArgs)
    {
        if (sender is not RoundedButton { Tag: AutomationWorkflowDefinition workflow })
            return;

        _ = _controller.Start(workflow);
    }

    private async Task StopWorkflowAsync()
    {
        _stopButton.Enabled = false;
        await _controller.StopAsync().ConfigureAwait(true);
    }

    private void OnControllerStateChanged(AutomationWorkflowState state)
    {
        if (IsHandleCreated && InvokeRequired)
        {
            BeginInvoke(() => ApplyState(state));
            return;
        }

        ApplyState(state);
    }

    private void OnActivityChanged(string text)
    {
        if (IsHandleCreated && InvokeRequired)
        {
            BeginInvoke(() => _statusLabel.Text = TrimStatus(text));
            return;
        }

        _statusLabel.Text = TrimStatus(text);
    }

    private void ApplyState(AutomationWorkflowState state)
    {
        bool running = state is AutomationWorkflowState.Running or AutomationWorkflowState.Stopping;
        foreach (RoundedButton button in _workflowButtons)
            button.Enabled = !running;

        _stopButton.Enabled = state == AutomationWorkflowState.Running;
        _statusLabel.Text = state switch
        {
            AutomationWorkflowState.Running => $"运行中: {_controller.ActiveWorkflow?.DisplayName ?? "流程"}",
            AutomationWorkflowState.Stopping => "正在停止...",
            AutomationWorkflowState.Stopped => "已停止",
            AutomationWorkflowState.Failed => "流程异常退出",
            _ => "空闲",
        };
    }

    private static string TrimStatus(string text)
    {
        text = text.Trim();
        return text.Length <= 48 ? text : text[..48];
    }

    private void LayoutActionButtons(Panel panel)
    {
        if (_workflowButtons.Count == 0)
            return;

        int y = Pad / 2;
        if (_workflowButtons.Count == 1)
        {
            int availableWidth = panel.Width - Pad * 2 - ButtonGap;
            int startWidth = (int)(availableWidth * 0.62);
            int stopWidth = availableWidth - startWidth;
            _workflowButtons[0].SetBounds(Pad, y, startWidth, ButtonHeight);
            _stopButton.SetBounds(Pad + startWidth + ButtonGap, y, stopWidth, ButtonHeight);
            return;
        }

        int fullWidth = panel.Width - Pad * 2;
        foreach (RoundedButton button in _workflowButtons)
        {
            button.SetBounds(Pad, y, fullWidth, ButtonHeight);
            y += ButtonHeight + ButtonGap;
        }

        _stopButton.SetBounds(Pad, y, fullWidth, ButtonHeight);
    }

    private static int ComputeClientHeight(int workflowCount) =>
        86 + ComputePanelHeight(workflowCount) + Pad;

    private static int ComputePanelHeight(int workflowCount)
    {
        if (workflowCount <= 1)
            return ButtonHeight + Pad;

        int rows = workflowCount + 1;
        return Pad + rows * ButtonHeight + (rows - 1) * ButtonGap;
    }
}

internal static class RepositoryPaths
{
    public static string FindRepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "YihuanRunner.sln"))
                || File.Exists(Path.Combine(dir.FullName, "scripts", "run-yihuan.ps1")))
                return dir.FullName;

            dir = dir.Parent;
        }

        return Directory.GetCurrentDirectory();
    }
}
