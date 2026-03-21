using System.Xml.Linq;
using FluentAssertions;
using MultiTool.Core.Models;
using MultiTool.Infrastructure.Windows.Tools;

namespace MultiTool.Infrastructure.Windows.Tests;

public sealed class WindowsStorageBenchmarkServiceTests
{
    [Fact]
    public async Task GetAvailableTargetsAsync_ShouldReturnFormattedMountedSsdTargets()
    {
        var service = new WindowsStorageBenchmarkService(
            _ =>
            [
                new StorageBenchmarkTargetSnapshot(
                    @"\\.\PHYSICALDRIVE0|C:",
                    0,
                    "Samsung SSD 990 PRO",
                    "1.8 TB",
                    "NVMe",
                    "SSD",
                    "5B2QJXD7",
                    "ABC123",
                    @"C:\",
                    "Windows",
                    "NTFS",
                    "621.4 GB",
                    621_400_000_000,
                    2_000_000_000_000),
            ],
            executionReader: null,
            winSatExecutablePath: "winsat.exe");

        var result = await service.GetAvailableTargetsAsync();

        result.Should().ContainSingle();
        result[0].TargetId.Should().Be(@"\\.\PHYSICALDRIVE0|C:");
        result[0].DisplayName.Should().Contain("Samsung SSD 990 PRO");
        result[0].DisplayName.Should().Contain("C: (Windows)");
        result[0].DisplayName.Should().Contain("NVMe / SSD");
    }

    [Fact]
    public async Task GetAvailableTargetsAsync_ShouldPreferCDriveThenAlphabeticalDriveLetters()
    {
        var service = new WindowsStorageBenchmarkService(
            _ =>
            [
                new StorageBenchmarkTargetSnapshot(
                    @"\\.\PHYSICALDRIVE1|D:",
                    1,
                    "Games SSD",
                    "931.5 GB",
                    "NVMe",
                    "SSD",
                    "1.0",
                    "DDD1",
                    @"D:\",
                    "Games",
                    "NTFS",
                    "300 GB",
                    300UL * 1024UL * 1024UL * 1024UL,
                    931UL * 1024UL * 1024UL * 1024UL),
                new StorageBenchmarkTargetSnapshot(
                    @"\\.\PHYSICALDRIVE2|E:",
                    2,
                    "Media SSD",
                    "2 TB",
                    "NVMe",
                    "SSD",
                    "1.0",
                    "EEE1",
                    @"E:\",
                    "Media",
                    "NTFS",
                    "1 TB",
                    1_000UL * 1024UL * 1024UL * 1024UL,
                    2_000UL * 1024UL * 1024UL * 1024UL),
                new StorageBenchmarkTargetSnapshot(
                    @"\\.\PHYSICALDRIVE0|C:",
                    0,
                    "System SSD",
                    "1.8 TB",
                    "NVMe",
                    "SSD",
                    "1.0",
                    "CCC1",
                    @"C:\",
                    "Windows",
                    "NTFS",
                    "600 GB",
                    600UL * 1024UL * 1024UL * 1024UL,
                    2_000UL * 1024UL * 1024UL * 1024UL),
            ],
            executionReader: null,
            winSatExecutablePath: "winsat.exe");

        var result = await service.GetAvailableTargetsAsync();

        result.Select(static target => target.VolumeRootPath).Should().ContainInOrder(@"C:\", @"D:\", @"E:\");
        result[0].Model.Should().Be("System SSD");
    }

    [Fact]
    public async Task RunAsync_ShouldBuildFourModesAndBalanceAssessment()
    {
        var target = new StorageBenchmarkTargetSnapshot(
            @"\\.\PHYSICALDRIVE0|C:",
            0,
            "Samsung SSD 990 PRO",
            "1.8 TB",
            "NVMe",
            "SSD",
            "5B2QJXD7",
            "ABC123",
            @"C:\",
            "Windows",
            "NTFS",
            "621.4 GB",
            621_400_000_000,
            2_000_000_000_000);
        var execution = new StorageBenchmarkExecutionSnapshot(
            new StorageBenchmarkMeasurement("Sequential Read", 2800, 65536),
            new StorageBenchmarkMeasurement("Sequential Write", 1900, 65536),
            new StorageBenchmarkMeasurement("Random Read", 820, 16384),
            new StorageBenchmarkMeasurement("Random Write", 280, 16384),
            new StorageBenchmarkSystemSnapshot("Intel Core i7", 8, 16, 32UL * 1024UL * 1024UL * 1024UL, "NVIDIA RTX 4070", 12UL * 1024UL * 1024UL * 1024UL),
            []);
        var service = new WindowsStorageBenchmarkService(
            _ => [target],
            (_, _, _) => Task.FromResult(execution),
            winSatExecutablePath: "winsat.exe");

        var result = await service.RunAsync(target.TargetId);

        result.Results.Should().HaveCount(4);
        result.Results.Select(static measurement => measurement.Mode).Should().ContainInOrder(
            "Sequential Read",
            "Sequential Write",
            "Random Read",
            "Random Write");
        result.Summary.Should().Contain("Seq read 2,800 MB/s");
        result.BalanceAssessment.Should().Contain("Good match");
        result.DetectedSystemSummary.Should().Contain("Intel Core i7");
        result.DetectedSystemSummary.Should().Contain("32 GB RAM");
    }

    [Fact]
    public async Task RunAsync_ShouldAddPreflightWarningsForSystemDriveAndLowFreeSpace()
    {
        var systemRoot = Path.GetPathRoot(Environment.SystemDirectory) ?? @"C:\";
        var target = new StorageBenchmarkTargetSnapshot(
            @"\\.\PHYSICALDRIVE0|C:",
            0,
            "Samsung SSD 990 PRO",
            "1.8 TB",
            "NVMe",
            "SSD",
            "5B2QJXD7",
            "ABC123",
            systemRoot,
            "Windows",
            "NTFS",
            "12 GB",
            12UL * 1024UL * 1024UL * 1024UL,
            1_000UL * 1024UL * 1024UL * 1024UL);
        var execution = new StorageBenchmarkExecutionSnapshot(
            new StorageBenchmarkMeasurement("Sequential Read", 2800, 65536),
            new StorageBenchmarkMeasurement("Sequential Write", 1900, 65536),
            new StorageBenchmarkMeasurement("Random Read", 820, 16384),
            new StorageBenchmarkMeasurement("Random Write", 280, 16384),
            new StorageBenchmarkSystemSnapshot("Intel Core i7", 8, 16, 32UL * 1024UL * 1024UL * 1024UL, "NVIDIA RTX 4070", 12UL * 1024UL * 1024UL * 1024UL),
            []);
        var service = new WindowsStorageBenchmarkService(
            _ => [target],
            (_, _, _) => Task.FromResult(execution),
            winSatExecutablePath: "winsat.exe");

        var result = await service.RunAsync(target.TargetId);

        result.Warnings.Should().Contain(warning => warning.Contains("system drive", StringComparison.OrdinalIgnoreCase));
        result.Warnings.Should().Contain(warning => warning.Contains("12 GB free", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task RunAsync_ShouldForwardLiveProgressUpdates()
    {
        var target = new StorageBenchmarkTargetSnapshot(
            @"\\.\PHYSICALDRIVE0|C:",
            0,
            "Samsung SSD 990 PRO",
            "1.8 TB",
            "NVMe",
            "SSD",
            "5B2QJXD7",
            "ABC123",
            @"C:\",
            "Windows",
            "NTFS",
            "621.4 GB",
            621_400_000_000,
            2_000_000_000_000);
        var execution = new StorageBenchmarkExecutionSnapshot(
            new StorageBenchmarkMeasurement("Sequential Read", 2800, 65536),
            new StorageBenchmarkMeasurement("Sequential Write", 1900, 65536),
            new StorageBenchmarkMeasurement("Random Read", 820, 16384),
            new StorageBenchmarkMeasurement("Random Write", 280, 16384),
            new StorageBenchmarkSystemSnapshot("Intel Core i7", 8, 16, 32UL * 1024UL * 1024UL * 1024UL, "NVIDIA RTX 4070", 12UL * 1024UL * 1024UL * 1024UL),
            []);
        List<StorageBenchmarkProgressUpdate> progressUpdates = [];
        var service = new WindowsStorageBenchmarkService(
            _ => [target],
            (_, progress, _) =>
            {
                progress?.Report(new StorageBenchmarkProgressUpdate(1, 3, "Sequential and random read", "Running WinSAT read tests on C:."));
                progress?.Report(new StorageBenchmarkProgressUpdate(2, 3, "Sequential write", "Running WinSAT sequential write on C:."));
                return Task.FromResult(execution);
            },
            winSatExecutablePath: "winsat.exe");

        await service.RunAsync(target.TargetId, new Progress<StorageBenchmarkProgressUpdate>(progressUpdates.Add));

        progressUpdates.Should().HaveCount(2);
        progressUpdates[0].StageName.Should().Be("Sequential and random read");
        progressUpdates[1].CurrentStage.Should().Be(2);
    }

    [Fact]
    public void ParseSystemSnapshot_ShouldReadProcessorMemoryAndGraphicsFromWinSatXml()
    {
        var document = XDocument.Parse(
            """
            <WinSAT>
              <SystemConfig>
                <Processor>
                  <Instance>
                    <ProcessorName>AMD Ryzen 7 7800X3D</ProcessorName>
                    <NumCores>8</NumCores>
                    <NumCPUs>16</NumCPUs>
                  </Instance>
                </Processor>
                <Memory>
                  <TotalPhysical>
                    <Bytes>34359738368</Bytes>
                  </TotalPhysical>
                </Memory>
                <Graphics>
                  <AdapterDescription>NVIDIA GeForce RTX 4080</AdapterDescription>
                  <DedicatedVideoMemory>17179869184</DedicatedVideoMemory>
                </Graphics>
              </SystemConfig>
            </WinSAT>
            """);

        var snapshot = WindowsStorageBenchmarkService.ParseSystemSnapshot(document);

        snapshot.ProcessorName.Should().Be("AMD Ryzen 7 7800X3D");
        snapshot.CoreCount.Should().Be(8);
        snapshot.LogicalProcessors.Should().Be(16);
        snapshot.MemoryBytes.Should().Be(34359738368UL);
        snapshot.GraphicsAdapter.Should().Be("NVIDIA GeForce RTX 4080");
        snapshot.DedicatedGraphicsMemoryBytes.Should().Be(17179869184UL);
    }
}
