using FluentAssertions;
using MultiTool.Core.Models;

namespace MultiTool.Core.Tests;

public sealed class StorageTargetDisplayTests
{
    [Fact]
    public void DriveSmartTargetInfo_PickerLabel_ShouldPreferSimpleDriveFirstText()
    {
        var target = new DriveSmartTargetInfo(
            @"\\.\PHYSICALDRIVE0",
            "Samsung SSD 990 PRO  |  C:  |  Disk 0  |  1.8 TB  |  NVMe / SSD",
            "Samsung SSD 990 PRO",
            "1.8 TB",
            "NVMe",
            "SSD",
            "5B2QJXD7",
            "ABC123",
            @"C:\",
            "C:");

        target.PickerLabel.Should().Be("C: - Samsung SSD 990 PRO (1.8 TB)");
        target.ToString().Should().Be(target.PickerLabel);
    }

    [Fact]
    public void StorageBenchmarkTargetInfo_PickerLabel_ShouldPreferSimpleVolumeFirstText()
    {
        var target = new StorageBenchmarkTargetInfo(
            @"\\.\PHYSICALDRIVE0|C:",
            "Samsung SSD 990 PRO  |  C: (Windows)  |  1.8 TB  |  NVMe / SSD",
            "Samsung SSD 990 PRO",
            "1.8 TB",
            "NVMe",
            "SSD",
            "5B2QJXD7",
            @"C:\",
            "Windows",
            "NTFS",
            "621.4 GB");

        target.PickerLabel.Should().Be("C: (Windows) - Samsung SSD 990 PRO (1.8 TB)");
        target.ToString().Should().Be(target.PickerLabel);
    }
}
