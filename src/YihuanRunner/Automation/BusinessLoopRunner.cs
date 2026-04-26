using System.Drawing.Imaging;
using YihuanRunner.Platform;

namespace YihuanRunner.Automation;

public sealed class BusinessLoopRunner(
    RunnerOptions options,
    WindowLocator windowLocator,
    ScreenCaptureService captureService,
    OcrReader ocrReader,
    MouseInput mouseInput,
    KeyboardInput keyboardInput)
{
    private static readonly TimeSpan StartButtonDisappearTimeout = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan StartButtonDisappearPollInterval = TimeSpan.FromMilliseconds(700);
    private static readonly TimeSpan ClaimRewardClickDelay = TimeSpan.FromMilliseconds(1800);
    private static readonly TimeSpan PostClaimEntryDelay = TimeSpan.FromMilliseconds(1500);
    private static readonly TimeSpan EntryRecoveryDelay = TimeSpan.FromMilliseconds(1800);

    private readonly Random _rng = new();

    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine($"目标: process={options.ProcessName}, title~={options.TitleContains}");
        Console.WriteLine($"锤子: x={options.HammerPoint.X:F3}, y={options.HammerPoint.Y:F3}, duration={options.BusinessDuration.TotalSeconds:F0}s");
        Console.WriteLine(options.LoopCount > 0 ? $"循环次数: 领取 {options.LoopCount} 次后停止" : "循环次数: 一直循环");
        Console.WriteLine("停止: 在这个控制台按 Ctrl+C");
        var entryRecoveryTracker = new EntryRecoveryTracker(requiredUnresolvedProbes: 2);
        var claimCounter = new ClaimCompletionCounter(options.LoopCount);

        while (!cancellationToken.IsCancellationRequested)
        {
            WindowInfo? window = windowLocator.Find(options.ProcessName, options.TitleContains);
            if (window is null)
            {
                Console.WriteLine("没有找到异环窗口，1 秒后重试。");
                await Task.Delay(1000, cancellationToken);
                if (options.RunOnce || options.ProbeOnly)
                    return 2;
                continue;
            }

            ClientPoint? startPoint = await ProbeStartButton(window, cancellationToken);
            _ = entryRecoveryTracker.Observe(resolvedScreen: startPoint is not null);
            if (options.ProbeOnly)
                return startPoint is null ? 2 : 0;

            if (startPoint is null)
            {
                ClientPoint? claimPoint = await ProbeClaimReward(window, cancellationToken);
                if (claimPoint is not null)
                {
                    _ = entryRecoveryTracker.Observe(resolvedScreen: true);
                    if (options.DryRun)
                    {
                        Console.WriteLine($"dry-run: 将点击领取 {claimPoint.Value.X},{claimPoint.Value.Y}");
                        return 0;
                    }

                    if (InputPrivilegeGuard.TryCreateBlockedInputMessageForTargetProcess(window.ProcessId, out string claimBlockedInputMessage))
                    {
                        Console.WriteLine(claimBlockedInputMessage);
                        return 3;
                    }

                    Console.WriteLine($"点击领取: {claimPoint.Value.X},{claimPoint.Value.Y}");
                    await mouseInput.ClickClientAsync(window.Handle, claimPoint.Value, _rng, jitterPixels: 3, cancellationToken);
                    await PressFAfterClaimAsync(window, cancellationToken);
                    if (RecordClaimAndShouldStop(claimCounter))
                        return 0;

                    continue;
                }

                if (entryRecoveryTracker.Observe(resolvedScreen: false))
                {
                    if (options.DryRun)
                    {
                        Console.WriteLine("dry-run: 将按 F 返回营业入口");
                        return 0;
                    }

                    if (InputPrivilegeGuard.TryCreateBlockedInputMessageForTargetProcess(window.ProcessId, out string recoveryBlockedInputMessage))
                    {
                        Console.WriteLine(recoveryBlockedInputMessage);
                        return 3;
                    }

                    Console.WriteLine("未识别到开始/领取，按 F 尝试返回营业入口。");
                    await keyboardInput.PressFAsync(window.Handle, _rng, cancellationToken);
                    await Task.Delay(EntryRecoveryDelay, cancellationToken);
                    continue;
                }

                if (options.RunOnce)
                    return 2;

                await Task.Delay(1200, cancellationToken);
                continue;
            }

            if (options.DryRun)
            {
                Console.WriteLine($"dry-run: 将点击开始营业 {startPoint.Value.X},{startPoint.Value.Y}");
                return 0;
            }

            if (InputPrivilegeGuard.TryCreateBlockedInputMessageForTargetProcess(window.ProcessId, out string blockedInputMessage))
            {
                Console.WriteLine(blockedInputMessage);
                return 3;
            }

            Console.WriteLine($"点击开始营业: {startPoint.Value.X},{startPoint.Value.Y}");
            await mouseInput.ClickClientAsync(window.Handle, startPoint.Value, _rng, jitterPixels: 3, cancellationToken);

            if (!await WaitForStartButtonToDisappear(window, cancellationToken))
            {
                Console.WriteLine("点击后“开始营业”仍然可见，暂不进入锤子阶段，重新尝试。");
                if (options.RunOnce)
                    return 4;

                await Task.Delay(1000, cancellationToken);
                continue;
            }

            Console.WriteLine($"开始营业已确认，等待 loading {options.LoadingDelay.TotalMilliseconds:F0}ms。");
            await Task.Delay(options.LoadingDelay, cancellationToken);

            await RunHammerPhase(window, cancellationToken);

            bool claimed = await TryClaimRewardUntilStartReturns(window, cancellationToken);
            if (claimed && RecordClaimAndShouldStop(claimCounter))
                return 0;

            if (options.RunOnce)
                return 0;

            Console.WriteLine("本轮结束，等待结算后重新寻找开始营业。");
            await Task.Delay(1000, cancellationToken);
        }

        return 130;
    }

    private async Task<ClientPoint?> ProbeStartButton(WindowInfo window, CancellationToken cancellationToken, bool saveSnapshot = true)
    {
        WindowNative.BringToFront(window.Handle);
        await Task.Delay(300, cancellationToken);

        using CapturedFrame frame = captureService.CaptureClient(window.Handle);
        if (saveSnapshot && !string.IsNullOrWhiteSpace(options.SnapshotPath))
            SaveSnapshot(frame);

        IReadOnlyList<OcrLineHit> hits = await ocrReader.RecognizeRegionLinesAsync(
            frame.Bitmap,
            options.StartButtonRegion,
            cancellationToken);

        string text = string.Join(" | ", hits.Select(hit => OcrTextMatcher.Normalize(hit.Text)));
        Console.WriteLine($"右下 OCR: {(text.Length == 0 ? "(empty)" : text)}");

        ClientPoint? point = StartButtonLocator.FindStartButtonCenter(
            hits,
            options.StartButtonRegion,
            frame.ClientWidth,
            frame.ClientHeight);

        if (point is null)
            Console.WriteLine("没有识别到“开始营业”。");
        else
            Console.WriteLine($"识别到“开始营业”: {point.Value.X},{point.Value.Y}");

        return point;
    }

    private async Task<ClientPoint?> ProbeClaimReward(WindowInfo window, CancellationToken cancellationToken)
    {
        WindowNative.BringToFront(window.Handle);
        await Task.Delay(300, cancellationToken);

        using CapturedFrame frame = captureService.CaptureClient(window.Handle);
        IReadOnlyList<OcrLineHit> hits = await ocrReader.RecognizeRegionLinesAsync(
            frame.Bitmap,
            options.ClaimRewardRegion,
            cancellationToken);

        string text = string.Join(" | ", hits.Select(hit => OcrTextMatcher.Normalize(hit.Text)));
        Console.WriteLine($"结算 OCR: {(text.Length == 0 ? "(empty)" : text)}");

        ClientPoint? point = ClaimRewardLocator.FindClaimRewardCenter(
            hits,
            options.ClaimRewardRegion,
            frame.ClientWidth,
            frame.ClientHeight);

        if (point is null)
            Console.WriteLine("没有识别到“领取”。");
        else
            Console.WriteLine($"识别到“领取”: {point.Value.X},{point.Value.Y}");

        return point;
    }

    private async Task<bool> TryClaimRewardUntilStartReturns(WindowInfo window, CancellationToken cancellationToken)
    {
        DateTimeOffset deadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(20);
        while (DateTimeOffset.UtcNow < deadline && !cancellationToken.IsCancellationRequested)
        {
            ClientPoint? claimPoint = await ProbeClaimReward(window, cancellationToken);
            if (claimPoint is not null)
            {
                Console.WriteLine($"点击领取: {claimPoint.Value.X},{claimPoint.Value.Y}");
                await mouseInput.ClickClientAsync(window.Handle, claimPoint.Value, _rng, jitterPixels: 3, cancellationToken);
                await PressFAfterClaimAsync(window, cancellationToken);
                return true;
            }

            ClientPoint? startPoint = await ProbeStartButton(window, cancellationToken, saveSnapshot: false);
            if (startPoint is not null)
                return false;

            await Task.Delay(1000, cancellationToken);
        }

        return false;
    }

    private static bool RecordClaimAndShouldStop(ClaimCompletionCounter claimCounter)
    {
        bool shouldStop = claimCounter.RecordClaimAndShouldStop();
        if (claimCounter.TargetClaims > 0)
            Console.WriteLine($"领取计数: {claimCounter.CompletedClaims}/{claimCounter.TargetClaims}");
        else
            Console.WriteLine($"领取计数: {claimCounter.CompletedClaims}");

        if (shouldStop)
            Console.WriteLine("已达到指定领取次数，自动停止。");

        return shouldStop;
    }

    private async Task PressFAfterClaimAsync(WindowInfo window, CancellationToken cancellationToken)
    {
        await Task.Delay(ClaimRewardClickDelay, cancellationToken);
        Console.WriteLine("领取后按 F 返回营业入口。");
        await keyboardInput.PressFAsync(window.Handle, _rng, cancellationToken);
        await Task.Delay(PostClaimEntryDelay, cancellationToken);
    }

    private async Task<bool> WaitForStartButtonToDisappear(WindowInfo window, CancellationToken cancellationToken)
    {
        var tracker = new StartButtonTransitionTracker(requiredMissingProbes: 2);
        DateTimeOffset deadline = DateTimeOffset.UtcNow + StartButtonDisappearTimeout;

        while (DateTimeOffset.UtcNow < deadline && !cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(StartButtonDisappearPollInterval, cancellationToken);
            ClientPoint? startPoint = await ProbeStartButton(window, cancellationToken, saveSnapshot: false);
            bool ready = tracker.Observe(startButtonVisible: startPoint is not null);
            if (ready)
                return true;
        }

        return false;
    }

    private void SaveSnapshot(CapturedFrame frame)
    {
        string path = options.SnapshotPath!;
        string? dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(dir))
            Directory.CreateDirectory(dir);

        frame.Bitmap.Save(path, ImageFormat.Png);
        Console.WriteLine($"截图已保存: {path}");
    }

    private async Task RunHammerPhase(WindowInfo window, CancellationToken cancellationToken)
    {
        double scale = DpiScaler.GetPrimaryScreenScale();
        (int physicalWidth, int physicalHeight) = DpiCoordinateMapper.ScaleLogicalClientSize(
            window.ClientWidth,
            window.ClientHeight,
            scale);
        ClientPoint hammer = options.HammerPoint.ToClientPoint(physicalWidth, physicalHeight);
        DateTimeOffset endAt = DateTimeOffset.UtcNow + options.BusinessDuration;
        Console.WriteLine($"进入营业点击阶段: hammer={hammer.X},{hammer.Y}");

        int clickCount = 0;
        while (DateTimeOffset.UtcNow < endAt && !cancellationToken.IsCancellationRequested)
        {
            clickCount++;
            await mouseInput.ClickClientAsync(window.Handle, hammer, _rng, jitterPixels: 5, cancellationToken);
            TimeSpan delay = RandomDelayPlanner.NextDelay(_rng, options.MinHammerDelayMs, options.MaxHammerDelayMs);
            double remainingSeconds = Math.Max(0, (endAt - DateTimeOffset.UtcNow).TotalSeconds);
            Console.WriteLine(
                $"锤子点击 #{clickCount}: point={hammer.X},{hammer.Y}, next={delay.TotalMilliseconds:F0}ms, remaining={remainingSeconds:F1}s");
            await Task.Delay(delay, cancellationToken);
        }
    }
}
