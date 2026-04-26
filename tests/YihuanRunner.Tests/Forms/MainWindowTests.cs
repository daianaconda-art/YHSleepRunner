using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using YihuanRunner.Forms;
using YihuanRunner.Workflows;

namespace YihuanRunner.Tests.Forms;

public sealed class MainWindowTests
{
    [Fact]
    public void MainWindow_contains_only_start_and_stop_buttons()
    {
        WinFormsTestHost.Run(() =>
        {
            using var controller = new FakeAutomationWorkflowController();
            using var form = new MainWindow([SampleWorkflow()], controller);
            form.CreateControl();
            form.Show();

            var buttons = form.Controls.Find("ActionButton", searchAllChildren: true).OfType<Button>().ToList();

            Assert.Equal(2, buttons.Count);
            Assert.Contains(buttons, button => button.Text == "店长特供2-8");
            Assert.Contains(buttons, button => button.Text.Contains("停止"));
        });
    }

    [Fact]
    public void MainWindow_uses_warm_theme_colors()
    {
        WinFormsTestHost.Run(() =>
        {
            using var controller = new FakeAutomationWorkflowController();
            using var form = new MainWindow([SampleWorkflow()], controller);
            form.CreateControl();

            Assert.Equal(RunnerTheme.Bg.ToArgb(), form.BackColor.ToArgb());
            Assert.Equal(RunnerTheme.TextPrimary.ToArgb(), form.ForeColor.ToArgb());
        });
    }

    [Fact]
    public void MainWindow_keeps_app_title_compact_inside_the_header()
    {
        WinFormsTestHost.Run(() =>
        {
            using var controller = new FakeAutomationWorkflowController();
            using var form = new MainWindow([SampleWorkflow()], controller);
            form.CreateControl();

            var title = form.Controls.OfType<Label>().Single(label => label.Text == "YHSleepRunner");
            Size textSize = TextRenderer.MeasureText(
                title.Text,
                title.Font,
                new Size(int.MaxValue, int.MaxValue),
                TextFormatFlags.NoPadding);

            Assert.True(
                title.Font.SizeInPoints <= 14F,
                $"Title font was {title.Font.SizeInPoints}pt.");
            Assert.True(
                textSize.Height + 8 <= title.Height,
                $"Title text height was {textSize.Height}px inside a {title.Height}px label.");
        });
    }

    [Fact]
    public void MainWindow_uses_roomier_resizable_shell()
    {
        WinFormsTestHost.Run(() =>
        {
            using var controller = new FakeAutomationWorkflowController();
            using var form = new MainWindow([SampleWorkflow()], controller);
            form.CreateControl();

            Assert.Equal(FormBorderStyle.Sizable, form.FormBorderStyle);
            Assert.True(form.MaximizeBox);
            Assert.True(form.ClientSize.Width >= 500);
            Assert.True(form.ClientSize.Height >= 220);
            Assert.True(form.MinimumSize.Width >= 460);
            Assert.True(form.MinimumSize.Height >= 220);
        });
    }

    [Fact]
    public void MainWindow_exposes_loop_count_input_defaulting_to_unlimited()
    {
        WinFormsTestHost.Run(() =>
        {
            using var controller = new FakeAutomationWorkflowController();
            using var form = new MainWindow([SampleWorkflow()], controller);
            form.CreateControl();

            var loopCount = form.Controls.Find("LoopCountInput", searchAllChildren: true)
                .OfType<NumericUpDown>()
                .Single();

            Assert.Equal(0, loopCount.Value);
            Assert.Equal(0, loopCount.Minimum);
        });
    }

    [Fact]
    public void MainWindow_forwards_loop_count_when_starting_workflow()
    {
        WinFormsTestHost.Run(() =>
        {
            using var controller = new FakeAutomationWorkflowController();
            using var form = new MainWindow([SampleWorkflow()], controller);
            form.CreateControl();

            var loopCount = form.Controls.Find("LoopCountInput", searchAllChildren: true)
                .OfType<NumericUpDown>()
                .Single();
            loopCount.Value = 3;

            var startButton = form.Controls.Find("ActionButton", searchAllChildren: true)
                .OfType<Button>()
                .Single(button => button.Text == "店长特供2-8");

            typeof(Button).GetMethod("OnClick", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(startButton, [EventArgs.Empty]);

            Assert.NotNull(controller.StartedWorkflow);
            Assert.Equal(
                ["-ExecutionPolicy", "Bypass", "-File", @".\scripts\run-yihuan.ps1", "-Loops", "3"],
                controller.StartedWorkflow!.Arguments);
        });
    }

    [Fact]
    public void MainWindow_reflows_action_buttons_when_resized()
    {
        WinFormsTestHost.Run(() =>
        {
            using var controller = new FakeAutomationWorkflowController();
            using var form = new MainWindow([SampleWorkflow()], controller);
            form.CreateControl();

            var start = form.Controls.Find("ActionButton", searchAllChildren: true)
                .OfType<Button>()
                .Single(button => button.Text == "店长特供2-8");
            int originalWidth = start.Width;

            form.ClientSize = new Size(form.ClientSize.Width + 180, form.ClientSize.Height + 80);

            Assert.True(start.Width > originalWidth);
        });
    }

    [Fact]
    public void MainWindow_disables_stop_until_a_workflow_is_running()
    {
        WinFormsTestHost.Run(() =>
        {
            using var controller = new FakeAutomationWorkflowController();
            using var form = new MainWindow([SampleWorkflow()], controller);
            form.CreateControl();

            var stop = form.Controls.Find("ActionButton", searchAllChildren: true)
                .OfType<Button>()
                .Single(button => button.Text.Contains("停止"));

            Assert.False(stop.Enabled);

            controller.SetState(AutomationWorkflowState.Running);

            Assert.True(stop.Enabled);
        });
    }

    [Fact]
    public void MainWindow_labels_stop_shortcut()
    {
        WinFormsTestHost.Run(() =>
        {
            using var controller = new FakeAutomationWorkflowController();
            using var form = new MainWindow([SampleWorkflow()], controller);
            form.CreateControl();

            var stop = form.Controls.Find("ActionButton", searchAllChildren: true)
                .OfType<Button>()
                .Single(button => button.Text.Contains("停止"));

            Assert.Contains("Alt+Q", stop.Text);
            Assert.True(form.KeyPreview);
        });
    }

    [Fact]
    public void MainWindow_alt_q_stops_running_workflow()
    {
        WinFormsTestHost.Run(() =>
        {
            using var controller = new FakeAutomationWorkflowController();
            using var form = new MainWindow([SampleWorkflow()], controller);
            form.CreateControl();
            controller.SetState(AutomationWorkflowState.Running);

            var handled = InvokeProcessCmdKey(form, Keys.Alt | Keys.Q);

            Assert.True(handled);
            Assert.Equal(1, controller.StopCount);
            Assert.Equal(AutomationWorkflowState.Stopped, controller.State);
        });
    }

    [Fact]
    public void MainWindow_global_alt_q_hotkey_stops_running_workflow_without_focus()
    {
        WinFormsTestHost.Run(() =>
        {
            using var controller = new FakeAutomationWorkflowController();
            using var form = new MainWindow([SampleWorkflow()], controller);
            form.CreateControl();
            controller.SetState(AutomationWorkflowState.Running);

            InvokeWndProc(form, Message.Create(IntPtr.Zero, 0x0312, 0x5151, IntPtr.Zero));

            Assert.Equal(1, controller.StopCount);
            Assert.Equal(AutomationWorkflowState.Stopped, controller.State);
        });
    }

    [Fact]
    public void MainWindow_low_level_alt_q_hook_stops_running_workflow_when_hotkey_is_unavailable()
    {
        WinFormsTestHost.Run(() =>
        {
            using var controller = new FakeAutomationWorkflowController();
            using var form = new MainWindow([SampleWorkflow()], controller);
            form.CreateControl();
            controller.SetState(AutomationWorkflowState.Running);

            bool handled = InvokeLowLevelKeyboardShortcut(form, message: 0x0104, key: Keys.Q, flags: 0x20);

            Assert.True(handled);
            Assert.Equal(1, controller.StopCount);
            Assert.Equal(AutomationWorkflowState.Stopped, controller.State);
        });
    }

    [Fact]
    public void MainWindow_lays_out_future_workflow_buttons_without_overlap()
    {
        WinFormsTestHost.Run(() =>
        {
            using var controller = new FakeAutomationWorkflowController();
            using var form = new MainWindow(
                [
                    SampleWorkflow(),
                    SampleWorkflow() with { Id = "second-flow", DisplayName = "备用流程" },
                ],
                controller);
            form.CreateControl();

            var buttons = form.Controls.Find("ActionButton", searchAllChildren: true)
                .OfType<Button>()
                .Where(button => !button.Text.Contains("停止"))
                .OrderBy(button => button.Top)
                .ToList();

            Assert.Equal(2, buttons.Count);
            Assert.True(buttons[1].Top > buttons[0].Bottom);
            Assert.All(buttons, button => Assert.True(button.Width > 250));
        });
    }

    private static AutomationWorkflowDefinition SampleWorkflow() =>
        new(
            Id: "store-special-2-8",
            DisplayName: "店长特供2-8",
            FileName: "powershell",
            Arguments: ["-ExecutionPolicy", "Bypass", "-File", @".\scripts\run-yihuan.ps1"],
            WorkingDirectory: "C:\\repo");

    private sealed class FakeAutomationWorkflowController : IAutomationWorkflowController
    {
        public event Action<AutomationWorkflowState>? StateChanged;
        public event Action<string>? ActivityChanged;

        public AutomationWorkflowState State { get; private set; } = AutomationWorkflowState.Idle;
        public AutomationWorkflowDefinition? ActiveWorkflow { get; private set; }
        public AutomationWorkflowDefinition? StartedWorkflow { get; private set; }
        public int StopCount { get; private set; }

        public bool Start(AutomationWorkflowDefinition workflow)
        {
            StartedWorkflow = workflow;
            ActiveWorkflow = workflow;
            SetState(AutomationWorkflowState.Running);
            ActivityChanged?.Invoke($"运行中: {workflow.DisplayName}");
            return true;
        }

        public Task StopAsync()
        {
            StopCount++;
            ActiveWorkflow = null;
            SetState(AutomationWorkflowState.Stopped);
            return Task.CompletedTask;
        }

        public void SetState(AutomationWorkflowState state)
        {
            State = state;
            StateChanged?.Invoke(state);
        }

        public void Dispose()
        {
        }
    }

    private static bool InvokeProcessCmdKey(Form form, Keys keyData)
    {
        MethodInfo processCmdKey = typeof(Form).GetMethod(
            "ProcessCmdKey",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ProcessCmdKey not found.");

        object?[] args = [new Message(), keyData];
        return (bool)processCmdKey.Invoke(form, args)!;
    }

    private static void InvokeWndProc(Form form, Message message)
    {
        MethodInfo wndProc = typeof(Form).GetMethod(
            "WndProc",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("WndProc not found.");

        object?[] args = [message];
        wndProc.Invoke(form, args);
    }

    private static bool InvokeLowLevelKeyboardShortcut(Form form, int message, Keys key, uint flags)
    {
        MethodInfo handler = typeof(MainWindow).GetMethod(
            "HandleLowLevelKeyboardMessage",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("HandleLowLevelKeyboardMessage not found.");

        object?[] args = [message, (int)key, flags];
        return (bool)handler.Invoke(form, args)!;
    }
}
