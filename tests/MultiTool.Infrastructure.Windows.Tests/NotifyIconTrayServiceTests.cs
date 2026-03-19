using FluentAssertions;
using MultiTool.Core.Models;
using MultiTool.Core.Services;
using MultiTool.Infrastructure.Windows.Tray;

namespace MultiTool.Infrastructure.Windows.Tests;

public sealed class NotifyIconTrayServiceTests
{
    [Fact]
    public async Task RegisterMetricsRefreshInterest_ShouldCaptureMetricsAndStopAfterIdleWindow()
    {
        var metricsService = new FakeSystemTrayMetricsService();
        var service = new NotifyIconTrayService(
            Path.GetTempPath(),
            metricsService,
            TimeSpan.FromMilliseconds(20),
            TimeSpan.FromMilliseconds(70));

        service.RegisterMetricsRefreshInterest();
        await WaitForConditionAsync(() => metricsService.CaptureCallCount >= 1);

        service.IsMetricsRefreshLoopActive.Should().BeTrue();
        service.CurrentTooltipText.Should().Contain("CPU 17%");

        await WaitForConditionAsync(() => !service.IsMetricsRefreshLoopActive);
        metricsService.CaptureCallCount.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task SetRunningState_ShouldNotStartMetricsRefreshLoopOnItsOwn()
    {
        var metricsService = new FakeSystemTrayMetricsService();
        var service = new NotifyIconTrayService(
            Path.GetTempPath(),
            metricsService,
            TimeSpan.FromMilliseconds(20),
            TimeSpan.FromMilliseconds(70));

        service.SetRunningState(true);
        await Task.Delay(100);

        service.IsMetricsRefreshLoopActive.Should().BeFalse();
        metricsService.CaptureCallCount.Should().Be(0);
    }

    private static async Task WaitForConditionAsync(Func<bool> condition, TimeSpan? timeout = null)
    {
        var effectiveTimeout = timeout ?? TimeSpan.FromSeconds(2);
        var startedAt = DateTime.UtcNow;

        while (!condition() && DateTime.UtcNow - startedAt < effectiveTimeout)
        {
            await Task.Delay(25);
        }

        condition().Should().BeTrue();
    }

    private sealed class FakeSystemTrayMetricsService : ISystemTrayMetricsService
    {
        public int CaptureCallCount { get; private set; }

        public Task<SystemTrayMetricsSnapshot> CaptureAsync(CancellationToken cancellationToken = default)
        {
            CaptureCallCount++;
            return Task.FromResult(
                new SystemTrayMetricsSnapshot(
                    CpuUsagePercent: 17,
                    TemperatureCelsius: 56.8,
                    MemoryUsagePercent: 63,
                    DiskUsagePercent: 2,
                    CapturedAt: DateTimeOffset.UtcNow));
        }
    }
}
