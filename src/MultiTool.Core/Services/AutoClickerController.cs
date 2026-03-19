using System.Diagnostics;
using MultiTool.Core.Enums;
using MultiTool.Core.Models;
using MultiTool.Core.Results;

namespace MultiTool.Core.Services;

public sealed class AutoClickerController : IAutoClickerController
{
    private readonly IMouseInputService mouseInputService;
    private readonly ICursorService cursorService;
    private readonly Random random;
    private readonly SemaphoreSlim stateLock = new(1, 1);

    private CancellationTokenSource? runCancellationTokenSource;
    private Task? runTask;
    private long suspendUntilUtcTicks;
    private long suspendVersion;

    public AutoClickerController(IMouseInputService mouseInputService, ICursorService cursorService, Random? random = null)
    {
        this.mouseInputService = mouseInputService;
        this.cursorService = cursorService;
        this.random = random ?? new Random();
    }

    public bool IsRunning { get; private set; }

    public event EventHandler<RunningStateChangedEventArgs>? RunningStateChanged;

    public async Task StartAsync(ClickSettings settings, CancellationToken cancellationToken = default)
    {
        await stateLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (IsRunning)
            {
                return;
            }

            runCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            runTask = RunLoopAsync(settings.Clone(), runCancellationTokenSource.Token);
            SetRunningState(true);
        }
        finally
        {
            stateLock.Release();
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        Task? activeTask = null;

        await stateLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (!IsRunning)
            {
                return;
            }

            runCancellationTokenSource?.Cancel();
            activeTask = runTask;
        }
        finally
        {
            stateLock.Release();
        }

        if (activeTask is null)
        {
            return;
        }

        try
        {
            await activeTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
    }

    public Task ToggleAsync(ClickSettings settings, CancellationToken cancellationToken = default) =>
        IsRunning ? StopAsync(cancellationToken) : StartAsync(settings, cancellationToken);

    public void SuspendFor(TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero || !IsRunning)
        {
            return;
        }

        var suspendUntilUtc = DateTime.UtcNow + duration;

        while (true)
        {
            var currentTicks = Interlocked.Read(ref suspendUntilUtcTicks);
            if (currentTicks >= suspendUntilUtc.Ticks)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref suspendUntilUtcTicks, suspendUntilUtc.Ticks, currentTicks) == currentTicks)
            {
                Interlocked.Increment(ref suspendVersion);
                return;
            }
        }
    }

    private async Task RunLoopAsync(ClickSettings settings, CancellationToken cancellationToken)
    {
        if (settings.ClickType == ClickKind.Hold)
        {
            await RunHoldAsync(settings, cancellationToken).ConfigureAwait(false);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        var executedClicks = 0;
        var interval = settings.GetNextInterval(random);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var suspensionVersion = Interlocked.Read(ref suspendVersion);
                var nextDue = interval - stopwatch.Elapsed;
                var suspendDelay = GetSuspendDelay();
                var waitTime = nextDue > suspendDelay ? nextDue : suspendDelay;
                if (waitTime > TimeSpan.Zero)
                {
                    await Task.Delay(waitTime, cancellationToken).ConfigureAwait(false);
                }

                if (suspensionVersion != Interlocked.Read(ref suspendVersion) || GetSuspendDelay() > TimeSpan.Zero)
                {
                    continue;
                }

                ExecuteClick(settings);
                executedClicks++;
                stopwatch.Restart();

                if (settings.RepeatMode == RepeatMode.Count && executedClicks >= settings.RepeatCount)
                {
                    break;
                }

                interval = settings.GetNextInterval(random);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        finally
        {
            await stateLock.WaitAsync(CancellationToken.None).ConfigureAwait(false);

            try
            {
                runCancellationTokenSource?.Dispose();
                runCancellationTokenSource = null;
                runTask = null;
                Interlocked.Exchange(ref suspendUntilUtcTicks, 0);
                Interlocked.Exchange(ref suspendVersion, 0);
                SetRunningState(false);
            }
            finally
            {
                stateLock.Release();
            }
        }
    }

    private async Task RunHoldAsync(ClickSettings settings, CancellationToken cancellationToken)
    {
        try
        {
            if (UsesPointerPosition(settings))
            {
                cursorService.SetCursorPosition(settings.GetFixedPoint());
            }

            ExecutePress(settings);
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        finally
        {
            ExecuteRelease(settings);

            await stateLock.WaitAsync(CancellationToken.None).ConfigureAwait(false);

            try
            {
                runCancellationTokenSource?.Dispose();
                runCancellationTokenSource = null;
                runTask = null;
                Interlocked.Exchange(ref suspendUntilUtcTicks, 0);
                Interlocked.Exchange(ref suspendVersion, 0);
                SetRunningState(false);
            }
            finally
            {
                stateLock.Release();
            }
        }
    }

    private void ExecuteClick(ClickSettings settings)
    {
        if (UsesPointerPosition(settings))
        {
            cursorService.SetCursorPosition(settings.GetFixedPoint());
        }

        var times = settings.ClickType == ClickKind.Double ? 2 : 1;

        if (UsesCustomKeyboard(settings))
        {
            mouseInputService.ClickKey(settings.CustomKeyVirtualKey, times);
            return;
        }

        if (UsesCustomMouseButton(settings))
        {
            mouseInputService.Click(settings.CustomMouseButton, times);
            return;
        }

        mouseInputService.Click(settings.MouseButton, times);
    }

    private void ExecutePress(ClickSettings settings)
    {
        if (UsesCustomKeyboard(settings))
        {
            mouseInputService.PressKey(settings.CustomKeyVirtualKey);
            return;
        }

        if (UsesCustomMouseButton(settings))
        {
            mouseInputService.Press(settings.CustomMouseButton);
            return;
        }

        mouseInputService.Press(settings.MouseButton);
    }

    private void ExecuteRelease(ClickSettings settings)
    {
        if (UsesCustomKeyboard(settings))
        {
            mouseInputService.ReleaseKey(settings.CustomKeyVirtualKey);
            return;
        }

        if (UsesCustomMouseButton(settings))
        {
            mouseInputService.Release(settings.CustomMouseButton);
            return;
        }

        mouseInputService.Release(settings.MouseButton);
    }

    private void SetRunningState(bool isRunning)
    {
        if (IsRunning == isRunning)
        {
            return;
        }

        IsRunning = isRunning;
        RunningStateChanged?.Invoke(this, new RunningStateChangedEventArgs(isRunning));
    }

    private static bool UsesCustomKeyboard(ClickSettings settings) =>
        settings.MouseButton == ClickMouseButton.Custom && settings.CustomInputKind == CustomInputKind.Keyboard;

    private static bool UsesCustomMouseButton(ClickSettings settings) =>
        settings.MouseButton == ClickMouseButton.Custom && settings.CustomInputKind == CustomInputKind.MouseButton;

    private static bool UsesPointerPosition(ClickSettings settings) =>
        (!settings.MouseButton.Equals(ClickMouseButton.Custom) || UsesCustomMouseButton(settings))
        && settings.LocationMode == ClickLocationMode.FixedPoint;

    private TimeSpan GetSuspendDelay()
    {
        var suspendUntilTicks = Interlocked.Read(ref suspendUntilUtcTicks);
        if (suspendUntilTicks <= 0)
        {
            return TimeSpan.Zero;
        }

        var remaining = new DateTime(suspendUntilTicks, DateTimeKind.Utc) - DateTime.UtcNow;
        if (remaining <= TimeSpan.Zero)
        {
            Interlocked.CompareExchange(ref suspendUntilUtcTicks, 0, suspendUntilTicks);
            return TimeSpan.Zero;
        }

        return remaining;
    }
}
