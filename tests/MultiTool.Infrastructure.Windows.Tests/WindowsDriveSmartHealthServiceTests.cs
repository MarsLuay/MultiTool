using FluentAssertions;
using MultiTool.Infrastructure.Windows.Tools;

namespace MultiTool.Infrastructure.Windows.Tests;

public sealed class WindowsDriveSmartHealthServiceTests
{
    [Fact]
    public async Task GetAvailableDrivesAsync_ShouldReturnFormattedTargets()
    {
        var service = new WindowsDriveSmartHealthService(
            _ =>
            [
                new DriveSmartDiskSnapshot(
                    0,
                    @"\\.\PHYSICALDRIVE0",
                    "Samsung SSD 990 PRO",
                    2_000_000_000_000,
                    "NVMe",
                    "SSD",
                    "OK",
                    "ABC123",
                    "5B2QJXD7",
                    @"PCI\VEN_144D&DEV_A80A"),
            ],
            _ => throw new InvalidOperationException("Scan should not run"));

        var result = await service.GetAvailableDrivesAsync();

        result.Should().ContainSingle();
        result[0].DeviceId.Should().Be(@"\\.\PHYSICALDRIVE0");
        result[0].DisplayName.Should().Contain("Samsung SSD 990 PRO");
        result[0].DisplayName.Should().Contain("Disk 0");
        result[0].DisplayName.Should().Contain("NVMe / SSD");
    }

    [Fact]
    public async Task ScanAsync_ShouldParseSmartAttributesIntoExportFriendlyRows()
    {
        var drive = new DriveSmartDiskSnapshot(
            0,
            @"\\.\PHYSICALDRIVE0",
            "Samsung SSD 990 PRO",
            2_000_000_000_000,
            "NVMe",
            "SSD",
            "OK",
            "ABC123",
            "5B2QJXD7",
            @"PCI\VEN_144D&DEV_A80A");
        var service = new WindowsDriveSmartHealthService(
            _ => [drive],
            _ => new DriveSmartScanSnapshot(
                drive,
                new DriveSmartPhysicalDiskSnapshot(
                    "Samsung SSD 990 PRO",
                    2_000_000_000_000,
                    "NVMe",
                    "SSD",
                    "Healthy",
                    "ABC123",
                    "5B2QJXD7",
                    "0"),
                new DriveSmartStatusSnapshot("SAMSUNGSSD990PRO_ABC123", false, string.Empty),
                new DriveSmartDataSnapshot("SAMSUNGSSD990PRO_ABC123", CreateSmartData(
                    (0x05, 100, new byte[] { 0, 0, 0, 0, 0, 0 }),
                    (0xE8, 100, new byte[] { 100, 0, 0, 0, 0, 0 }))),
                new DriveSmartThresholdSnapshot("SAMSUNGSSD990PRO_ABC123", CreateSmartThresholds(
                    (0x05, 10),
                    (0xE8, 10))),
                []));

        var result = await service.ScanAsync(drive.DeviceId);

        result.OverallHealth.Should().Be("Healthy");
        result.Attributes.Should().HaveCount(2);
        result.Attributes[0].Byte.Should().Be("05");
        result.Attributes[0].Status.Should().Be("OK");
        result.Attributes[0].Description.Should().Be("Reallocated Sector Count");
        result.Attributes[0].RawData.Should().Be("0 (0x000000000000)");
        result.Attributes[1].Byte.Should().Be("E8");
        result.Attributes[1].Description.Should().Be("Available Reserved Space");
        result.Attributes[1].RawData.Should().Be("100 (0x000000000064)");
    }

    [Fact]
    public async Task ScanAsync_ShouldWarnWhenSsdSpareAreaRunsLow()
    {
        var drive = new DriveSmartDiskSnapshot(
            0,
            @"\\.\PHYSICALDRIVE0",
            "Samsung SSD 990 PRO",
            2_000_000_000_000,
            "NVMe",
            "SSD",
            "OK",
            "ABC123",
            "5B2QJXD7",
            @"PCI\VEN_144D&DEV_A80A");
        var service = new WindowsDriveSmartHealthService(
            _ => [drive],
            _ => new DriveSmartScanSnapshot(
                drive,
                new DriveSmartPhysicalDiskSnapshot(
                    "Samsung SSD 990 PRO",
                    2_000_000_000_000,
                    "NVMe",
                    "SSD",
                    "Healthy",
                    "ABC123",
                    "5B2QJXD7",
                    "0"),
                new DriveSmartStatusSnapshot("SAMSUNGSSD990PRO_ABC123", false, string.Empty),
                new DriveSmartDataSnapshot("SAMSUNGSSD990PRO_ABC123", CreateSmartData((0xE8, 15, new byte[] { 15, 0, 0, 0, 0, 0 }))),
                new DriveSmartThresholdSnapshot("SAMSUNGSSD990PRO_ABC123", CreateSmartThresholds()),
                []));

        var result = await service.ScanAsync(drive.DeviceId);

        result.OverallHealth.Should().Be("Warning");
        result.Attributes.Should().ContainSingle();
        result.Attributes[0].Status.Should().Be("Warning");
        result.Summary.Should().ContainEquivalentOf("reserved space");
        result.Summary.Should().Contain("15%");
    }

    [Fact]
    public async Task ScanAsync_ShouldEscalateOverallHealthWhenSectorIssuesAccumulateWithoutPredictFailure()
    {
        var drive = new DriveSmartDiskSnapshot(
            1,
            @"\\.\PHYSICALDRIVE1",
            "Test SSD",
            1_000_000_000_000,
            "SATA",
            "SSD",
            "OK",
            "DEF456",
            "1.0",
            @"SCSI\DISK&VEN_TEST&PROD_SSD");
        var service = new WindowsDriveSmartHealthService(
            _ => [drive],
            _ => new DriveSmartScanSnapshot(
                drive,
                new DriveSmartPhysicalDiskSnapshot("Test SSD", 1_000_000_000_000, "SATA", "SSD", "Healthy", "DEF456", "1.0", "1"),
                new DriveSmartStatusSnapshot("TESTSSD_DEF456", false, string.Empty),
                new DriveSmartDataSnapshot("TESTSSD_DEF456", CreateSmartData((0xC5, 100, new byte[] { 12, 0, 0, 0, 0, 0 }))),
                new DriveSmartThresholdSnapshot("TESTSSD_DEF456", CreateSmartThresholds()),
                []));

        var result = await service.ScanAsync(drive.DeviceId);

        result.OverallHealth.Should().Be("Critical");
        result.Attributes.Should().ContainSingle();
        result.Attributes[0].Status.Should().Be("Critical");
        result.Summary.Should().Contain("Current Pending Sector Count");
    }

    [Fact]
    public async Task ScanAsync_ShouldEscalateOverallHealthWhenPredictFailureIsReported()
    {
        var drive = new DriveSmartDiskSnapshot(
            1,
            @"\\.\PHYSICALDRIVE1",
            "Test SSD",
            1_000_000_000_000,
            "SATA",
            "SSD",
            "OK",
            "DEF456",
            "1.0",
            @"SCSI\DISK&VEN_TEST&PROD_SSD");
        var service = new WindowsDriveSmartHealthService(
            _ => [drive],
            _ => new DriveSmartScanSnapshot(
                drive,
                new DriveSmartPhysicalDiskSnapshot("Test SSD", 1_000_000_000_000, "SATA", "SSD", "Healthy", "DEF456", "1.0", "1"),
                new DriveSmartStatusSnapshot("TESTSSD_DEF456", true, string.Empty),
                new DriveSmartDataSnapshot("TESTSSD_DEF456", CreateSmartData((0x05, 1, new byte[] { 2, 0, 0, 0, 0, 0 }))),
                new DriveSmartThresholdSnapshot("TESTSSD_DEF456", CreateSmartThresholds((0x05, 10))),
                []));

        var result = await service.ScanAsync(drive.DeviceId);

        result.OverallHealth.Should().Be("Critical");
        result.Summary.Should().Contain("predicted a failure");
    }

    private static byte[] CreateSmartData(params (byte Id, byte Current, byte[] RawData)[] attributes)
    {
        var bytes = new byte[362];
        bytes[0] = 0x01;
        bytes[1] = 0x00;

        for (var index = 0; index < attributes.Length; index++)
        {
            var offset = 2 + (index * 12);
            bytes[offset] = attributes[index].Id;
            bytes[offset + 1] = 0;
            bytes[offset + 2] = 0;
            bytes[offset + 3] = attributes[index].Current;
            bytes[offset + 4] = attributes[index].Current;
            Array.Copy(attributes[index].RawData, 0, bytes, offset + 5, Math.Min(attributes[index].RawData.Length, 6));
        }

        return bytes;
    }

    private static byte[] CreateSmartThresholds(params (byte Id, byte Threshold)[] attributes)
    {
        var bytes = new byte[362];
        bytes[0] = 0x01;
        bytes[1] = 0x00;

        for (var index = 0; index < attributes.Length; index++)
        {
            var offset = 2 + (index * 12);
            bytes[offset] = attributes[index].Id;
            bytes[offset + 1] = attributes[index].Threshold;
        }

        return bytes;
    }
}
