using FluentAssertions;
using MultiTool.App.Localization;
using MultiTool.App.ViewModels;
using MultiTool.Core.Defaults;
using MultiTool.Core.Models;
using System.IO;

namespace MultiTool.App.Tests;

public sealed partial class MainWindowViewModelSettingsFlowTests
{
    [Fact]
    public async Task LoadDriveSmartTargetsAsync_ShouldPopulateChoicesWithoutScanningHealth()
    {
        await StaDispatcherTestRunner.RunAsync(
            async () =>
            {
                var context = new MainWindowViewModelTestContext(DefaultSettingsFactory.Create());
                context.DriveSmartHealthService.AvailableDrives =
                [
                    new DriveSmartTargetInfo(
                        @"\\.\PHYSICALDRIVE0",
                        "Samsung SSD 990 PRO  |  Disk 0  |  1.8 TB  |  NVMe / SSD",
                        "Samsung SSD 990 PRO",
                        "1.8 TB",
                        "NVMe",
                        "SSD",
                        "5B2QJXD7",
                        "ABC123"),
                ];

                await context.ViewModel.InitializeAsync();
                context.ViewModel.SelectedMainTabIndex = 4;

                await context.ViewModel.LoadDriveSmartTargetsCommand.ExecuteAsync(null);

                context.DriveSmartHealthService.GetAvailableDrivesCallCount.Should().Be(1);
                context.DriveSmartHealthService.ScanCallCount.Should().Be(0);
                context.ViewModel.DriveSmartTargets.Should().ContainSingle();
                context.ViewModel.SelectedDriveSmartTarget.Should().NotBeNull();
                context.ViewModel.DriveSmartStatusMessage.Should().Contain("Loaded 1 drive");
            });
    }

    [Fact]
    public async Task ScanSelectedDriveSmartAsync_ShouldCacheResultsAndReloadThemWhenDriveIsReselected()
    {
        await StaDispatcherTestRunner.RunAsync(
            async () =>
            {
                var context = new MainWindowViewModelTestContext(DefaultSettingsFactory.Create());
                var firstDrive = new DriveSmartTargetInfo(
                    @"\\.\PHYSICALDRIVE0",
                    "Samsung SSD 990 PRO  |  Disk 0  |  1.8 TB  |  NVMe / SSD",
                    "Samsung SSD 990 PRO",
                    "1.8 TB",
                    "NVMe",
                    "SSD",
                    "5B2QJXD7",
                    "ABC123");
                var secondDrive = new DriveSmartTargetInfo(
                    @"\\.\PHYSICALDRIVE1",
                    "WD Blue SN580  |  Disk 1  |  931.5 GB  |  NVMe / SSD",
                    "WD Blue SN580",
                    "931.5 GB",
                    "NVMe",
                    "SSD",
                    "731400WD",
                    "DEF456");

                context.DriveSmartHealthService.AvailableDrives = [firstDrive, secondDrive];
                context.DriveSmartHealthService.ReportsByDeviceId[firstDrive.DeviceId] =
                    new DriveSmartHealthReport(
                        firstDrive,
                        "Healthy",
                        "Overall health: Healthy. Read 2 SMART attributes.",
                        DateTimeOffset.Parse("2026-03-20T20:40:00-07:00"),
                        [
                            new DriveSmartAttributeInfo("05", "OK", "Reallocated Sector Count", "0 (0x000000000000)"),
                            new DriveSmartAttributeInfo("09", "Info", "Power-On Hours", "1040 (0x000000000410)"),
                        ],
                        []);

                await context.ViewModel.InitializeAsync();
                context.ViewModel.SelectedMainTabIndex = 4;
                await context.ViewModel.LoadDriveSmartTargetsCommand.ExecuteAsync(null);

                await context.ViewModel.ScanSelectedDriveSmartCommand.ExecuteAsync(null);

                context.DriveSmartHealthService.ScanCallCount.Should().Be(1);
                context.ViewModel.DriveSmartAttributes.Should().HaveCount(2);
                context.ViewModel.DriveSmartOverallHealth.Should().Be("Healthy");

                context.ViewModel.SelectedDriveSmartTarget = secondDrive;
                context.ViewModel.DriveSmartAttributes.Should().BeEmpty();

                context.ViewModel.SelectedDriveSmartTarget = firstDrive;
                context.DriveSmartHealthService.ScanCallCount.Should().Be(1);
                context.ViewModel.DriveSmartAttributes.Should().HaveCount(2);
                context.ViewModel.DriveSmartStatusMessage.Should().Contain("Loaded cached SMART data");
            });
    }

    [Fact]
    public async Task ExportDriveSmartReportAsync_ShouldWriteRequestedColumns()
    {
        await StaDispatcherTestRunner.RunAsync(
            async () =>
            {
                var context = new MainWindowViewModelTestContext(DefaultSettingsFactory.Create());
                var drive = new DriveSmartTargetInfo(
                    @"\\.\PHYSICALDRIVE0",
                    "Samsung SSD 990 PRO  |  Disk 0  |  1.8 TB  |  NVMe / SSD",
                    "Samsung SSD 990 PRO",
                    "1.8 TB",
                    "NVMe",
                    "SSD",
                    "5B2QJXD7",
                    "ABC123");

                context.DriveSmartHealthService.AvailableDrives = [drive];
                context.DriveSmartHealthService.ReportsByDeviceId[drive.DeviceId] =
                    new DriveSmartHealthReport(
                        drive,
                        "Healthy",
                        "Overall health: Healthy. Read 1 SMART attribute.",
                        DateTimeOffset.Parse("2026-03-20T20:41:00-07:00"),
                        [
                            new DriveSmartAttributeInfo("E8", "OK", "Available Reserved Space", "100 (0x000000000064)"),
                        ],
                        []);

                var exportPath = Path.Combine(Path.GetTempPath(), $"multitool-smart-export-{Guid.NewGuid():N}.csv");
                context.TextFileSaveDialogService.NextResult = exportPath;

                await context.ViewModel.InitializeAsync();
                context.ViewModel.SelectedMainTabIndex = 4;
                await context.ViewModel.LoadDriveSmartTargetsCommand.ExecuteAsync(null);
                await context.ViewModel.ScanSelectedDriveSmartCommand.ExecuteAsync(null);

                await context.ViewModel.ExportDriveSmartReportCommand.ExecuteAsync(null);

                context.TextFileSaveDialogService.PickSavePathCallCount.Should().Be(1);
                File.Exists(exportPath).Should().BeTrue();

                var exportText = await File.ReadAllTextAsync(exportPath);
                exportText.Should().Contain("Drive,Overall Health,Captured At,Byte,Status,Description,Raw Data");
                exportText.Should().Contain("E8");
                exportText.Should().Contain("Available Reserved Space");
                exportText.Should().Contain("100 (0x000000000064)");
                context.ViewModel.DriveSmartStatusMessage.Should().Contain(exportPath);
            });
    }
}
