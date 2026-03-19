using System.Drawing;
using System.IO;
using MultiTool.Core.Models;
using MultiTool.Core.Services;

namespace MultiTool.Infrastructure.Windows.Tray;

public sealed class NotifyIconTrayService : ITrayIconService
{
    private const int MaximumNotifyIconTextLength = 63;
    private static readonly TimeSpan MetricsRefreshInterval = TimeSpan.FromSeconds(5);

    private readonly string iconDirectoryPath;
    private readonly ISystemTrayMetricsService systemTrayMetricsService;

    private System.Windows.Forms.NotifyIcon? notifyIcon;
    private System.Windows.Forms.ContextMenuStrip? menu;
    private Icon? idleIcon;
    private Icon? runningIcon;
    private CancellationTokenSource? metricsRefreshCancellationTokenSource;
    private Task? metricsRefreshTask;
    private SynchronizationContext? synchronizationContext;
    private string currentTooltipText = BuildTooltipText(
        new SystemTrayMetricsSnapshot(
            CpuUsagePercent: null,
            TemperatureCelsius: null,
            MemoryUsagePercent: null,
            DiskUsagePercent: null,
            CapturedAt: DateTimeOffset.MinValue));
    private bool initialized;
    private bool disposed;
    private bool isRunning;

    public NotifyIconTrayService(ISystemTrayMetricsService systemTrayMetricsService)
        : this(Path.Combine(AppContext.BaseDirectory, "Resources", "Icons"), systemTrayMetricsService)
    {
    }

    internal NotifyIconTrayService(string iconDirectoryPath, ISystemTrayMetricsService systemTrayMetricsService)
    {
        this.iconDirectoryPath = iconDirectoryPath;
        this.systemTrayMetricsService = systemTrayMetricsService;
    }

    public event EventHandler? ShowRequested;

    public event EventHandler? HideRequested;

    public event EventHandler? ExitRequested;

    public void Initialize()
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        if (initialized)
        {
            return;
        }

        idleIcon = LoadIcon("icon.ico");
        runningIcon = LoadIcon("icon_running.ico");
        synchronizationContext = SynchronizationContext.Current;
        menu = new System.Windows.Forms.ContextMenuStrip();

        var showItem = new System.Windows.Forms.ToolStripMenuItem("Show MultiTool");
        var hideItem = new System.Windows.Forms.ToolStripMenuItem("Hide MultiTool");
        var exitItem = new System.Windows.Forms.ToolStripMenuItem("Exit");

        showItem.Click += (_, _) => ShowRequested?.Invoke(this, EventArgs.Empty);
        hideItem.Click += (_, _) => HideRequested?.Invoke(this, EventArgs.Empty);
        exitItem.Click += (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty);

        menu.Items.Add(showItem);
        menu.Items.Add(hideItem);
        menu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        menu.Items.Add(exitItem);

        notifyIcon = new System.Windows.Forms.NotifyIcon
        {
            Icon = idleIcon,
            Text = currentTooltipText,
            Visible = true,
            ContextMenuStrip = menu,
        };

        notifyIcon.MouseClick += (_, eventArgs) =>
        {
            if (eventArgs.Button == System.Windows.Forms.MouseButtons.Left)
            {
                ShowRequested?.Invoke(this, EventArgs.Empty);
            }
        };

        metricsRefreshCancellationTokenSource = new CancellationTokenSource();
        metricsRefreshTask = RunMetricsRefreshLoopAsync(metricsRefreshCancellationTokenSource.Token);
        initialized = true;
    }

    public void SetRunningState(bool isRunning)
    {
        this.isRunning = isRunning;

        if (notifyIcon is null)
        {
            return;
        }

        ApplyNotifyIconState();
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        metricsRefreshCancellationTokenSource?.Cancel();
        metricsRefreshCancellationTokenSource?.Dispose();
        metricsRefreshCancellationTokenSource = null;

        if (notifyIcon is not null)
        {
            notifyIcon.Visible = false;
            notifyIcon.Dispose();
            notifyIcon = null;
        }

        menu?.Dispose();
        idleIcon?.Dispose();
        runningIcon?.Dispose();
    }

    internal static string BuildTooltipText(SystemTrayMetricsSnapshot snapshot)
    {
        var tooltipText = $"MultiTool | CPU {FormatPercent(snapshot.CpuUsagePercent)} | Temp {FormatTemperature(snapshot.TemperatureCelsius)} | RAM {FormatPercent(snapshot.MemoryUsagePercent)} | Disk {FormatPercent(snapshot.DiskUsagePercent)}";

        return tooltipText.Length <= MaximumNotifyIconTextLength
            ? tooltipText
            : tooltipText[..MaximumNotifyIconTextLength];
    }

    private Icon LoadIcon(string fileName)
    {
        var path = Path.Combine(iconDirectoryPath, fileName);
        if (!File.Exists(path))
        {
            return (Icon)SystemIcons.Application.Clone();
        }

        try
        {
            return new Icon(path);
        }
        catch (Exception)
        {
            return (Icon)SystemIcons.Application.Clone();
        }
    }

    private async Task RunMetricsRefreshLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            await RefreshMetricsAsync(cancellationToken).ConfigureAwait(false);

            using var timer = new PeriodicTimer(MetricsRefreshInterval);
            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
            {
                await RefreshMetricsAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task RefreshMetricsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var snapshot = await systemTrayMetricsService.CaptureAsync(cancellationToken).ConfigureAwait(false);
            var tooltipText = BuildTooltipText(snapshot);
            UpdateTooltipText(tooltipText);
        }
        catch (OperationCanceledException)
        {
        }
        catch
        {
        }
    }

    private void UpdateTooltipText(string tooltipText)
    {
        currentTooltipText = tooltipText;

        if (synchronizationContext is not null && SynchronizationContext.Current != synchronizationContext)
        {
            synchronizationContext.Post(
                _ =>
                {
                    if (!disposed)
                    {
                        ApplyNotifyIconState();
                    }
                },
                null);
            return;
        }

        ApplyNotifyIconState();
    }

    private void ApplyNotifyIconState()
    {
        if (disposed || notifyIcon is null)
        {
            return;
        }

        notifyIcon.Icon = isRunning ? runningIcon ?? idleIcon : idleIcon;
        notifyIcon.Text = currentTooltipText;
    }

    private static string FormatPercent(int? value) =>
        value.HasValue ? $"{value.Value}%" : "--";

    private static string FormatTemperature(double? value)
    {
        if (!value.HasValue)
        {
            return "--";
        }

        var roundedValue = Math.Abs(value.Value - Math.Round(value.Value)) < 0.05d
            ? Math.Round(value.Value).ToString("0", System.Globalization.CultureInfo.InvariantCulture)
            : value.Value.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);
        return $"{roundedValue}C";
    }
}
