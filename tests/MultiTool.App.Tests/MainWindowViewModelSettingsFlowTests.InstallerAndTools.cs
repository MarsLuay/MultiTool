using FluentAssertions;
using MultiTool.App.Localization;
using MultiTool.App.Models;
using MultiTool.App.Services;
using MultiTool.App.ViewModels;
using MultiTool.Core.Defaults;
using MultiTool.Core.Enums;
using MultiTool.Core.Models;
using MultiTool.Core.Results;
using MultiTool.Core.Services;
using MultiTool.Core.Validation;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Data;
using System.Windows.Input;

namespace MultiTool.App.Tests;


public sealed partial class MainWindowViewModelSettingsFlowTests
{
    [Fact]
    public async Task InitializeAsync_ShouldDeferInstallerInitializationUntilInstallerTabIsSelected()
    {
        await StaDispatcherTestRunner.RunAsync(
            async () =>
            {
                var context = new MainWindowViewModelTestContext(DefaultSettingsFactory.Create());

                await context.ViewModel.InitializeAsync();

                context.InstallerService.GetCatalogCallCount.Should().Be(0);
                context.InstallerService.GetCleanupCatalogCallCount.Should().Be(0);
                context.InstallerService.GetPackageStatusesCallCount.Should().Be(0);
                context.ViewModel.InstallerPackages.Should().BeEmpty();
                context.ViewModel.CleanupPackages.Should().BeEmpty();

                context.ViewModel.SelectedMainTabIndex = 3;
                await context.InstallerService.WaitForPackageStatusCallsAsync(expectedCount: 1);

                context.InstallerService.GetCatalogCallCount.Should().Be(1);
                context.InstallerService.GetCleanupCatalogCallCount.Should().Be(1);
                context.InstallerService.GetPackageStatusesCallCount.Should().Be(1);
                context.InstallerService.PackageStatusIncludeUpdateChecks.Should().ContainSingle().Which.Should().BeFalse();
            });
    }

    [Fact]
    public async Task CheckAllInstallerUpdatesAsync_ShouldRunDeferredUpdateScan()
    {
        await StaDispatcherTestRunner.RunAsync(
            async () =>
            {
                var context = new MainWindowViewModelTestContext(DefaultSettingsFactory.Create());

                await context.ViewModel.InitializeAsync();

                context.ViewModel.SelectedMainTabIndex = 3;
                await context.InstallerService.WaitForPackageStatusCallsAsync(expectedCount: 1);

                await context.ViewModel.CheckAllInstallerUpdatesCommand.ExecuteAsync(null);
                await context.InstallerService.WaitForPackageStatusCallsAsync(expectedCount: 2);

                context.InstallerService.PackageStatusIncludeUpdateChecks.Should().Equal(false, true);
            });
    }

    [Fact]
    public async Task UpgradeSelectedPackagesAsync_ShouldShowPerAppDownloadProgressAcrossQueuedUpdates()
    {
        await StaDispatcherTestRunner.RunAsync(
            async () =>
            {
                var context = new MainWindowViewModelTestContext(DefaultSettingsFactory.Create());
                context.InstallerService.CatalogItems =
                [
                    new InstallerCatalogItem("KiCad.KiCad", "KiCad", "CAD", "PCB design"),
                    new InstallerCatalogItem("Microsoft.VisualStudioCode", "Visual Studio Code", "Developer", "Editor"),
                ];
                context.InstallerService.PackageStatuses =
                [
                    new InstallerPackageStatus("KiCad.KiCad", true, true, "Update available."),
                    new InstallerPackageStatus("Microsoft.VisualStudioCode", true, true, "Update available."),
                ];

                var allowFirstPackageToFinish = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                var allowSecondPackageToFinish = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                var firstPackageProgressRaised = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                var secondPackageProgressRaised = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

                context.InstallerService.RunPackageOperationAsyncHandler = async (packageId, action, cancellationToken) =>
                {
                    if (string.Equals(packageId, "KiCad.KiCad", StringComparison.OrdinalIgnoreCase))
                    {
                        context.InstallerService.RaiseOperationProgress(packageId, "KiCad", action, "Downloading 20%...", 20);
                        firstPackageProgressRaised.TrySetResult();
                        await allowFirstPackageToFinish.Task.WaitAsync(cancellationToken);
                        return
                        [
                            new InstallerOperationResult(packageId, "KiCad", true, true, "Updated successfully.", string.Empty),
                        ];
                    }

                    if (string.Equals(packageId, "Microsoft.VisualStudioCode", StringComparison.OrdinalIgnoreCase))
                    {
                        context.InstallerService.RaiseOperationProgress(packageId, "Visual Studio Code", action, "Downloading 50%...", 50);
                        secondPackageProgressRaised.TrySetResult();
                        await allowSecondPackageToFinish.Task.WaitAsync(cancellationToken);
                        return
                        [
                            new InstallerOperationResult(packageId, "Visual Studio Code", true, true, "Updated successfully.", string.Empty),
                        ];
                    }

                    throw new InvalidOperationException($"Unexpected package queued in test: {packageId}");
                };

                await context.ViewModel.InitializeAsync();

                context.ViewModel.SelectedMainTabIndex = 3;
                await context.InstallerService.WaitForPackageStatusCallsAsync(expectedCount: 1);

                context.ViewModel.InstallerPackages.Should().HaveCount(2);
                foreach (var package in context.ViewModel.InstallerPackages)
                {
                    package.IsSelected = true;
                }

                var upgradeTask = context.ViewModel.UpgradeSelectedPackagesCommand.ExecuteAsync(null);

                await firstPackageProgressRaised.Task;
                await WaitForConditionAsync(
                    () => context.ViewModel.InstallerProgressText.Contains("KiCad", StringComparison.Ordinal)
                        && context.ViewModel.InstallerProgressText.Contains("[1/2]", StringComparison.Ordinal)
                        && context.ViewModel.InstallerProgressText.Contains("Downloading 20%", StringComparison.Ordinal));
                context.ViewModel.IsInstallerProgressIndeterminate.Should().BeFalse();
                context.ViewModel.InstallerProgressValue.Should().Be(20);

                allowFirstPackageToFinish.TrySetResult();

                await secondPackageProgressRaised.Task;
                await WaitForConditionAsync(
                    () => context.ViewModel.InstallerProgressText.Contains("Visual Studio Code", StringComparison.Ordinal)
                        && context.ViewModel.InstallerProgressText.Contains("[2/2]", StringComparison.Ordinal)
                        && context.ViewModel.InstallerProgressText.Contains("Downloading 50%", StringComparison.Ordinal));
                context.ViewModel.IsInstallerProgressIndeterminate.Should().BeFalse();
                context.ViewModel.InstallerProgressValue.Should().Be(50);

                allowSecondPackageToFinish.TrySetResult();
                await upgradeTask;

                context.ViewModel.IsInstallerProgressVisible.Should().BeFalse();
            });
    }

    [Fact]
    public async Task InitializeAsync_ShouldDeferToolsInitializationUntilToolsTabIsSelected()
    {
        await StaDispatcherTestRunner.RunAsync(
            async () =>
            {
                var context = new MainWindowViewModelTestContext(DefaultSettingsFactory.Create());

                await context.ViewModel.InitializeAsync();

                context.ViewModel.MouseSensitivityLevels.Should().BeEmpty();
                context.ViewModel.UsefulSites.Should().BeEmpty();

                context.ViewModel.SelectedMainTabIndex = 4;

                context.ViewModel.MouseSensitivityLevels.Should().NotBeEmpty();
                context.ViewModel.UsefulSites.Should().NotBeEmpty();
            });
    }

    [Fact]
    public async Task InitializeAsync_WhenInstallerTabLoads_ShouldGroupInstallerPackagesByCategory()
    {
        await StaDispatcherTestRunner.RunAsync(
            async () =>
            {
                var context = new MainWindowViewModelTestContext(DefaultSettingsFactory.Create());
                context.InstallerService.CatalogItems =
                [
                    new InstallerCatalogItem("Mozilla.Firefox", "Firefox", "Browsers", "Browser"),
                    new InstallerCatalogItem("Microsoft.VisualStudioCode", "VS Code", "Developer Tools", "Editor"),
                    new InstallerCatalogItem("Google.Chrome", "Chrome", "Browsers", "Browser"),
                ];

                await context.ViewModel.InitializeAsync();

                context.ViewModel.SelectedMainTabIndex = 3;
                await context.InstallerService.WaitForPackageStatusCallsAsync(expectedCount: 1);

                var groups = context.ViewModel.InstallerPackagesView.Groups!
                    .Cast<CollectionViewGroup>()
                    .ToArray();

                groups.Should().HaveCount(2);
                groups[0].Name.Should().Be("Browsers");
                groups[0].ItemCount.Should().Be(2);
                groups[1].Name.Should().Be("Developer Tools");
                groups[1].ItemCount.Should().Be(1);
            });
    }

    [Fact]
    public async Task ShowAssignedShortcutHotkeysAsync_ShouldRescanAfterCachedResultsExpire()
    {
        await StaDispatcherTestRunner.RunAsync(
            async () =>
            {
                var context = new MainWindowViewModelTestContext(
                    DefaultSettingsFactory.Create(),
                    toolScanResultRetentionWindow: TimeSpan.FromMilliseconds(50));
                context.ShortcutHotkeyInventoryService.NextResult = new ShortcutHotkeyScanResult(
                    [
                        new ShortcutHotkeyInfo(
                            "Ctrl+Alt+T",
                            "Test Shortcut",
                            @"C:\Temp\Test.lnk",
                            @"C:\Temp",
                            @"C:\Temp\Test.exe",
                            true),
                    ],
                    1,
                    []);

                await context.ViewModel.InitializeAsync();

                await context.ViewModel.ShowAssignedShortcutHotkeysCommand.ExecuteAsync(null);
                context.ShortcutHotkeyInventoryService.ScanCallCount.Should().Be(1);

                await WaitForConditionAsync(
                    () => context.ViewModel.ShortcutHotkeyStatusMessage == AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.ToolsStatusShortcutScanExpired));

                await context.ViewModel.ShowAssignedShortcutHotkeysCommand.ExecuteAsync(null);
                context.ShortcutHotkeyInventoryService.ScanCallCount.Should().Be(2);
            });
    }

    [Fact]
    public async Task CaptureIpv4SocketSnapshotAsync_ShouldAutoExpireVisibleResults()
    {
        await StaDispatcherTestRunner.RunAsync(
            async () =>
            {
                var context = new MainWindowViewModelTestContext(
                    DefaultSettingsFactory.Create(),
                    toolScanResultRetentionWindow: TimeSpan.FromMilliseconds(50));
                context.Ipv4SocketSnapshotService.NextResult = new Ipv4SocketSnapshotResult(
                    DateTimeOffset.Parse("2026-03-19T12:00:00Z"),
                    [
                        new Ipv4SocketEntry("tcp4", "ESTABLISHED", "127.0.0.1:5000", "93.184.216.34:443", "TestApp", 1234),
                    ],
                    1,
                    0,
                    0);

                await context.ViewModel.InitializeAsync();

                await context.ViewModel.CaptureIpv4SocketSnapshotCommand.ExecuteAsync(null);
                context.ViewModel.Ipv4SocketEntries.Should().HaveCount(1);

                await WaitForConditionAsync(() => context.ViewModel.Ipv4SocketEntries.Count == 0);

                context.ViewModel.Ipv4SocketStatusMessage.Should().Be(AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.ToolsStatusIpv4SocketExpired));
                context.ViewModel.Ipv4SocketSummary.Should().Be(AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.ToolsIpv4SocketSummaryEmpty));
            });
    }

    [Fact]
    public async Task ScanHardwareCheckAsync_ShouldExpireCachedDriverHardwareInventory()
    {
        await StaDispatcherTestRunner.RunAsync(
            async () =>
            {
                var context = new MainWindowViewModelTestContext(
                    DefaultSettingsFactory.Create(),
                    toolScanResultRetentionWindow: TimeSpan.FromMilliseconds(50));
                context.HardwareInventoryService.NextReport = new HardwareInventoryReport(
                    "Test system",
                    "Healthy",
                    "Windows 11",
                    "Test CPU",
                    "32 GB",
                    "Test board",
                    "Test BIOS",
                    [],
                    [],
                    [],
                    [],
                    [],
                    [],
                    [
                        new DriverHardwareInfo("GPU", "NVIDIA", "NVIDIA", "1.0.0", "Display", "PCI\\VEN_TEST"),
                    ],
                    []);
                context.DriverUpdateService.NextScanResult = new DriverUpdateScanResult([], [], []);

                await context.ViewModel.InitializeAsync();

                await context.ViewModel.ScanHardwareCheckCommand.ExecuteAsync(null);
                await context.ViewModel.ScanDriverUpdatesCommand.ExecuteAsync(null);
                context.DriverUpdateService.LastHardwareInventoryArgument.Should().NotBeNull();
                context.DriverUpdateService.LastHardwareInventoryArgument.Should().HaveCount(1);

                await WaitForConditionAsync(
                    () => context.ViewModel.HardwareCheckStatusMessage == AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.ToolsStatusHardwareScanExpired));

                await context.ViewModel.ScanDriverUpdatesCommand.ExecuteAsync(null);
                context.DriverUpdateService.LastHardwareInventoryArgument.Should().BeNull();
            });
    }

    [Fact]
    public async Task CaptureIpv4SocketSnapshotAsync_ShouldShowProgramsAndFriendlyClipboardText()
    {
        await StaDispatcherTestRunner.RunAsync(
            async () =>
            {
                var context = new MainWindowViewModelTestContext(DefaultSettingsFactory.Create());
                context.Ipv4SocketSnapshotService.NextResult = new Ipv4SocketSnapshotResult(
                    new DateTimeOffset(2026, 3, 19, 18, 30, 0, TimeSpan.Zero),
                    [
                        new Ipv4SocketEntry("tcp4", "ESTAB", "10.0.0.25:52344", "93.184.216.34:443", "chrome.exe", 7420),
                        new Ipv4SocketEntry("tcp4", "LISTEN", "0.0.0.0:3000", "*:*", "node.exe", 9216),
                        new Ipv4SocketEntry("udp4", "UNCONN", "127.0.0.1:5353", "*:*", string.Empty, 1104),
                    ],
                    1,
                    1,
                    1);

                await context.ViewModel.InitializeAsync();
                await context.ViewModel.CaptureIpv4SocketSnapshotCommand.ExecuteAsync(null);

                context.ViewModel.Ipv4SocketSummary.Should().Contain("across 3 apps");
                context.ViewModel.Ipv4SocketStatusMessage.Should().Contain("Found 3 IPv4 entries across 3 apps");
                context.ViewModel.Ipv4SocketEntries.Select(static entry => entry.ProgramSummary).Should().Contain(
                    "chrome.exe (PID 7420)",
                    "node.exe (PID 9216)",
                    "Unknown app (PID 1104)");

                context.ViewModel.CopyIpv4SocketSnapshotCommand.Execute(null);

                context.ClipboardTextService.LastText.Should().NotBeNull();
                context.ClipboardTextService.LastText.Should().Contain("IPv4 App Activity");
                context.ClipboardTextService.LastText.Should().Contain("chrome.exe (PID 7420)");
                context.ClipboardTextService.LastText.Should().Contain("TCP connection  |  tcp4  ESTAB");
                context.ClipboardTextService.LastText.Should().Contain("Unknown app (PID 1104)");
            });
    }

    [Fact]
    public async Task RestoreTelemetryDefaultsAsync_ShouldKeepRestoreSuccessMessage()
    {
        await StaDispatcherTestRunner.RunAsync(
            async () =>
            {
                var context = new MainWindowViewModelTestContext(DefaultSettingsFactory.Create());
                context.WindowsTelemetryService.Status = new WindowsTelemetryStatus(
                    false,
                    "Telemetry hardening not fully applied. AllowTelemetry policy is not set to 0.");
                context.WindowsTelemetryService.RestoreResult = new WindowsTelemetryResult(
                    true,
                    true,
                    "Restored telemetry defaults by removing the policy override, re-enabling telemetry tasks, and restoring telemetry service startup defaults.");

                await context.ViewModel.InitializeAsync();
                await context.ViewModel.RestoreTelemetryDefaultsCommand.ExecuteAsync(null);

                context.ViewModel.TelemetryToolStatusMessage.Should().Be(context.WindowsTelemetryService.RestoreResult.Message);
            });
    }
}
