using MultiTool.Core.Models;
using MultiTool.Infrastructure.Windows.Tools;
using MultiTool.Infrastructure.Windows.Tray;
using FluentAssertions;

namespace MultiTool.Infrastructure.Windows.Tests;

public sealed class WindowsSystemTrayMetricsServiceTests
{
    [Fact]
    public async Task CaptureAsync_ShouldReturnClampedMetricsSnapshot()
    {
        var service = new WindowsSystemTrayMetricsService(
            getCpuUsagePercent: () => 135,
            getTemperatureCelsius: () => 57.4,
            getMemoryUsagePercent: () => -8,
            getDiskUsagePercent: () => 103);

        var snapshot = await service.CaptureAsync();

        snapshot.CpuUsagePercent.Should().Be(100);
        snapshot.TemperatureCelsius.Should().Be(57.4);
        snapshot.MemoryUsagePercent.Should().Be(0);
        snapshot.DiskUsagePercent.Should().Be(100);
        snapshot.CapturedAt.Should().NotBe(default);
    }

    [Fact]
    public void BuildTooltipText_ShouldFormatCompactSystemSummary()
    {
        var snapshot = new SystemTrayMetricsSnapshot(
            CpuUsagePercent: 17,
            TemperatureCelsius: 56.8,
            MemoryUsagePercent: 63,
            DiskUsagePercent: 2,
            CapturedAt: DateTimeOffset.Now);

        var tooltipText = NotifyIconTrayService.BuildTooltipText(snapshot);

        tooltipText.Should().Be("MultiTool | CPU 17% | Temp 56.8C | RAM 63% | Disk 2%");
    }

    [Fact]
    public void BuildTooltipText_ShouldUsePlaceholdersWhenMetricsUnavailable()
    {
        var snapshot = new SystemTrayMetricsSnapshot(
            CpuUsagePercent: null,
            TemperatureCelsius: null,
            MemoryUsagePercent: 41,
            DiskUsagePercent: null,
            CapturedAt: DateTimeOffset.Now);

        var tooltipText = NotifyIconTrayService.BuildTooltipText(snapshot);

        tooltipText.Should().Be("MultiTool | CPU -- | Temp -- | RAM 41% | Disk --");
    }
}
