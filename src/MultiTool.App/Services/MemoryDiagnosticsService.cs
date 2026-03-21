using System.Diagnostics;
using Microsoft.Extensions.Hosting;

namespace MultiTool.App.Services;

public sealed class MemoryDiagnosticsService : BackgroundService
{
    private static readonly TimeSpan DefaultSnapshotInterval = TimeSpan.FromSeconds(30);

    private readonly AppLaunchOptions launchOptions;
    private readonly TimeSpan snapshotInterval;

    public MemoryDiagnosticsService(AppLaunchOptions launchOptions)
        : this(launchOptions, DefaultSnapshotInterval)
    {
    }

    internal MemoryDiagnosticsService(AppLaunchOptions launchOptions, TimeSpan snapshotInterval)
    {
        this.launchOptions = launchOptions;
        this.snapshotInterval = snapshotInterval > TimeSpan.Zero
            ? snapshotInterval
            : DefaultSnapshotInterval;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        if (!launchOptions.IsMemoryLoggingEnabled)
        {
            return Task.CompletedTask;
        }

        AppLog.Info(CreateSnapshotMessage("Memory diagnostics enabled"));
        return base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!launchOptions.IsMemoryLoggingEnabled)
        {
            return;
        }

        using var timer = new PeriodicTimer(snapshotInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        {
            AppLog.Info(CreateSnapshotMessage("Memory snapshot"));
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (launchOptions.IsMemoryLoggingEnabled)
        {
            AppLog.Info(CreateSnapshotMessage("Memory diagnostics stopping"));
        }

        await base.StopAsync(cancellationToken).ConfigureAwait(false);
    }

    internal static string CreateSnapshotMessage(string context)
    {
        using var process = Process.GetCurrentProcess();
        process.Refresh();

        var gcInfo = GC.GetGCMemoryInfo();
        return string.Join(
            " | ",
            context,
            $"WorkingSet={FormatBytes(process.WorkingSet64)}",
            $"Private={FormatBytes(process.PrivateMemorySize64)}",
            $"Paged={FormatBytes(process.PagedMemorySize64)}",
            $"GCHeap={FormatBytes(GC.GetTotalMemory(forceFullCollection: false))}",
            $"GCCommitted={FormatBytes(gcInfo.TotalCommittedBytes)}",
            $"HeapSize={FormatBytes(gcInfo.HeapSizeBytes)}",
            $"Fragmented={FormatBytes(gcInfo.FragmentedBytes)}",
            $"Handles={process.HandleCount}",
            $"Threads={process.Threads.Count}");
    }

    internal static string FormatBytes(long bytes)
    {
        var normalizedBytes = bytes < 0 ? 0d : bytes;
        string[] suffixes = ["B", "KB", "MB", "GB", "TB"];
        var suffixIndex = 0;

        while (normalizedBytes >= 1024d && suffixIndex < suffixes.Length - 1)
        {
            normalizedBytes /= 1024d;
            suffixIndex++;
        }

        var format = suffixIndex == 0 ? "0" : "0.0";
        return $"{normalizedBytes.ToString(format, System.Globalization.CultureInfo.InvariantCulture)} {suffixes[suffixIndex]}";
    }
}
