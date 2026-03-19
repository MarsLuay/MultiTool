using MultiTool.Core.Models;
using MultiTool.Infrastructure.Windows.Tools;
using FluentAssertions;

namespace MultiTool.Infrastructure.Windows.Tests;

public sealed class WindowsHardwareInventoryServiceTests
{
    [Fact]
    public async Task GetReportAsync_ShouldReturnInjectedReport()
    {
        var expectedReport = new HardwareInventoryReport(
            "WORKSTATION  |  Example Corp Model X",
            "No critical issues detected.",
            "Windows 11 Pro  |  Version 10.0.26100  |  Build 26100",
            "AMD Ryzen 7 7800X3D (8 cores / 16 threads)",
            "32 GB installed across 2 modules at up to 6000 MHz.",
            "ASUSTeK COMPUTER INC. ROG STRIX",
            "3208  |  Released 2026-02-01",
            [new HardwareDisplayAdapterInfo("NVIDIA GeForce RTX 4080", "32.0.0.1", "16 GB")],
            [new HardwareStorageDriveInfo("Samsung SSD 990 PRO", "2 TB", "NVMe", "SSD", "Healthy", "Healthy", "5B2QJXD7", "S6Z9", "OK")],
            [new HardwarePartitionInfo("Samsung SSD 990 PRO", "Disk #0, Partition #1", "512 GB", "GPT: Basic Data", "C: (Windows)", "NTFS", "210 GB", "Boot  |  Primary  |  OK")],
            [new HardwareSensorInfo("Temperature", "CPU Package", "64.2 C", "ACPI thermal zone", "Reported by Windows ACPI telemetry")],
            [new HardwarePciDeviceInfo("NVIDIA GeForce RTX 4080", "Display", "NVIDIA", "PCI bus 1, device 0, function 0", "OK")],
            [new HardwareRaidInfo("Intel RST Premium Controller", "PCIe RAID Controller", "OK", "Intel  |  PCI bus 0, device 17, function 0", "Win32_PnPEntity")],
            [new DriverHardwareInfo("NVIDIA GeForce RTX 4080", "NVIDIA", "NVIDIA", "32.0.0.1", "Display", "PCI\\VEN_10DE")],
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
