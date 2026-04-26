using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using YihuanRunner.Forms.Controls;
using YihuanRunner.Workflows;

namespace YihuanRunner.Forms;

public sealed class MainWindow : Form
{
    private const int DefaultClientWidth = 540;
    private const int DefaultClientHeight = 292;
    private const int MinimumWindowWidth = 460;
    private const int MinimumWindowHeight = 270;
    private const int Pad = 24;
    private const int ButtonHeight = 54;
    private const int ButtonGap = 12;
    private const int SettingsTop = 92;
    private const int ActionPanelTop = 156;
    private const int WmHotKey = 0x0312;
    private const int WmKeyDown = 0x0100;
    private const int WmSysKeyDown = 0x0104;
    private const int WhKeyboardLl = 13;
    private const int StopHotKeyId = 0x5151;
    private const uint ModAlt = 0x0001;
    private const uint ModNoRepeat = 0x4000;
    private const uint LowLevelKeyboardAltDown = 0x20;
    private const uint VirtualKeyQ = 0x51;

    private readonly IReadOnlyList<AutomationWorkflowDefinition> _workflows;
    private readonly IAutomationWorkflowController _controller;
    private readonly Label _titleLabel;
    private readonly Label _statusLabel;
    private readonly Label _loopCountLabel;
    private readonly NumericUpDown _loopCountInput;
    private readonly Panel _actionPanel;
    private readonly List<RoundedButton> _workflowButtons = [];
    private readonly RoundedButton _stopButton;
    private bool _stopHotKeyRegistered;
    private IntPtr _stopShortcutHook;
    private LowLevelKeyboardProc? _stopShortcutHookProc;

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
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        MinimizeBox = true;
        MinimumSize = new Size(MinimumWindowWidth, MinimumWindowHeight);
        ClientSize = new Size(DefaultClientWidth, Math.Max(DefaultClientHeight, ComputeClientHeight(_workflows.Count)));
        KeyPreview = true;
        BackColor = RunnerTheme.Bg;
        ForeColor = RunnerTheme.TextPrimary;
        Font = RunnerTheme.BodyFont();

        _titleLabel = new Label
        {
            AutoSize = false,
            Text = "YHSleepRunner",
            Font = RunnerTheme.BoldFont(12F),
            ForeColor = RunnerTheme.TextPrimary,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleLeft,
            Location = new Point(Pad, 14),
            Size = new Size(ClientSize.Width - Pad * 2, 40),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
        };

        _statusLabel = new Label
        {
            AutoSize = false,
            Text = "空闲",
            Font = RunnerTheme.CaptionFont(),
            ForeColor = RunnerTheme.TextSecondary,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleLeft,
            Location = new Point(Pad, 58),
            Size = new Size(ClientSize.Width - Pad * 2, 26),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
        };

        _loopCountLabel = new Label
        {
            AutoSize = false,
            Text = "循环次数",
            Font = RunnerTheme.CaptionFont(),
            ForeColor = RunnerTheme.TextSecondary,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleLeft,
            Location = new Point(Pad, SettingsTop + 5),
            Size = new Size(74, 32),
        };

        _loopCountInput = new NumericUpDown
        {
            Name = "LoopCountInput",
            Minimum = 0,
            Maximum = 999,
            Value = 0,
            Font = RunnerTheme.BoldFont(11.25F),
            ForeColor = RunnerTheme.TextPrimary,
            BackColor = RunnerTheme.Panel,
            Location = new Point(Pad + 82, SettingsTop),
            Size = new Size(96, 34),
            TextAlign = HorizontalAlignment.Center,
        };

        _actionPanel = new Panel
        {
            BackColor = RunnerTheme.Panel,
            Location = new Point(Pad, ActionPanelTop),
            Size = new Size(ClientSize.Width - Pad * 2, ComputePanelHeight(_workflows.Count)),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
        };
        _actionPanel.Paint += (_, eventArgs) =>
        {
            eventArgs.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var path = RunnerTheme.BuildRoundedPath(new Rectangle(0, 0, _actionPanel.Width, _actionPanel.Height), 12);
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
            _actionPanel.Controls.Add(button);
        }

        _stopButton = new RoundedButton
        {
            Name = "ActionButton",
            Text = "停止  Alt+Q",
            Variant = RoundedButton.ButtonVariant.Secondary,
            Icon = RoundedButton.LeadingIcon.Stop,
            AccentColor = RunnerTheme.Danger,
            Font = RunnerTheme.BoldFont(11.25F),
            ForeColor = RunnerTheme.TextSecondary,
            Height = ButtonHeight,
        };
        _stopButton.Click += async (_, _) => await StopWorkflowAsync();
        _actionPanel.Controls.Add(_stopButton);

        Controls.Add(_titleLabel);
        Controls.Add(_statusLabel);
        Controls.Add(_loopCountLabel);
        Controls.Add(_loopCountInput);
        Controls.Add(_actionPanel);

        _controller.StateChanged += OnControllerStateChanged;
        _controller.ActivityChanged += OnActivityChanged;
        LayoutActionButtons();
        ApplyState(_controller.State);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);

        if (_actionPanel is null)
            return;

        int contentWidth = Math.Max(1, ClientSize.Width - Pad * 2);
        _titleLabel.Width = contentWidth;
        _statusLabel.Width = contentWidth;
        _actionPanel.Width = contentWidth;
        LayoutActionButtons();
        _actionPanel.Invalidate();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        RegisterStopHotKey();
    }

    protected override void OnHandleDestroyed(EventArgs e)
    {
        UnregisterStopHotKey();
        base.OnHandleDestroyed(e);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _controller.Dispose();
        base.OnFormClosing(e);
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == (Keys.Alt | Keys.Q))
        {
            RequestStopFromShortcut();
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WmHotKey && m.WParam.ToInt32() == StopHotKeyId)
        {
            RequestStopFromShortcut();
            return;
        }

        base.WndProc(ref m);
    }

    private void RequestStopFromShortcut()
    {
        if (_controller.State == AutomationWorkflowState.Running)
            _ = StopWorkflowAsync();
    }

    private void RegisterStopHotKey()
    {
        if (_stopHotKeyRegistered || _stopShortcutHook != IntPtr.Zero || !IsHandleCreated)
            return;

        if (RegisterHotKey(Handle, StopHotKeyId, ModAlt | ModNoRepeat, VirtualKeyQ))
        {
            _stopHotKeyRegistered = true;
            return;
        }

        InstallStopShortcutHook();
    }

    private void UnregisterStopHotKey()
    {
        if (_stopHotKeyRegistered)
        {
            UnregisterHotKey(Handle, StopHotKeyId);
            _stopHotKeyRegistered = false;
        }

        if (_stopShortcutHook != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_stopShortcutHook);
            _stopShortcutHook = IntPtr.Zero;
            _stopShortcutHookProc = null;
        }
    }

    private void InstallStopShortcutHook()
    {
        _stopShortcutHookProc = StopShortcutHookProc;
        _stopShortcutHook = SetWindowsHookEx(
            WhKeyboardLl,
            _stopShortcutHookProc,
            GetModuleHandle(null),
            0);

        if (_stopShortcutHook == IntPtr.Zero)
            _stopShortcutHookProc = null;
    }

    private IntPtr StopShortcutHookProc(int code, IntPtr wParam, IntPtr lParam)
    {
        if (code >= 0)
        {
            var input = Marshal.PtrToStructure<LowLevelKeyboardInput>(lParam);
            if (HandleLowLevelKeyboardMessage(wParam.ToInt32(), (int)input.VirtualKeyCode, input.Flags))
                return 1;
        }

        return CallNextHookEx(_stopShortcutHook, code, wParam, lParam);
    }

    private bool HandleLowLevelKeyboardMessage(int message, int virtualKey, uint flags)
    {
        bool isKeyDown = message is WmKeyDown or WmSysKeyDown;
        bool isAltDown = (flags & LowLevelKeyboardAltDown) != 0;
        if (!isKeyDown || virtualKey != VirtualKeyQ || !isAltDown)
            return false;

        if (_controller.State != AutomationWorkflowState.Running)
            return false;

        RequestStopFromShortcut();
        return true;
    }

    private void OnWorkflowButtonClick(object? sender, EventArgs eventArgs)
    {
        if (sender is not RoundedButton { Tag: AutomationWorkflowDefinition workflow })
            return;

        _ = _controller.Start(ApplyLoopCount(workflow));
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

        _loopCountInput.Enabled = !running;
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

    private AutomationWorkflowDefinition ApplyLoopCount(AutomationWorkflowDefinition workflow)
    {
        int loopCount = decimal.ToInt32(_loopCountInput.Value);
        if (loopCount <= 0)
            return workflow;

        return workflow with
        {
            Arguments = workflow.Arguments
                .Concat(["-Loops", loopCount.ToString(CultureInfo.InvariantCulture)])
                .ToArray(),
        };
    }

    private void LayoutActionButtons()
    {
        if (_workflowButtons.Count == 0)
            return;

        int y = Pad / 2;
        if (_workflowButtons.Count == 1)
        {
            int availableWidth = _actionPanel.Width - Pad * 2 - ButtonGap;
            int startWidth = (int)(availableWidth * 0.62);
            int stopWidth = availableWidth - startWidth;
            _workflowButtons[0].SetBounds(Pad, y, startWidth, ButtonHeight);
            _stopButton.SetBounds(Pad + startWidth + ButtonGap, y, stopWidth, ButtonHeight);
            return;
        }

        int fullWidth = _actionPanel.Width - Pad * 2;
        foreach (RoundedButton button in _workflowButtons)
        {
            button.SetBounds(Pad, y, fullWidth, ButtonHeight);
            y += ButtonHeight + ButtonGap;
        }

        _stopButton.SetBounds(Pad, y, fullWidth, ButtonHeight);
    }

    private static int ComputeClientHeight(int workflowCount) =>
        ActionPanelTop + ComputePanelHeight(workflowCount) + Pad;

    private static int ComputePanelHeight(int workflowCount)
    {
        if (workflowCount <= 1)
            return ButtonHeight + Pad;

        int rows = workflowCount + 1;
        return Pad + rows * ButtonHeight + (rows - 1) * ButtonGap;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(
        int idHook,
        LowLevelKeyboardProc lpfn,
        IntPtr hMod,
        uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct LowLevelKeyboardInput
    {
        public uint VirtualKeyCode;
        public uint ScanCode;
        public uint Flags;
        public uint Time;
        public IntPtr ExtraInfo;
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
