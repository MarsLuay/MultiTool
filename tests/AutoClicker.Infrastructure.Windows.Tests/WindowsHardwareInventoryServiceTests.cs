using AutoClicker.Core.Models;
using AutoClicker.Infrastructure.Windows.Tools;
using FluentAssertions;

namespace AutoClicker.Infrastructure.Windows.Tests;

public sealed class WindowsHardwareInventoryServiceTests
{
    [Fact]
    public async Task GetReportAsync_ShouldReturnInjectedReport()
    {
        var expectedReport = new HardwareInventoryReport(
            "WORKSTATION  |  Example Corp Model X",
            "Windows 11 Pro  |  Version 10.0.26100  |  Build 26100",
            "AMD Ryzen 7 7800X3D (8 cores / 16 threads)",
            "32 GB installed across 2 modules at up to 6000 MHz.",
            "ASUSTeK COMPUTER INC. ROG STRIX",
            "3208  |  Released 2026-02-01",
            [new HardwareDisplayAdapterInfo("NVIDIA GeForce RTX 4080", "32.0.0.1", "16 GB")],
            [new HardwareStorageDriveInfo("Samsung SSD 990 PRO", "2 TB", "NVMe", "SSD")],
            []);
        var service = new WindowsHardwareInventoryService(() => expectedReport);

        var result = await service.GetReportAsync();

        result.Should().BeEquivalentTo(expectedReport);
    }

    [Fact]
    public async Task GetReportAsync_ShouldSurfaceReaderFailures()
    {
        var service = new WindowsHardwareInventoryService(() => throw new InvalidOperationException("WMI failed"));

        var action = async () => await service.GetReportAsync();

        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("WMI failed");
    }
}
