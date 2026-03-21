using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Management;
using System.Xml.Linq;
using MultiTool.Core.Models;
using MultiTool.Core.Services;

namespace MultiTool.Infrastructure.Windows.Tools;

internal delegate IReadOnlyList<StorageBenchmarkTargetSnapshot> StorageBenchmarkInventoryReader(ICollection<string> warnings);
internal delegate Task<StorageBenchmarkExecutionSnapshot> StorageBenchmarkExecutionReader(
    StorageBenchmarkTargetSnapshot target,
    IProgress<StorageBenchmarkProgressUpdate>? progress,
    CancellationToken cancellationToken);

public sealed class WindowsStorageBenchmarkService : IStorageBenchmarkService
{
    private const string DefaultManagementScope = @"\\.\root\cimv2";
    private const string StorageManagementScope = @"\\.\root\Microsoft\Windows\Storage";
    private const double LowFreeSpaceWarningRatio = 0.15d;
    private const ulong LowFreeSpaceWarningBytes = 40UL * 1024UL * 1024UL * 1024UL;
    private readonly StorageBenchmarkInventoryReader inventoryReader;
    private readonly StorageBenchmarkExecutionReader executionReader;
    private readonly string winSatExecutablePath;

    public WindowsStorageBenchmarkService()
        : this(ReadTargetInventory, executionReader: null)
    {
    }

    internal WindowsStorageBenchmarkService(
        StorageBenchmarkInventoryReader inventoryReader,
        StorageBenchmarkExecutionReader? executionReader,
        string? winSatExecutablePath = null)
    {
        this.inventoryReader = inventoryReader;
        this.winSatExecutablePath = string.IsNullOrWhiteSpace(winSatExecutablePath)
            ? Path.Combine(Environment.SystemDirectory, "winsat.exe")
            : winSatExecutablePath;
        this.executionReader = executionReader ?? ExecuteWithWinSatAsync;
    }

    public Task<IReadOnlyList<StorageBenchmarkTargetInfo>> GetAvailableTargetsAsync(CancellationToken cancellationToken = default) =>
        Task.Run(
            () =>
            {
                List<string> warnings = [];
                var snapshots = inventoryReader(warnings);
                return BuildAvailableTargets(snapshots);
            },
            cancellationToken);

    public async Task<StorageBenchmarkReport> RunAsync(
        string targetId,
        IProgress<StorageBenchmarkProgressUpdate>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(targetId))
        {
            throw new ArgumentException("A benchmark target is required.", nameof(targetId));
        }

        List<string> warnings = [];
        var targets = await Task.Run(() => inventoryReader(warnings), cancellationToken).ConfigureAwait(false);
        var target = targets.FirstOrDefault(candidate => candidate.TargetId.Equals(targetId, StringComparison.OrdinalIgnoreCase));
        if (target is null)
        {
            throw new InvalidOperationException($"SSD benchmark target '{targetId}' could not be found.");
        }

        EnsureBenchmarkTargetIsReady(target);
        var targetWarnings = BuildTargetWarnings(target);
        var execution = await executionReader(target, progress, cancellationToken).ConfigureAwait(false);
        var combinedWarnings = warnings
            .Concat(targetWarnings)
            .Concat(execution.Warnings)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return BuildReport(target, execution, combinedWarnings);
    }

    private async Task<StorageBenchmarkExecutionSnapshot> ExecuteWithWinSatAsync(
        StorageBenchmarkTargetSnapshot target,
        IProgress<StorageBenchmarkProgressUpdate>? progress,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(winSatExecutablePath))
        {
            throw new InvalidOperationException("Windows System Assessment Tool (WinSAT) is unavailable on this PC.");
        }

        var driveLetter = ExtractDriveLetter(target.VolumeRootPath);
        if (string.IsNullOrWhiteSpace(driveLetter))
        {
            throw new InvalidOperationException($"'{target.VolumeRootPath}' is not a benchmarkable mounted volume.");
        }

        var tempDirectory = Path.Combine(Path.GetTempPath(), "MultiTool", "StorageBenchmarks");
        Directory.CreateDirectory(tempDirectory);

        var safeDriveName = driveLetter.TrimEnd(':').ToUpperInvariant();
        var readXmlPath = Path.Combine(tempDirectory, $"storage-benchmark-read-{safeDriveName}-{Guid.NewGuid():N}.xml");
        var sequentialWriteXmlPath = Path.Combine(tempDirectory, $"storage-benchmark-seq-write-{safeDriveName}-{Guid.NewGuid():N}.xml");
        var randomWriteXmlPath = Path.Combine(tempDirectory, $"storage-benchmark-ran-write-{safeDriveName}-{Guid.NewGuid():N}.xml");
        var readStep = new StorageBenchmarkStep(
            "sequential and random read",
            $"diskformal -drive {driveLetter} -xml \"{readXmlPath}\"",
            readXmlPath);
        var sequentialWriteStep = new StorageBenchmarkStep(
            "sequential write",
            $"disk -seq -write -drive {driveLetter} -xml \"{sequentialWriteXmlPath}\"",
            sequentialWriteXmlPath);
        var randomWriteStep = new StorageBenchmarkStep(
            "random write",
            $"disk -ran -write -ransize 16384 -drive {driveLetter} -xml \"{randomWriteXmlPath}\"",
            randomWriteXmlPath);

        try
        {
            ReportProgress(
                progress,
                currentStage: 1,
                totalStages: 3,
                "Sequential and random read",
                $"Running WinSAT read tests on {target.VolumeRootPath.TrimEnd('\\')}.");
            await RunWinSatCommandAsync(readStep, cancellationToken).ConfigureAwait(false);
            ReportProgress(
                progress,
                currentStage: 2,
                totalStages: 3,
                "Sequential write",
                $"Running WinSAT sequential write on {target.VolumeRootPath.TrimEnd('\\')}.");
            await RunWinSatCommandAsync(sequentialWriteStep, cancellationToken).ConfigureAwait(false);
            ReportProgress(
                progress,
                currentStage: 3,
                totalStages: 3,
                "Random write",
                $"Running WinSAT random write on {target.VolumeRootPath.TrimEnd('\\')}.");
            await RunWinSatCommandAsync(randomWriteStep, cancellationToken).ConfigureAwait(false);

            var readDocument = LoadBenchmarkDocument(readStep);
            var sequentialWriteDocument = LoadBenchmarkDocument(sequentialWriteStep);
            var randomWriteDocument = LoadBenchmarkDocument(randomWriteStep);

            var sequentialRead = ParseMeasurement(readDocument, "Sequential Read");
            var randomRead = ParseMeasurement(readDocument, "Random Read");
            var sequentialWrite = ParseMeasurement(sequentialWriteDocument, "Sequential Write");
            var randomWrite = ParseMeasurement(randomWriteDocument, "Random Write");
            var system = ParseSystemSnapshot(readDocument);
            var warnings = BuildExecutionWarnings(target, sequentialRead, sequentialWrite, randomRead, randomWrite);

            return new StorageBenchmarkExecutionSnapshot(
                sequentialRead,
                sequentialWrite,
                randomRead,
                randomWrite,
                system,
                warnings);
        }
        finally
        {
            TryDeleteFile(readXmlPath);
            TryDeleteFile(sequentialWriteXmlPath);
            TryDeleteFile(randomWriteXmlPath);
        }
    }

    private async Task RunWinSatCommandAsync(StorageBenchmarkStep step, CancellationToken cancellationToken)
    {
        using Process process = new();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = winSatExecutablePath,
            Arguments = step.Arguments,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
        };

        process.Start();
        using var registration = cancellationToken.Register(
            static state =>
            {
                try
                {
                    ((Process)state!).Kill(entireProcessTree: true);
                }
                catch
                {
                }
            },
            process);

        var standardOutputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var standardErrorTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        var standardOutput = await standardOutputTask.ConfigureAwait(false);
        var standardError = await standardErrorTask.ConfigureAwait(false);

        if (process.ExitCode == 0)
        {
            if (!File.Exists(step.OutputXmlPath))
            {
                throw new InvalidOperationException($"WinSAT finished the {step.ModeDescription} benchmark without writing its XML results.");
            }

            var fileInfo = new FileInfo(step.OutputXmlPath);
            if (fileInfo.Length == 0)
            {
                throw new InvalidOperationException($"WinSAT finished the {step.ModeDescription} benchmark, but the XML result file was empty.");
            }

            return;
        }

        var detail = FirstNonEmpty(standardError, standardOutput, $"WinSAT exited with code {process.ExitCode}.");
        throw new InvalidOperationException($"Unable to run the {step.ModeDescription} benchmark: {detail}");
    }

    private static IReadOnlyList<StorageBenchmarkTargetInfo> BuildAvailableTargets(IReadOnlyList<StorageBenchmarkTargetSnapshot> targets) =>
        targets
            .OrderBy(static target => target.DiskIndex)
            .ThenBy(static target => target.VolumeRootPath, StringComparer.OrdinalIgnoreCase)
            .Select(BuildTargetInfo)
            .ToArray();

    private static StorageBenchmarkReport BuildReport(
        StorageBenchmarkTargetSnapshot target,
        StorageBenchmarkExecutionSnapshot execution,
        IReadOnlyList<string> warnings)
    {
        var results = new[]
        {
            BuildModeResult(execution.SequentialRead, "Large file reads, loading assets, and copying data off the SSD."),
            BuildModeResult(execution.SequentialWrite, "Large installs, exports, and big file copies onto the SSD."),
            BuildModeResult(execution.RandomRead, "App launches, small file lookups, and metadata-heavy reads."),
            BuildModeResult(execution.RandomWrite, "Small writes, patching, caches, and temp-file activity."),
        };

        return new StorageBenchmarkReport(
            BuildTargetInfo(target),
            BuildBenchmarkSummary(results),
            BuildBalanceAssessment(target, execution),
            BuildDetectedSystemSummary(execution.System),
            DateTimeOffset.Now,
            results,
            warnings);
    }

    private static StorageBenchmarkTargetInfo BuildTargetInfo(StorageBenchmarkTargetSnapshot target)
    {
        var volumeLabel = string.IsNullOrWhiteSpace(target.VolumeLabel)
            ? target.VolumeRootPath.TrimEnd('\\')
            : $"{target.VolumeRootPath.TrimEnd('\\')} ({target.VolumeLabel})";
        var displayName = string.Join(
            "  |  ",
            new[]
            {
                target.Model,
                volumeLabel,
                string.IsNullOrWhiteSpace(target.SizeText) ? string.Empty : target.SizeText,
                $"{target.InterfaceType} / {target.MediaType}",
            }.Where(static value => !string.IsNullOrWhiteSpace(value)));

        return new StorageBenchmarkTargetInfo(
            target.TargetId,
            displayName,
            target.Model,
            target.SizeText,
            target.InterfaceType,
            target.MediaType,
            target.FirmwareVersion,
            target.VolumeRootPath,
            target.VolumeLabel,
            target.FileSystem,
            target.FreeSpaceText);
    }

    private static StorageBenchmarkModeResult BuildModeResult(StorageBenchmarkMeasurement measurement, string notes) =>
        new(
            measurement.Mode,
            measurement.ThroughputMegabytesPerSecond,
            CalculateIops(measurement.ThroughputMegabytesPerSecond, measurement.BlockSizeBytes),
            measurement.BlockSizeBytes,
            notes);

    private static double CalculateIops(double throughputMegabytesPerSecond, int blockSizeBytes)
    {
        if (throughputMegabytesPerSecond <= 0 || blockSizeBytes <= 0)
        {
            return 0d;
        }

        return (throughputMegabytesPerSecond * 1024d * 1024d) / blockSizeBytes;
    }

    private static string BuildBenchmarkSummary(IReadOnlyList<StorageBenchmarkModeResult> results)
    {
        var sequentialRead = results.FirstOrDefault(result => result.Mode.Equals("Sequential Read", StringComparison.OrdinalIgnoreCase));
        var sequentialWrite = results.FirstOrDefault(result => result.Mode.Equals("Sequential Write", StringComparison.OrdinalIgnoreCase));
        var randomRead = results.FirstOrDefault(result => result.Mode.Equals("Random Read", StringComparison.OrdinalIgnoreCase));
        var randomWrite = results.FirstOrDefault(result => result.Mode.Equals("Random Write", StringComparison.OrdinalIgnoreCase));

        return string.Join(
            "  |  ",
            new[]
            {
                sequentialRead is null ? string.Empty : $"Seq read {sequentialRead.ThroughputMegabytesPerSecond:N0} MB/s",
                sequentialWrite is null ? string.Empty : $"Seq write {sequentialWrite.ThroughputMegabytesPerSecond:N0} MB/s",
                randomRead is null ? string.Empty : $"Random read {randomRead.ThroughputMegabytesPerSecond:N0} MB/s",
                randomWrite is null ? string.Empty : $"Random write {randomWrite.ThroughputMegabytesPerSecond:N0} MB/s",
            }.Where(static value => !string.IsNullOrWhiteSpace(value)));
    }

    private static string BuildBalanceAssessment(StorageBenchmarkTargetSnapshot target, StorageBenchmarkExecutionSnapshot execution)
    {
        var systemTier = DetermineSystemTier(execution.System);
        var driveTier = DetermineDriveTier(target, execution);
        var isNvme = target.InterfaceType.Contains("NVMe", StringComparison.OrdinalIgnoreCase);
        var driveClass = isNvme ? "NVMe" : "SSD";

        return (systemTier, driveTier, isNvme) switch
        {
            (<= 0, >= 1, _) => $"Good enough for the detected CPU and memory tier. This {driveClass} should feel balanced for everyday work on this PC.",
            (1, 2, _) => "Good match for the detected CPU and memory tier. Storage performance should keep up well with the rest of this PC.",
            (1, 1, true) => "Good enough for the detected CPU and memory tier. This SSD is not the fastest NVMe result, but it should still feel balanced in normal use.",
            (1, 1, false) => "Usable for the detected CPU and memory tier, but storage is a little behind the rest of the system. Large installs and file copies may feel slower than the CPU and RAM suggest.",
            (>= 2, >= 2, _) => "Good match for the detected higher-end CPU and memory tier. Storage speed looks strong enough to avoid being an obvious bottleneck.",
            (>= 2, 1, true) => "Below what a stronger NVMe setup would usually deliver for the detected CPU and memory tier. It is still usable, but storage may hold this PC back during heavier workloads.",
            (>= 2, _, false) => "Likely the slowest part of this system. For the detected CPU and memory tier, this SSD looks noticeably behind the rest of the hardware.",
            _ => "Below the pace suggested by the detected CPU and memory tier. Background load, thermal throttling, low free space, or the drive itself may be limiting performance.",
        };
    }

    private static int DetermineSystemTier(StorageBenchmarkSystemSnapshot system)
    {
        var memoryGigabytes = system.MemoryBytes / (1024d * 1024d * 1024d);
        var hasDedicatedGraphics = system.GraphicsAdapter.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase)
            || system.GraphicsAdapter.Contains("Radeon", StringComparison.OrdinalIgnoreCase)
            || system.DedicatedGraphicsMemoryBytes >= 2UL * 1024UL * 1024UL * 1024UL;

        if (system.LogicalProcessors >= 12 || memoryGigabytes >= 32d || (system.LogicalProcessors >= 8 && hasDedicatedGraphics))
        {
            return 2;
        }

        if (system.LogicalProcessors >= 8 || memoryGigabytes >= 16d)
        {
            return 1;
        }

        return 0;
    }

    private static int DetermineDriveTier(StorageBenchmarkTargetSnapshot target, StorageBenchmarkExecutionSnapshot execution)
    {
        var sequentialRead = execution.SequentialRead.ThroughputMegabytesPerSecond;
        var sequentialWrite = execution.SequentialWrite.ThroughputMegabytesPerSecond;
        var randomRead = execution.RandomRead.ThroughputMegabytesPerSecond;
        var randomWrite = execution.RandomWrite.ThroughputMegabytesPerSecond;
        var isNvme = target.InterfaceType.Contains("NVMe", StringComparison.OrdinalIgnoreCase);

        if (isNvme)
        {
            if (sequentialRead >= 2500 && sequentialWrite >= 1000 && randomRead >= 500 && randomWrite >= 120)
            {
                return 2;
            }

            if (sequentialRead >= 1500 && sequentialWrite >= 600 && randomRead >= 250 && randomWrite >= 60)
            {
                return 1;
            }

            return 0;
        }

        if (sequentialRead >= 500 && sequentialWrite >= 400 && randomRead >= 80 && randomWrite >= 35)
        {
            return 1;
        }

        return 0;
    }

    private static string BuildDetectedSystemSummary(StorageBenchmarkSystemSnapshot system)
    {
        var memoryText = system.MemoryBytes == 0
            ? string.Empty
            : $"{FormatBytes(system.MemoryBytes)} RAM";
        var processorText = FirstNonEmpty(system.ProcessorName, "Unknown CPU");
        var graphicsText = FirstNonEmpty(system.GraphicsAdapter, "Graphics unavailable");

        return string.Join(
            "  |  ",
            new[]
            {
                processorText,
                memoryText,
                graphicsText,
            }.Where(static value => !string.IsNullOrWhiteSpace(value)));
    }

    private static StorageBenchmarkMeasurement ParseMeasurement(XDocument document, string kind)
    {
        var metric = document
            .Descendants("AvgThroughput")
            .FirstOrDefault(element => kind.Equals((string?)element.Attribute("kind"), StringComparison.OrdinalIgnoreCase));
        if (metric is null)
        {
            throw new InvalidOperationException($"WinSAT did not return a '{kind}' result.");
        }

        var throughputText = metric.Value?.Trim();
        if (!double.TryParse(throughputText, NumberStyles.Float, CultureInfo.InvariantCulture, out var throughput))
        {
            throw new InvalidOperationException($"WinSAT returned an invalid throughput for '{kind}'.");
        }

        var ioSizeText = ((string?)metric.Attribute("ioSize"))?.Trim();
        var blockSizeBytes = int.TryParse(ioSizeText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedIoSize)
            ? parsedIoSize
            : 0;

        return new StorageBenchmarkMeasurement(kind, throughput, blockSizeBytes);
    }

    private static void ReportProgress(
        IProgress<StorageBenchmarkProgressUpdate>? progress,
        int currentStage,
        int totalStages,
        string stageName,
        string detail) =>
        progress?.Report(new StorageBenchmarkProgressUpdate(currentStage, totalStages, stageName, detail));

    private static void EnsureBenchmarkTargetIsReady(StorageBenchmarkTargetSnapshot target)
    {
        if (string.IsNullOrWhiteSpace(target.VolumeRootPath) || !Directory.Exists(target.VolumeRootPath))
        {
            throw new InvalidOperationException($"The selected SSD volume '{target.VolumeRootPath}' is no longer available.");
        }
    }

    private static IReadOnlyList<string> BuildTargetWarnings(StorageBenchmarkTargetSnapshot target)
    {
        List<string> warnings = [];
        var systemVolumeRoot = EnsureVolumeRootPath(Path.GetPathRoot(Environment.SystemDirectory) ?? string.Empty);
        if (!string.IsNullOrWhiteSpace(systemVolumeRoot)
            && target.VolumeRootPath.Equals(systemVolumeRoot, StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("Benchmarking the Windows/system drive can be noisier because background OS activity can affect the numbers.");
        }

        if (!string.IsNullOrWhiteSpace(target.FileSystem)
            && !target.FileSystem.Equals("NTFS", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add($"This volume uses {target.FileSystem}. WinSAT usually produces the most consistent storage results on NTFS volumes.");
        }

        if (target.FreeSpaceBytes > 0
            && (target.FreeSpaceBytes < LowFreeSpaceWarningBytes
                || (target.VolumeSizeBytes > 0 && (double)target.FreeSpaceBytes / target.VolumeSizeBytes < LowFreeSpaceWarningRatio)))
        {
            warnings.Add($"This volume only has {FormatBytes(target.FreeSpaceBytes)} free. Write scores can drop or vary more when an SSD is close to full.");
        }

        if (target.InterfaceType.Contains("USB", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("This SSD is connected over USB, so the enclosure or port may cap performance below the drive's native speed.");
        }

        return warnings;
    }

    private static IReadOnlyList<string> BuildExecutionWarnings(
        StorageBenchmarkTargetSnapshot target,
        StorageBenchmarkMeasurement sequentialRead,
        StorageBenchmarkMeasurement sequentialWrite,
        StorageBenchmarkMeasurement randomRead,
        StorageBenchmarkMeasurement randomWrite)
    {
        List<string> warnings = [];
        var isNvme = target.InterfaceType.Contains("NVMe", StringComparison.OrdinalIgnoreCase);
        var isUsb = target.InterfaceType.Contains("USB", StringComparison.OrdinalIgnoreCase);

        if (sequentialRead.ThroughputMegabytesPerSecond > 0
            && sequentialWrite.ThroughputMegabytesPerSecond > 0
            && sequentialWrite.ThroughputMegabytesPerSecond < sequentialRead.ThroughputMegabytesPerSecond * 0.25d)
        {
            warnings.Add("Sequential write speed is far below read speed. Low free space, exhausted cache, sustained throttling, or background writes may be dragging the result down.");
        }

        if (randomRead.ThroughputMegabytesPerSecond > 0
            && randomWrite.ThroughputMegabytesPerSecond > 0
            && randomWrite.ThroughputMegabytesPerSecond < randomRead.ThroughputMegabytesPerSecond * 0.20d)
        {
            warnings.Add("Random write speed is far below random read speed. Background activity, low free space, or controller limits may be affecting the benchmark.");
        }

        if (isNvme && !isUsb && sequentialRead.ThroughputMegabytesPerSecond > 0 && sequentialRead.ThroughputMegabytesPerSecond < 1000d)
        {
            warnings.Add("This NVMe result is much lower than expected. Power saving, thermal throttling, RAID layers, or background load may be limiting the benchmark.");
        }
        else if (!isNvme && !isUsb && sequentialRead.ThroughputMegabytesPerSecond > 0 && sequentialRead.ThroughputMegabytesPerSecond < 250d)
        {
            warnings.Add("This SSD benchmark is on the low side for a local SSD. Link speed limits, thermal throttling, or background activity may be affecting the result.");
        }

        return warnings;
    }

    private static XDocument LoadBenchmarkDocument(StorageBenchmarkStep step)
    {
        try
        {
            return XDocument.Load(step.OutputXmlPath);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"WinSAT wrote an unreadable XML result for the {step.ModeDescription} benchmark: {ex.Message}",
                ex);
        }
    }

    internal static StorageBenchmarkSystemSnapshot ParseSystemSnapshot(XDocument document)
    {
        var processorName = GetDescendantValue(document.Root, "ProcessorName");
        var numCores = ParseInt(GetDescendantValue(document.Root, "NumCores"));
        var logicalProcessors = ParseInt(GetDescendantValue(document.Root, "NumCPUs"));
        var memoryBytes = ParseUInt64(GetDescendantValue(document.Root, "Bytes"));
        var graphicsAdapter = GetDescendantValue(document.Root, "AdapterDescription");
        var dedicatedGraphicsMemoryBytes = ParseUInt64(GetDescendantValue(document.Root, "DedicatedVideoMemory"));

        return new StorageBenchmarkSystemSnapshot(
            processorName,
            numCores,
            logicalProcessors,
            memoryBytes,
            graphicsAdapter,
            dedicatedGraphicsMemoryBytes);
    }

    private static string GetDescendantValue(XContainer? container, string name) =>
        container?
            .Descendants(name)
            .Select(static element => element.Value?.Trim())
            .FirstOrDefault(static value => !string.IsNullOrWhiteSpace(value))
        ?? string.Empty;

    private static int ParseInt(string value) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : 0;

    private static ulong ParseUInt64(string value) =>
        ulong.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : 0UL;

    private static IReadOnlyList<StorageBenchmarkTargetSnapshot> ReadTargetInventory(ICollection<string> warnings)
    {
        var disks = TryReadMany(
            DefaultManagementScope,
            "SELECT Index, DeviceID, Model, Size, InterfaceType, MediaType, SerialNumber, FirmwareRevision, PNPDeviceID FROM Win32_DiskDrive WHERE DeviceID IS NOT NULL OR Model IS NOT NULL",
            item => new StorageBenchmarkDiskSnapshot(
                GetInt(item, "Index"),
                GetString(item, "DeviceID"),
                GetString(item, "Model", "Unknown drive"),
                GetUInt64(item, "Size"),
                GetString(item, "InterfaceType", "Unknown interface"),
                NormalizeWin32MediaType(GetString(item, "MediaType", "Unknown media")),
                GetString(item, "SerialNumber"),
                GetString(item, "FirmwareRevision"),
                GetString(item, "PNPDeviceID")),
            warnings,
            "SSD benchmark drive inventory");

        var physicalDisks = TryReadMany(
            StorageManagementScope,
            "SELECT FriendlyName, Size, BusType, MediaType, SerialNumber, FirmwareVersion, DeviceId FROM MSFT_PhysicalDisk",
            item => new StorageBenchmarkPhysicalDiskSnapshot(
                GetString(item, "FriendlyName"),
                GetUInt64(item, "Size"),
                MapBusType(GetInt(item, "BusType")),
                MapStorageMediaType(GetInt(item, "MediaType")),
                GetString(item, "SerialNumber"),
                GetString(item, "FirmwareVersion"),
                GetString(item, "DeviceId")),
            warnings,
            "SSD benchmark physical disk inventory");

        var partitions = TryReadMany(
            DefaultManagementScope,
            "SELECT DiskIndex, DeviceID FROM Win32_DiskPartition",
            item => new StorageBenchmarkPartitionSnapshot(
                GetInt(item, "DiskIndex"),
                GetString(item, "DeviceID")),
            warnings,
            "SSD benchmark partition inventory");

        var logicalDisks = TryReadMany(
            DefaultManagementScope,
            "SELECT DeviceID, VolumeName, FileSystem, FreeSpace, Size, DriveType FROM Win32_LogicalDisk WHERE DeviceID IS NOT NULL AND DriveType = 3",
            item => new StorageBenchmarkLogicalDiskSnapshot(
                GetString(item, "DeviceID"),
                GetString(item, "VolumeName"),
                GetString(item, "FileSystem"),
                GetUInt64(item, "FreeSpace"),
                GetUInt64(item, "Size"),
                GetInt(item, "DriveType")),
            warnings,
            "SSD benchmark logical disk inventory");

        var partitionLinks = TryReadMany(
            DefaultManagementScope,
            "SELECT Antecedent, Dependent FROM Win32_LogicalDiskToPartition",
            item => new StorageBenchmarkPartitionLinkSnapshot(
                ParseEmbeddedPropertyValue(GetString(item, "Antecedent"), "DeviceID"),
                ParseEmbeddedPropertyValue(GetString(item, "Dependent"), "DeviceID")),
            warnings,
            "SSD benchmark partition map");

        var logicalDiskLookup = logicalDisks
            .Where(static disk => !string.IsNullOrWhiteSpace(disk.DeviceId))
            .ToDictionary(static disk => disk.DeviceId, StringComparer.OrdinalIgnoreCase);
        var partitionVolumeLookup = partitionLinks
            .Where(static link => !string.IsNullOrWhiteSpace(link.PartitionDeviceId) && !string.IsNullOrWhiteSpace(link.LogicalDiskId))
            .GroupBy(static link => link.PartitionDeviceId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => group.Select(static link => link.LogicalDiskId).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
                StringComparer.OrdinalIgnoreCase);

        List<StorageBenchmarkTargetSnapshot> targets = [];

        foreach (var disk in disks.OrderBy(static disk => disk.Index).ThenBy(static disk => disk.Model, StringComparer.OrdinalIgnoreCase))
        {
            var physicalDisk = MatchPhysicalDisk(disk, physicalDisks);
            var interfaceType = FirstNonEmpty(
                physicalDisk is not null && !IsUnknownValue(physicalDisk.BusType) ? physicalDisk.BusType : string.Empty,
                disk.InterfaceType,
                "Unknown interface");
            var mediaType = FirstNonEmpty(
                physicalDisk is not null && !IsUnknownValue(physicalDisk.MediaType) ? physicalDisk.MediaType : string.Empty,
                disk.MediaType,
                "Unknown media");

            if (!IsSolidStateTarget(interfaceType, mediaType, disk.Model))
            {
                continue;
            }

            var partitionIds = partitions
                .Where(partition => partition.DiskIndex == disk.Index && !string.IsNullOrWhiteSpace(partition.DeviceId))
                .Select(static partition => partition.DeviceId)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var logicalDiskIds = partitionIds
                .SelectMany(partitionId => partitionVolumeLookup.TryGetValue(partitionId, out var mountedVolumes) ? mountedVolumes : [])
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            foreach (var logicalDiskId in logicalDiskIds)
            {
                if (!logicalDiskLookup.TryGetValue(logicalDiskId, out var logicalDisk))
                {
                    continue;
                }

                var volumeRootPath = EnsureVolumeRootPath(logicalDisk.DeviceId);
                if (string.IsNullOrWhiteSpace(volumeRootPath))
                {
                    continue;
                }

                targets.Add(
                    new StorageBenchmarkTargetSnapshot(
                        $"{disk.DeviceId}|{logicalDisk.DeviceId}",
                        disk.Index,
                        FirstNonEmpty(disk.Model, disk.DeviceId, $"Disk {disk.Index}"),
                        FormatBytes(disk.Size),
                        interfaceType,
                        mediaType,
                        FirstNonEmpty(physicalDisk?.FirmwareVersion, disk.FirmwareRevision, "Unavailable"),
                        FirstNonEmpty(disk.SerialNumber, physicalDisk?.SerialNumber, "Unavailable"),
                        volumeRootPath,
                        logicalDisk.VolumeName,
                        FirstNonEmpty(logicalDisk.FileSystem, "Unknown"),
                        FormatBytes(logicalDisk.FreeSpace),
                        logicalDisk.FreeSpace,
                        logicalDisk.Size));
            }
        }

        return targets;
    }

    private static StorageBenchmarkPhysicalDiskSnapshot? MatchPhysicalDisk(
        StorageBenchmarkDiskSnapshot snapshot,
        IReadOnlyList<StorageBenchmarkPhysicalDiskSnapshot> physicalDisks)
    {
        var candidates = physicalDisks
            .Select(
                physicalDisk => new
                {
                    Disk = physicalDisk,
                    Score = ScorePhysicalDiskMatch(snapshot, physicalDisk),
                })
            .Where(static item => item.Score > 0)
            .OrderByDescending(static item => item.Score)
            .ToArray();

        return candidates.Length == 0 ? null : candidates[0].Disk;
    }

    private static int ScorePhysicalDiskMatch(StorageBenchmarkDiskSnapshot snapshot, StorageBenchmarkPhysicalDiskSnapshot physicalDisk)
    {
        var bestScore = 0;

        foreach (var token in GetMatchTokens(snapshot.Model, snapshot.SerialNumber, snapshot.PnpDeviceId, snapshot.DeviceId))
        {
            bestScore = Math.Max(bestScore, ScoreMatchToken(token, physicalDisk.SerialNumber, exactMatchBonus: 120));
            bestScore = Math.Max(bestScore, ScoreMatchToken(token, physicalDisk.FriendlyName, exactMatchBonus: 90));
            bestScore = Math.Max(bestScore, ScoreMatchToken(token, physicalDisk.DeviceId, exactMatchBonus: 60));
        }

        if (snapshot.Size > 0 && physicalDisk.Size > 0 && snapshot.Size == physicalDisk.Size)
        {
            bestScore += 15;
        }

        return bestScore;
    }

    private static int ScoreMatchToken(string token, string candidateValue, int exactMatchBonus = 80)
    {
        var normalizedToken = NormalizeToken(token);
        var normalizedCandidate = NormalizeToken(candidateValue);
        if (string.IsNullOrWhiteSpace(normalizedToken) || string.IsNullOrWhiteSpace(normalizedCandidate))
        {
            return 0;
        }

        if (normalizedToken.Equals(normalizedCandidate, StringComparison.OrdinalIgnoreCase))
        {
            return exactMatchBonus;
        }

        if (normalizedCandidate.Contains(normalizedToken, StringComparison.OrdinalIgnoreCase)
            || normalizedToken.Contains(normalizedCandidate, StringComparison.OrdinalIgnoreCase))
        {
            return 40;
        }

        return 0;
    }

    private static IEnumerable<string> GetMatchTokens(params string?[] values) =>
        values
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value!.Trim())
            .Select(NormalizeToken)
            .Where(static value => value.Length >= 4 && !value.StartsWith("UNKNOWN", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase);

    private static bool IsSolidStateTarget(string interfaceType, string mediaType, string model)
    {
        if (mediaType.Contains("SSD", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (interfaceType.Contains("NVMe", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return model.Contains("SSD", StringComparison.OrdinalIgnoreCase)
            || model.Contains("NVME", StringComparison.OrdinalIgnoreCase);
    }

    private static string ExtractDriveLetter(string volumeRootPath)
    {
        if (string.IsNullOrWhiteSpace(volumeRootPath) || volumeRootPath.Length < 1)
        {
            return string.Empty;
        }

        var root = Path.GetPathRoot(volumeRootPath);
        if (string.IsNullOrWhiteSpace(root) || root.Length < 2 || root[1] != ':')
        {
            return string.Empty;
        }

        return char.ToLowerInvariant(root[0]).ToString(CultureInfo.InvariantCulture);
    }

    private static string EnsureVolumeRootPath(string deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            return string.Empty;
        }

        return deviceId.EndsWith(@"\", StringComparison.Ordinal)
            ? deviceId
            : deviceId + @"\";
    }

    private static IReadOnlyList<T> TryReadMany<T>(
        string scopePath,
        string query,
        Func<ManagementObject, T> selector,
        ICollection<string> warnings,
        string context)
    {
        try
        {
            using var searcher = CreateSearcher(scopePath, query);
            return
            [
                .. searcher.Get().Cast<ManagementObject>().Select(selector),
            ];
        }
        catch (Exception ex)
        {
            warnings.Add(FormatWarningMessage(context, ex));
            return [];
        }
    }

    private static ManagementObjectSearcher CreateSearcher(string scopePath, string query)
    {
        var scope = new ManagementScope(scopePath);
        scope.Connect();
        return new ManagementObjectSearcher(scope, new ObjectQuery(query));
    }

    private static string FormatWarningMessage(string context, Exception ex)
    {
        var message = ex.Message?.Trim();
        if (IsAccessDeniedException(ex))
        {
            return $"{context}: Access denied. Start MultiTool as administrator to read all storage details on this PC.";
        }

        return string.IsNullOrWhiteSpace(message)
            ? $"{context}: Unknown error."
            : $"{context}: {message}";
    }

    private static bool IsAccessDeniedException(Exception ex) =>
        ex is UnauthorizedAccessException
        || ex is ManagementException { ErrorCode: ManagementStatus.AccessDenied }
        || ex.Message.Contains("Access denied", StringComparison.OrdinalIgnoreCase);

    private static object? GetValue(ManagementBaseObject source, string propertyName)
    {
        if (source.Properties[propertyName] is { } property)
        {
            return property.Value;
        }

        return null;
    }

    private static string GetString(ManagementBaseObject source, string propertyName, string fallback = "")
    {
        var value = GetValue(source, propertyName);
        return Convert.ToString(value)?.Trim() ?? fallback;
    }

    private static int GetInt(ManagementBaseObject source, string propertyName)
    {
        var value = GetValue(source, propertyName);
        return value is null ? 0 : Convert.ToInt32(value, CultureInfo.InvariantCulture);
    }

    private static ulong GetUInt64(ManagementBaseObject source, string propertyName)
    {
        var value = GetValue(source, propertyName);
        return value is null ? 0UL : Convert.ToUInt64(value, CultureInfo.InvariantCulture);
    }

    private static string ParseEmbeddedPropertyValue(string path, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(propertyName))
        {
            return string.Empty;
        }

        var marker = propertyName + "=\"";
        var startIndex = path.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (startIndex < 0)
        {
            return string.Empty;
        }

        startIndex += marker.Length;
        var endIndex = path.IndexOf('"', startIndex);
        if (endIndex <= startIndex)
        {
            return string.Empty;
        }

        return path[startIndex..endIndex].Replace(@"\\", @"\");
    }

    private static string MapBusType(int busType) =>
        busType switch
        {
            1 => "SCSI",
            2 => "ATAPI",
            3 => "ATA",
            4 => "IEEE 1394",
            5 => "SSA",
            6 => "Fibre Channel",
            7 => "USB",
            8 => "RAID",
            9 => "iSCSI",
            10 => "SAS",
            11 => "SATA",
            12 => "SD",
            13 => "MMC",
            14 => "Virtual",
            15 => "File-backed virtual",
            16 => "Storage Spaces",
            17 => "NVMe",
            18 => "SCM",
            19 => "UFS",
            _ => string.Empty,
        };

    private static string MapStorageMediaType(int mediaType) =>
        mediaType switch
        {
            3 => "HDD",
            4 => "SSD",
            5 => "SCM",
            _ => string.Empty,
        };

    private static string NormalizeWin32MediaType(string mediaType)
    {
        if (string.IsNullOrWhiteSpace(mediaType))
        {
            return string.Empty;
        }

        if (mediaType.Contains("SSD", StringComparison.OrdinalIgnoreCase)
            || mediaType.Contains("Solid State", StringComparison.OrdinalIgnoreCase))
        {
            return "SSD";
        }

        if (mediaType.Contains("Fixed hard disk", StringComparison.OrdinalIgnoreCase)
            || mediaType.Contains("Hard Disk", StringComparison.OrdinalIgnoreCase)
            || mediaType.Contains("HDD", StringComparison.OrdinalIgnoreCase))
        {
            return "HDD";
        }

        return mediaType;
    }

    private static string FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(static value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? string.Empty;

    private static bool IsUnknownValue(string value) =>
        string.IsNullOrWhiteSpace(value)
        || value.Equals("Unknown", StringComparison.OrdinalIgnoreCase)
        || value.Equals("Unknown media", StringComparison.OrdinalIgnoreCase)
        || value.Equals("Unknown interface", StringComparison.OrdinalIgnoreCase);

    private static string NormalizeToken(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return new string(value.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
    }

    private static string FormatBytes(ulong bytes)
    {
        if (bytes == 0)
        {
            return string.Empty;
        }

        string[] suffixes = ["B", "KB", "MB", "GB", "TB", "PB"];
        decimal scaled = bytes;
        var index = 0;
        while (scaled >= 1024 && index < suffixes.Length - 1)
        {
            scaled /= 1024;
            index++;
        }

        return $"{scaled:0.#} {suffixes[index]}";
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
        }
    }
}

internal sealed record StorageBenchmarkDiskSnapshot(
    int Index,
    string DeviceId,
    string Model,
    ulong Size,
    string InterfaceType,
    string MediaType,
    string SerialNumber,
    string FirmwareRevision,
    string PnpDeviceId);

internal sealed record StorageBenchmarkPhysicalDiskSnapshot(
    string FriendlyName,
    ulong Size,
    string BusType,
    string MediaType,
    string SerialNumber,
    string FirmwareVersion,
    string DeviceId);

internal sealed record StorageBenchmarkPartitionSnapshot(
    int DiskIndex,
    string DeviceId);

internal sealed record StorageBenchmarkLogicalDiskSnapshot(
    string DeviceId,
    string VolumeName,
    string FileSystem,
    ulong FreeSpace,
    ulong Size,
    int DriveType);

internal sealed record StorageBenchmarkPartitionLinkSnapshot(
    string PartitionDeviceId,
    string LogicalDiskId);

internal sealed record StorageBenchmarkTargetSnapshot(
    string TargetId,
    int DiskIndex,
    string Model,
    string SizeText,
    string InterfaceType,
    string MediaType,
    string FirmwareVersion,
    string SerialNumber,
    string VolumeRootPath,
    string VolumeLabel,
    string FileSystem,
    string FreeSpaceText,
    ulong FreeSpaceBytes,
    ulong VolumeSizeBytes);

internal sealed record StorageBenchmarkMeasurement(
    string Mode,
    double ThroughputMegabytesPerSecond,
    int BlockSizeBytes);

internal sealed record StorageBenchmarkExecutionSnapshot(
    StorageBenchmarkMeasurement SequentialRead,
    StorageBenchmarkMeasurement SequentialWrite,
    StorageBenchmarkMeasurement RandomRead,
    StorageBenchmarkMeasurement RandomWrite,
    StorageBenchmarkSystemSnapshot System,
    IReadOnlyList<string> Warnings);

internal sealed record StorageBenchmarkStep(
    string ModeDescription,
    string Arguments,
    string OutputXmlPath);

internal sealed record StorageBenchmarkSystemSnapshot(
    string ProcessorName,
    int CoreCount,
    int LogicalProcessors,
    ulong MemoryBytes,
    string GraphicsAdapter,
    ulong DedicatedGraphicsMemoryBytes);
