using FluentAssertions;
using MultiTool.App.ViewModels;
using MultiTool.Core.Defaults;
using MultiTool.Core.Models;
using System.IO;

namespace MultiTool.App.Tests;

public sealed partial class MainWindowViewModelSettingsFlowTests
{
    [Fact]
    public async Task LoadStorageBenchmarkTargetsAsync_ShouldPopulateChoicesWithoutRunningBenchmark()
    {
        await StaDispatcherTestRunner.RunAsync(
            async () =>
            {
                var context = new MainWindowViewModelTestContext(DefaultSettingsFactory.Create());
                context.StorageBenchmarkService.AvailableTargets =
                [
                    new StorageBenchmarkTargetInfo(
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
                        "621.4 GB"),
                ];

                await context.ViewModel.InitializeAsync();
                context.ViewModel.SelectedMainTabIndex = 4;

                await context.ViewModel.LoadStorageBenchmarkTargetsCommand.ExecuteAsync(null);

                context.StorageBenchmarkService.GetAvailableTargetsCallCount.Should().Be(1);
                context.StorageBenchmarkService.RunCallCount.Should().Be(0);
                context.ViewModel.StorageBenchmarkTargets.Should().ContainSingle();
                context.ViewModel.SelectedStorageBenchmarkTarget.Should().NotBeNull();
                context.ViewModel.StorageBenchmarkStatusMessage.Should().Contain("Loaded 1 benchmark-ready SSD volume");
            });
    }

    [Fact]
    public async Task RunSelectedStorageBenchmarkAsync_ShouldCacheResultsAndReloadThemWhenDriveIsReselected()
    {
        await StaDispatcherTestRunner.RunAsync(
            async () =>
            {
                var context = new MainWindowViewModelTestContext(DefaultSettingsFactory.Create());
                var firstTarget = new StorageBenchmarkTargetInfo(
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
                var secondTarget = new StorageBenchmarkTargetInfo(
                    @"\\.\PHYSICALDRIVE1|D:",
                    "WD Blue SN580  |  D: (Games)  |  931.5 GB  |  NVMe / SSD",
                    "WD Blue SN580",
                    "931.5 GB",
                    "NVMe",
                    "SSD",
                    "731400WD",
                    @"D:\",
                    "Games",
                    "NTFS",
                    "403.9 GB");

                context.StorageBenchmarkService.AvailableTargets = [firstTarget, secondTarget];
                context.StorageBenchmarkService.ReportsByTargetId[firstTarget.TargetId] =
                    new StorageBenchmarkReport(
                        firstTarget,
                        "Seq read 2,800 MB/s  |  Seq write 1,900 MB/s  |  Random read 820 MB/s  |  Random write 280 MB/s",
                        "Good match for the detected CPU and memory tier. Storage performance should keep up well with the rest of this PC.",
                        "Intel Core i7  |  32 GB RAM  |  NVIDIA RTX 4070",
                        DateTimeOffset.Parse("2026-03-20T21:30:00-07:00"),
                        [
                            new StorageBenchmarkModeResult("Sequential Read", 2800, 45875, 65536, "Large file reads."),
                            new StorageBenchmarkModeResult("Sequential Write", 1900, 31129, 65536, "Large file writes."),
                            new StorageBenchmarkModeResult("Random Read", 820, 52480, 16384, "Small-file reads."),
                            new StorageBenchmarkModeResult("Random Write", 280, 17920, 16384, "Small-file writes."),
                        ],
                        []);

                await context.ViewModel.InitializeAsync();
                context.ViewModel.SelectedMainTabIndex = 4;
                await context.ViewModel.LoadStorageBenchmarkTargetsCommand.ExecuteAsync(null);

                await context.ViewModel.RunSelectedStorageBenchmarkCommand.ExecuteAsync(null);

                context.StorageBenchmarkService.RunCallCount.Should().Be(1);
                context.ViewModel.StorageBenchmarkResults.Should().HaveCount(4);
                context.ViewModel.StorageBenchmarkBalanceAssessment.Should().Contain("Good match");

                context.ViewModel.SelectedStorageBenchmarkTarget = secondTarget;
                context.ViewModel.StorageBenchmarkResults.Should().BeEmpty();

                context.ViewModel.SelectedStorageBenchmarkTarget = firstTarget;
                context.StorageBenchmarkService.RunCallCount.Should().Be(1);
                context.ViewModel.StorageBenchmarkResults.Should().HaveCount(4);
                context.ViewModel.StorageBenchmarkStatusMessage.Should().Contain("Loaded cached SSD benchmark results");
            });
    }

    [Fact]
    public async Task RunSelectedStorageBenchmarkAsync_ShouldShowLiveStageProgressWhileBenchmarkRuns()
    {
        await StaDispatcherTestRunner.RunAsync(
            async () =>
            {
                var context = new MainWindowViewModelTestContext(DefaultSettingsFactory.Create());
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
                var completionSource = new TaskCompletionSource<StorageBenchmarkReport>(TaskCreationOptions.RunContinuationsAsynchronously);
                context.StorageBenchmarkService.AvailableTargets = [target];
                context.StorageBenchmarkService.RunOverride = (_, progress, _) =>
                {
                    progress?.Report(new StorageBenchmarkProgressUpdate(1, 3, "Sequential and random read", "Running WinSAT read tests on C:."));
                    return completionSource.Task;
                };

                await context.ViewModel.InitializeAsync();
                context.ViewModel.SelectedMainTabIndex = 4;
                await context.ViewModel.LoadStorageBenchmarkTargetsCommand.ExecuteAsync(null);

                var runTask = context.ViewModel.RunSelectedStorageBenchmarkCommand.ExecuteAsync(null);
                await Task.Delay(50);

                context.ViewModel.IsStorageBenchmarkProgressVisible.Should().BeTrue();
                context.ViewModel.StorageBenchmarkProgressValue.Should().Be(1);
                context.ViewModel.StorageBenchmarkProgressMaximum.Should().Be(3);
                context.ViewModel.StorageBenchmarkProgressSummary.Should().Contain("Step 1 of 3");
                context.ViewModel.StorageBenchmarkStatusMessage.Should().Contain("Step 1 of 3");

                completionSource.SetResult(
                    new StorageBenchmarkReport(
                        target,
                        "Seq read 2,800 MB/s  |  Seq write 1,900 MB/s  |  Random read 820 MB/s  |  Random write 280 MB/s",
                        "Good match for the detected CPU and memory tier. Storage performance should keep up well with the rest of this PC.",
                        "Intel Core i7  |  32 GB RAM  |  NVIDIA RTX 4070",
                        DateTimeOffset.Parse("2026-03-20T21:32:00-07:00"),
                        [
                            new StorageBenchmarkModeResult("Sequential Read", 2800, 45875, 65536, "Large file reads."),
                            new StorageBenchmarkModeResult("Sequential Write", 1900, 31129, 65536, "Large file writes."),
                            new StorageBenchmarkModeResult("Random Read", 820, 52480, 16384, "Small-file reads."),
                            new StorageBenchmarkModeResult("Random Write", 280, 17920, 16384, "Small-file writes."),
                        ],
                        []));

                await runTask;

                context.ViewModel.IsStorageBenchmarkProgressVisible.Should().BeFalse();
                context.ViewModel.StorageBenchmarkProgressSummary.Should().Contain("stage progress will appear here");
                context.ViewModel.StorageBenchmarkStatusMessage.Should().Contain("Finished benchmarking");
            });
    }

    [Fact]
    public async Task ExportStorageBenchmarkReportAsync_ShouldWriteExpectedColumns()
    {
        await StaDispatcherTestRunner.RunAsync(
            async () =>
            {
                var context = new MainWindowViewModelTestContext(DefaultSettingsFactory.Create());
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

                context.StorageBenchmarkService.AvailableTargets = [target];
                context.StorageBenchmarkService.ReportsByTargetId[target.TargetId] =
                    new StorageBenchmarkReport(
                        target,
                        "Seq read 2,800 MB/s  |  Seq write 1,900 MB/s  |  Random read 820 MB/s  |  Random write 280 MB/s",
                        "Good match for the detected CPU and memory tier. Storage performance should keep up well with the rest of this PC.",
                        "Intel Core i7  |  32 GB RAM  |  NVIDIA RTX 4070",
                        DateTimeOffset.Parse("2026-03-20T21:31:00-07:00"),
                        [
                            new StorageBenchmarkModeResult("Sequential Read", 2800, 45875, 65536, "Large file reads."),
                            new StorageBenchmarkModeResult("Sequential Write", 1900, 31129, 65536, "Large file writes."),
                            new StorageBenchmarkModeResult("Random Read", 820, 52480, 16384, "Small-file reads."),
                            new StorageBenchmarkModeResult("Random Write", 280, 17920, 16384, "Small-file writes."),
                        ],
                        []);

                var exportPath = Path.Combine(Path.GetTempPath(), $"multitool-ssd-benchmark-{Guid.NewGuid():N}.csv");
                context.TextFileSaveDialogService.NextResult = exportPath;

                await context.ViewModel.InitializeAsync();
                context.ViewModel.SelectedMainTabIndex = 4;
                await context.ViewModel.LoadStorageBenchmarkTargetsCommand.ExecuteAsync(null);
                await context.ViewModel.RunSelectedStorageBenchmarkCommand.ExecuteAsync(null);

                await context.ViewModel.ExportStorageBenchmarkReportCommand.ExecuteAsync(null);

                context.TextFileSaveDialogService.PickSavePathCallCount.Should().Be(1);
                File.Exists(exportPath).Should().BeTrue();

                var exportText = await File.ReadAllTextAsync(exportPath);
                exportText.Should().Contain("Drive,Volume,Detected System,Balance Assessment,Captured At,Mode,Throughput MB/s,IOPS,Block Size,Notes");
                exportText.Should().Contain("Sequential Read");
                exportText.Should().Contain("2800.00");
                exportText.Should().Contain("65536");
                exportText.Should().Contain("Good match for the detected CPU and memory tier");
                context.ViewModel.StorageBenchmarkStatusMessage.Should().Contain(exportPath);
            });
    }
}
