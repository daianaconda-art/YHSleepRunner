using System.Text;
using System.Windows.Forms;
using YihuanRunner.Automation;
using YihuanRunner.Forms;
using YihuanRunner.Platform;

namespace YihuanRunner;

internal static class Program
{
    [STAThread]
    private static async Task<int> Main(string[] args)
    {
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Console.OutputEncoding = Encoding.UTF8;

        if (AppStartupModeResolver.Resolve(args) == AppStartupMode.Ui)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainWindow());
            return 0;
        }

        return await RunCliAsync(args);
    }

    private static async Task<int> RunCliAsync(string[] args)
    {
        RunnerOptions options;
        try
        {
            options = RunnerOptions.Parse(args);
        }
        catch (Exception ex) when (ex is ArgumentException or FormatException)
        {
            Console.WriteLine($"参数错误: {ex.Message}");
            Console.WriteLine(RunnerOptions.Usage);
            return 2;
        }

        if (options.ShowHelp)
        {
            Console.WriteLine(RunnerOptions.Usage);
            return 0;
        }

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cts.Cancel();
            Console.WriteLine("正在停止...");
        };

        try
        {
            var runner = new BusinessLoopRunner(
                options,
                new WindowLocator(),
                new ScreenCaptureService(),
                OcrReader.CreateDefault(),
                new MouseInput(),
                new KeyboardInput());
            return await runner.RunAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            return 130;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"运行失败: {ex.Message}");
            return 1;
        }
    }
}
