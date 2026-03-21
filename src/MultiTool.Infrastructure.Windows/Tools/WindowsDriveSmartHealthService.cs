using System.Management;
using MultiTool.Core.Models;
using MultiTool.Core.Services;

namespace MultiTool.Infrastructure.Windows.Tools;

internal delegate IReadOnlyList<DriveSmartDiskSnapshot> DriveSmartDiskInventoryReader(ICollection<string> warnings);
internal delegate DriveSmartScanSnapshot DriveSmartScanReader(string deviceId);

public sealed class WindowsDriveSmartHealthService : IDriveSmartHealthService
{
    private const string DefaultManagementScope = @"\\.\root\cimv2";
    private const string StorageManagementScope = @"\\.\root\Microsoft\Windows\Storage";
    private const string WmiManagementScope = @"\\.\root\WMI";
    private const int SmartTemperatureWarningCelsius = 70;
    private const int SmartTemperatureCriticalCelsius = 80;
    private const int SmartRemainingLifeWarningPercent = 20;
    private const int SmartRemainingLifeCriticalPercent = 10;

    private static readonly IReadOnlyDictionary<byte, string> SmartAttributeDescriptions = new Dictionary<byte, string>
    {
        [0x01] = "Read Error Rate",
        [0x03] = "Spin-Up Time",
        [0x04] = "Start/Stop Count",
        [0x05] = "Reallocated Sector Count",
        [0x07] = "Seek Error Rate",
        [0x09] = "Power-On Hours",
        [0x0A] = "Spin Retry Count",
        [0x0C] = "Power Cycle Count",
        [0xAB] = "Program Fail Count",
        [0xAC] = "Erase Fail Count",
        [0xAE] = "Unexpected Power Loss Count",
        [0xB1] = "Wear Range Delta",
        [0xB3] = "Used Reserved Block Count Total",
        [0xB5] = "Program Fail Count Total",
        [0xB7] = "SATA Downshift Count",
        [0xB8] = "End-to-End Error",
        [0xBB] = "Reported Uncorrectable Errors",
        [0xBC] = "Command Timeout",
        [0xBE] = "Airflow Temperature",
        [0xBF] = "G-Sense Error Rate",
        [0xC0] = "Unsafe Shutdown Count",
        [0xC2] = "Temperature",
        [0xC3] = "Hardware ECC Recovered",
        [0xC4] = "Reallocation Event Count",
        [0xC5] = "Current Pending Sector Count",
        [0xC6] = "Uncorrectable Sector Count",
        [0xC7] = "UltraDMA CRC Error Count",
        [0xE7] = "SSD Life Left",
        [0xE8] = "Available Reserved Space",
        [0xE9] = "Media Wearout Indicator",
        [0xEA] = "Drive Life Protection Status",
        [0xF1] = "Total LBAs Written",
        [0xF2] = "Total LBAs Read",
    };

    private readonly DriveSmartDiskInventoryReader diskInventoryReader;
    private readonly DriveSmartScanReader scanReader;

    public WindowsDriveSmartHealthService()
        : this(ReadDiskInventory, ReadDriveScan)
    {
    }

    internal WindowsDriveSmartHealthService(
        DriveSmartDiskInventoryReader diskInventoryReader,
        DriveSmartScanReader scanReader)
    {
        this.diskInventoryReader = diskInventoryReader;
        this.scanReader = scanReader;
    }

    public Task<IReadOnlyList<DriveSmartTargetInfo>> GetAvailableDrivesAsync(CancellationToken cancellationToken = default) =>
        Task.Run(
            () =>
            {
                List<string> warnings = [];
                var diskSnapshots = diskInventoryReader(warnings);
                return BuildAvailableDrives(diskSnapshots);
            },
            cancellationToken);

    public Task<DriveSmartHealthReport> ScanAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            throw new ArgumentException("A drive ID is required.", nameof(deviceId));
        }

        return Task.Run(() => BuildReport(scanReader(deviceId)), cancellationToken);
    }

    private static IReadOnlyList<DriveSmartTargetInfo> BuildAvailableDrives(IReadOnlyList<DriveSmartDiskSnapshot> diskSnapshots) =>
        diskSnapshots
            .OrderBy(static disk => disk.Index)
            .ThenBy(static disk => disk.Model, StringComparer.OrdinalIgnoreCase)
            .Select(static disk => BuildTargetInfo(disk, physicalDisk: null))
            .ToArray();

    private static DriveSmartHealthReport BuildReport(DriveSmartScanSnapshot snapshot)
    {
        var attributeEvaluations = BuildSmartAttributes(snapshot.SmartData?.VendorSpecific, snapshot.SmartThresholds?.VendorSpecific);
        var attributes = attributeEvaluations
            .Select(static attribute => new DriveSmartAttributeInfo(attribute.Byte, attribute.Status, attribute.Description, attribute.RawData))
            .ToArray();
        var drive = BuildTargetInfo(snapshot.Drive, snapshot.PhysicalDisk);
        var overallHealth = BuildOverallHealth(snapshot, attributeEvaluations);
        var summary = BuildSummary(snapshot, attributeEvaluations, overallHealth);

        return new DriveSmartHealthReport(
            drive,
            overallHealth,
            summary,
            DateTimeOffset.Now,
            attributes,
            snapshot.Warnings);
    }

    private static DriveSmartTargetInfo BuildTargetInfo(DriveSmartDiskSnapshot disk, DriveSmartPhysicalDiskSnapshot? physicalDisk)
    {
        var interfaceType = FirstNonEmpty(
            physicalDisk is not null && !IsUnknownValue(physicalDisk.BusType) ? physicalDisk.BusType : string.Empty,
            disk.InterfaceType,
            "Unknown interface");
        var mediaType = FirstNonEmpty(
            physicalDisk is not null && !IsUnknownValue(physicalDisk.MediaType) ? physicalDisk.MediaType : string.Empty,
            disk.MediaType,
            "Unknown media");
        var size = FormatBytes(disk.Size);
        var model = FirstNonEmpty(disk.Model, disk.DeviceId, $"Disk {disk.Index}");
        var displayName = string.Join(
            "  |  ",
            new[]
            {
                model,
                $"Disk {disk.Index}",
                string.IsNullOrWhiteSpace(size) ? string.Empty : size,
                $"{interfaceType} / {mediaType}",
            }.Where(static value => !string.IsNullOrWhiteSpace(value)));

        return new DriveSmartTargetInfo(
            disk.DeviceId,
            displayName,
            model,
            size,
            interfaceType,
            mediaType,
            FirstNonEmpty(physicalDisk?.FirmwareVersion, disk.FirmwareRevision, "Unavailable"),
            FirstNonEmpty(disk.SerialNumber, physicalDisk?.SerialNumber, "Unavailable"));
    }

    private static string BuildOverallHealth(DriveSmartScanSnapshot snapshot, IReadOnlyList<DriveSmartAttributeEvaluation> attributes)
    {
        if (snapshot.SmartStatus?.PredictFailure == true
            || attributes.Any(static attribute => attribute.Severity == SmartAttributeSeverity.Critical))
        {
            return "Critical";
        }

        var physicalHealth = FirstNonEmpty(snapshot.PhysicalDisk?.HealthStatus, snapshot.Drive.Status);
        if (physicalHealth.Contains("Unhealthy", StringComparison.OrdinalIgnoreCase) ||
            physicalHealth.Contains("Error", StringComparison.OrdinalIgnoreCase))
        {
            return "Critical";
        }

        if (physicalHealth.Contains("Warning", StringComparison.OrdinalIgnoreCase) ||
            physicalHealth.Contains("Degraded", StringComparison.OrdinalIgnoreCase) ||
            physicalHealth.Contains("Stressed", StringComparison.OrdinalIgnoreCase))
        {
            return "Warning";
        }

        if (attributes.Any(static attribute => attribute.Severity == SmartAttributeSeverity.Warning))
        {
            return "Warning";
        }

        if (physicalHealth.Contains("Healthy", StringComparison.OrdinalIgnoreCase) ||
            physicalHealth.Contains("OK", StringComparison.OrdinalIgnoreCase))
        {
            return "Healthy";
        }

        return attributes.Count > 0 || snapshot.SmartStatus is not null
            ? "Healthy"
            : "Unknown";
    }

    private static string BuildSummary(DriveSmartScanSnapshot snapshot, IReadOnlyList<DriveSmartAttributeEvaluation> attributes, string overallHealth)
    {
        if (snapshot.SmartStatus?.PredictFailure == true)
        {
            return "SMART predicted a failure for this drive.";
        }

        var notableIssues = attributes
            .Where(static attribute => attribute.Severity is SmartAttributeSeverity.Warning or SmartAttributeSeverity.Critical)
            .OrderByDescending(static attribute => attribute.Severity)
            .ToArray();
        if (notableIssues.Length > 0)
        {
            var uniqueMessages = notableIssues
                .Select(static attribute => attribute.SummaryNote)
                .Where(static note => !string.IsNullOrWhiteSpace(note))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(2)
                .ToArray();
            if (uniqueMessages.Length > 0)
            {
                var remainingIssueCount = notableIssues.Length - uniqueMessages.Length;
                var suffix = remainingIssueCount > 0
                    ? $" Plus {remainingIssueCount} more SMART warning{(remainingIssueCount == 1 ? string.Empty : "s")}."
                    : string.Empty;
                return $"Overall health: {overallHealth}. {string.Join(" ", uniqueMessages)}{suffix}";
            }
        }

        if (!string.IsNullOrWhiteSpace(snapshot.PhysicalDisk?.HealthStatus)
            && !snapshot.PhysicalDisk.HealthStatus.Equals("Healthy", StringComparison.OrdinalIgnoreCase))
        {
            return $"Overall health: {overallHealth}. Windows reported this drive as {snapshot.PhysicalDisk.HealthStatus}.";
        }

        if (attributes.Count > 0)
        {
            return $"Overall health: {overallHealth}. Read {attributes.Count} SMART attribute{(attributes.Count == 1 ? string.Empty : "s")}.";
        }

        if (!string.IsNullOrWhiteSpace(snapshot.PhysicalDisk?.HealthStatus))
        {
            return $"Overall health: {overallHealth}. Windows reported physical drive health, but raw SMART attributes were unavailable.";
        }

        return $"Overall health: {overallHealth}. Raw SMART attributes were unavailable for this drive on this PC.";
    }

    private static IReadOnlyList<DriveSmartAttributeEvaluation> BuildSmartAttributes(byte[]? vendorSpecificData, byte[]? thresholdData)
    {
        if (vendorSpecificData is null || vendorSpecificData.Length < 14)
        {
            return [];
        }

        var thresholds = ParseThresholds(thresholdData);
        List<DriveSmartAttributeEvaluation> attributes = [];

        for (var offset = 2; offset + 11 < vendorSpecificData.Length; offset += 12)
        {
            var id = vendorSpecificData[offset];
            if (id == 0)
            {
                continue;
            }

            var current = vendorSpecificData[offset + 3];
            var rawBytes = vendorSpecificData[(offset + 5)..(offset + 11)];
            thresholds.TryGetValue(id, out var threshold);
            attributes.Add(BuildSmartAttributeEvaluation(id, current, threshold, rawBytes));
        }

        return attributes
            .OrderBy(static attribute => attribute.Byte, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static Dictionary<byte, byte> ParseThresholds(byte[]? thresholdData)
    {
        Dictionary<byte, byte> thresholds = [];
        if (thresholdData is null || thresholdData.Length < 14)
        {
            return thresholds;
        }

        for (var offset = 2; offset + 11 < thresholdData.Length; offset += 12)
        {
            var id = thresholdData[offset];
            if (id == 0)
            {
                continue;
            }

            thresholds[id] = thresholdData[offset + 1];
        }

        return thresholds;
    }

    private static DriveSmartAttributeEvaluation BuildSmartAttributeEvaluation(byte id, byte currentValue, byte thresholdValue, byte[] rawBytes)
    {
        var description = ResolveAttributeDescription(id);
        var rawValue = ParseRawValue(rawBytes);
        var rawData = FormatRawData(rawValue);

        if (thresholdValue > 0 && currentValue <= thresholdValue)
        {
            return new DriveSmartAttributeEvaluation(
                id.ToString("X2"),
                "Critical",
                description,
                rawData,
                SmartAttributeSeverity.Critical,
                $"{description} crossed its SMART threshold.");
        }

        return id switch
        {
            0x05 or 0xC4 or 0xC5 or 0xC6 or 0xBB => BuildCountSensitiveEvaluation(id, description, rawData, rawValue),
            0xC7 => rawValue == 0
                ? new DriveSmartAttributeEvaluation(id.ToString("X2"), "OK", description, rawData, SmartAttributeSeverity.Ok, $"{description} is clean.")
                : new DriveSmartAttributeEvaluation(id.ToString("X2"), "Warning", description, rawData, SmartAttributeSeverity.Warning, "Interface CRC errors were reported. Cabling or the SATA/USB bridge may be unstable."),
            0xE7 or 0xE8 or 0xE9 => BuildRemainingLifeEvaluation(id, description, rawData, rawValue, currentValue),
            0xBE or 0xC2 => BuildTemperatureEvaluation(id, description, rawData, rawValue, rawBytes),
            _ => thresholdValue > 0
                ? new DriveSmartAttributeEvaluation(id.ToString("X2"), "OK", description, rawData, SmartAttributeSeverity.Ok, $"{description} is above its SMART threshold.")
                : new DriveSmartAttributeEvaluation(id.ToString("X2"), "Info", description, rawData, SmartAttributeSeverity.Info, string.Empty),
        };
    }

    private static DriveSmartAttributeEvaluation BuildCountSensitiveEvaluation(byte id, string description, string rawData, ulong rawValue)
    {
        var status = rawValue switch
        {
            0 => "OK",
            < 10 => "Warning",
            _ => "Critical",
        };
        var severity = rawValue switch
        {
            0 => SmartAttributeSeverity.Ok,
            < 10 => SmartAttributeSeverity.Warning,
            _ => SmartAttributeSeverity.Critical,
        };
        var note = rawValue switch
        {
            0 => $"{description} is clear.",
            < 10 => $"{description} is non-zero and worth watching.",
            _ => $"{description} is elevated and points to real media or I/O trouble.",
        };

        return new DriveSmartAttributeEvaluation(id.ToString("X2"), status, description, rawData, severity, note);
    }

    private static DriveSmartAttributeEvaluation BuildRemainingLifeEvaluation(
        byte id,
        string description,
        string rawData,
        ulong rawValue,
        byte currentValue)
    {
        var remainingLife = GetPercentLikeValue(currentValue, rawValue);
        if (remainingLife is null)
        {
            return new DriveSmartAttributeEvaluation(id.ToString("X2"), "Info", description, rawData, SmartAttributeSeverity.Info, string.Empty);
        }

        var summaryLabel = id == 0xE8
            ? "Available reserved space"
            : "Remaining SSD life";
        if (remainingLife <= SmartRemainingLifeCriticalPercent)
        {
            return new DriveSmartAttributeEvaluation(
                id.ToString("X2"),
                "Critical",
                description,
                rawData,
                SmartAttributeSeverity.Critical,
                $"{summaryLabel} is very low at about {remainingLife}%.");
        }

        if (remainingLife <= SmartRemainingLifeWarningPercent)
        {
            return new DriveSmartAttributeEvaluation(
                id.ToString("X2"),
                "Warning",
                description,
                rawData,
                SmartAttributeSeverity.Warning,
                $"{summaryLabel} is getting low at about {remainingLife}%.");
        }

        return new DriveSmartAttributeEvaluation(
            id.ToString("X2"),
            "OK",
            description,
            rawData,
            SmartAttributeSeverity.Ok,
            $"{summaryLabel} still looks healthy at about {remainingLife}%.");
    }

    private static DriveSmartAttributeEvaluation BuildTemperatureEvaluation(
        byte id,
        string description,
        string rawData,
        ulong rawValue,
        byte[] rawBytes)
    {
        var temperature = ParseTemperatureCelsius(rawBytes, rawValue);
        if (temperature >= SmartTemperatureCriticalCelsius)
        {
            return new DriveSmartAttributeEvaluation(
                id.ToString("X2"),
                "Critical",
                description,
                rawData,
                SmartAttributeSeverity.Critical,
                $"Drive temperature reached {temperature} C.");
        }

        if (temperature >= SmartTemperatureWarningCelsius)
        {
            return new DriveSmartAttributeEvaluation(
                id.ToString("X2"),
                "Warning",
                description,
                rawData,
                SmartAttributeSeverity.Warning,
                $"Drive temperature is high at {temperature} C.");
        }

        if (temperature > 0)
        {
            return new DriveSmartAttributeEvaluation(
                id.ToString("X2"),
                "OK",
                description,
                rawData,
                SmartAttributeSeverity.Ok,
                $"Drive temperature is {temperature} C.");
        }

        return new DriveSmartAttributeEvaluation(id.ToString("X2"), "Info", description, rawData, SmartAttributeSeverity.Info, string.Empty);
    }

    private static string ResolveAttributeDescription(byte id) =>
        SmartAttributeDescriptions.TryGetValue(id, out var description)
            ? description
            : "Vendor-specific attribute";

    private static ulong ParseRawValue(byte[] rawBytes)
    {
        ulong rawValue = 0;
        for (var index = 0; index < rawBytes.Length; index++)
        {
            rawValue |= (ulong)rawBytes[index] << (index * 8);
        }

        return rawValue;
    }

    private static string FormatRawData(ulong rawValue) => $"{rawValue} (0x{rawValue:X12})";

    private static int? GetPercentLikeValue(byte currentValue, ulong rawValue)
    {
        List<int> candidates = [];
        if (currentValue is > 0 and <= 100)
        {
            candidates.Add(currentValue);
        }

        if (rawValue is > 0 and <= 100)
        {
            candidates.Add((int)rawValue);
        }

        return candidates.Count == 0 ? null : candidates.Min();
    }

    private static int ParseTemperatureCelsius(byte[] rawBytes, ulong rawValue)
    {
        if (rawBytes.Length > 0 && rawBytes[0] is > 0 and < 150)
        {
            return rawBytes[0];
        }

        if (rawValue is > 0 and < 150)
        {
            return (int)rawValue;
        }

        return 0;
    }

    private static IReadOnlyList<DriveSmartDiskSnapshot> ReadDiskInventory(ICollection<string> warnings) =>
        TryReadMany(
            DefaultManagementScope,
            "SELECT Index, DeviceID, Model, Size, InterfaceType, MediaType, Status, SerialNumber, FirmwareRevision, PNPDeviceID FROM Win32_DiskDrive WHERE DeviceID IS NOT NULL OR Model IS NOT NULL",
            item => new DriveSmartDiskSnapshot(
                GetInt(item, "Index"),
                GetString(item, "DeviceID"),
                GetString(item, "Model", "Unknown drive"),
                GetUInt64(item, "Size"),
                GetString(item, "InterfaceType", "Unknown interface"),
                NormalizeWin32MediaType(GetString(item, "MediaType", "Unknown media")),
                GetString(item, "Status"),
                GetString(item, "SerialNumber"),
                GetString(item, "FirmwareRevision"),
                GetString(item, "PNPDeviceID")),
            warnings,
            "SMART drive inventory")
        .OrderBy(static disk => disk.Index)
        .ThenBy(static disk => disk.Model, StringComparer.OrdinalIgnoreCase)
        .ToArray();

    private static DriveSmartScanSnapshot ReadDriveScan(string deviceId)
    {
        List<string> warnings = [];
        var diskSnapshots = ReadDiskInventory(warnings);
        var drive = diskSnapshots.FirstOrDefault(
            disk => disk.DeviceId.Equals(deviceId, StringComparison.OrdinalIgnoreCase));

        if (drive is null)
        {
            throw new InvalidOperationException($"Drive '{deviceId}' could not be found.");
        }

        var physicalDisks = TryReadMany(
            StorageManagementScope,
            "SELECT FriendlyName, Size, BusType, MediaType, HealthStatus, SerialNumber, FirmwareVersion, DeviceId FROM MSFT_PhysicalDisk",
            item => new DriveSmartPhysicalDiskSnapshot(
                GetString(item, "FriendlyName"),
                GetUInt64(item, "Size"),
                MapBusType(GetInt(item, "BusType")),
                MapStorageMediaType(GetInt(item, "MediaType")),
                MapStorageHealthStatus(GetInt(item, "HealthStatus")),
                GetString(item, "SerialNumber"),
                GetString(item, "FirmwareVersion"),
                GetString(item, "DeviceId")),
            warnings,
            "SMART physical disk health");

        var smartStatuses = TryReadMany(
            WmiManagementScope,
            "SELECT InstanceName, PredictFailure, Reason FROM MSStorageDriver_FailurePredictStatus",
            item => new DriveSmartStatusSnapshot(
                GetString(item, "InstanceName"),
                GetBool(item, "PredictFailure"),
                GetString(item, "Reason")),
            warnings,
            "SMART status");

        var smartData = TryReadMany(
            WmiManagementScope,
            "SELECT InstanceName, VendorSpecific FROM MSStorageDriver_FailurePredictData",
            item => new DriveSmartDataSnapshot(
                GetString(item, "InstanceName"),
                GetByteArray(item, "VendorSpecific")),
            warnings,
            "SMART data");

        var smartThresholds = TryReadMany(
            WmiManagementScope,
            "SELECT InstanceName, VendorSpecific FROM MSStorageDriver_FailurePredictThresholds",
            item => new DriveSmartThresholdSnapshot(
                GetString(item, "InstanceName"),
                GetByteArray(item, "VendorSpecific")),
            warnings,
            "SMART thresholds");

        return new DriveSmartScanSnapshot(
            drive,
            MatchPhysicalDisk(drive, physicalDisks),
            MatchSmartStatus(drive, smartStatuses),
            MatchSmartData(drive, smartData),
            MatchSmartThresholds(drive, smartThresholds),
            warnings);
    }

    private static DriveSmartPhysicalDiskSnapshot? MatchPhysicalDisk(DriveSmartDiskSnapshot snapshot, IReadOnlyList<DriveSmartPhysicalDiskSnapshot> physicalDisks)
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

    private static DriveSmartStatusSnapshot? MatchSmartStatus(DriveSmartDiskSnapshot snapshot, IReadOnlyList<DriveSmartStatusSnapshot> smartStatuses) =>
        MatchSmartSnapshot(snapshot, smartStatuses, static item => item.InstanceName);

    private static DriveSmartDataSnapshot? MatchSmartData(DriveSmartDiskSnapshot snapshot, IReadOnlyList<DriveSmartDataSnapshot> smartData) =>
        MatchSmartSnapshot(snapshot, smartData, static item => item.InstanceName);

    private static DriveSmartThresholdSnapshot? MatchSmartThresholds(DriveSmartDiskSnapshot snapshot, IReadOnlyList<DriveSmartThresholdSnapshot> smartThresholds) =>
        MatchSmartSnapshot(snapshot, smartThresholds, static item => item.InstanceName);

    private static T? MatchSmartSnapshot<T>(
        DriveSmartDiskSnapshot snapshot,
        IReadOnlyList<T> candidates,
        Func<T, string> instanceNameSelector)
        where T : class
    {
        var candidateTokens = GetMatchTokens(snapshot.Model, snapshot.SerialNumber, snapshot.PnpDeviceId).ToArray();
        if (candidateTokens.Length == 0)
        {
            return null;
        }

        return candidates
            .Select(
                candidate => new
                {
                    Candidate = candidate,
                    Score = candidateTokens.Max(token => ScoreMatchToken(token, instanceNameSelector(candidate))),
                })
            .Where(static item => item.Score > 0)
            .OrderByDescending(static item => item.Score)
            .Select(static item => item.Candidate)
            .FirstOrDefault();
    }

    private static int ScorePhysicalDiskMatch(DriveSmartDiskSnapshot snapshot, DriveSmartPhysicalDiskSnapshot physicalDisk)
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

        if (normalizedCandidate.Contains(normalizedToken, StringComparison.OrdinalIgnoreCase) ||
            normalizedToken.Contains(normalizedCandidate, StringComparison.OrdinalIgnoreCase))
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
            return $"{context}: Access denied. Start MultiTool as administrator to access this drive data on this PC.";
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
        return value is null ? 0 : Convert.ToInt32(value);
    }

    private static ulong GetUInt64(ManagementBaseObject source, string propertyName)
    {
        var value = GetValue(source, propertyName);
        return value is null ? 0UL : Convert.ToUInt64(value);
    }

    private static bool GetBool(ManagementBaseObject source, string propertyName)
    {
        var value = GetValue(source, propertyName);
        return value is not null && Convert.ToBoolean(value);
    }

    private static byte[] GetByteArray(ManagementBaseObject source, string propertyName)
    {
        var value = GetValue(source, propertyName);
        return value switch
        {
            byte[] bytes => bytes,
            Array array => array.Cast<object?>().Where(static item => item is not null).Select(static item => Convert.ToByte(item)).ToArray(),
            _ => [],
        };
    }

    private static string MapStorageHealthStatus(int healthStatus) =>
        healthStatus switch
        {
            1 => "Healthy",
            2 => "Warning",
            3 => "Unhealthy",
            _ => string.Empty,
        };

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

        if (mediaType.Contains("SSD", StringComparison.OrdinalIgnoreCase) ||
            mediaType.Contains("Solid State", StringComparison.OrdinalIgnoreCase))
        {
            return "SSD";
        }

        if (mediaType.Contains("Fixed hard disk", StringComparison.OrdinalIgnoreCase) ||
            mediaType.Contains("Hard Disk", StringComparison.OrdinalIgnoreCase) ||
            mediaType.Contains("HDD", StringComparison.OrdinalIgnoreCase))
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
}

internal sealed record DriveSmartDiskSnapshot(
    int Index,
    string DeviceId,
    string Model,
    ulong Size,
    string InterfaceType,
    string MediaType,
    string Status,
    string SerialNumber,
    string FirmwareRevision,
    string PnpDeviceId);

internal sealed record DriveSmartPhysicalDiskSnapshot(
    string FriendlyName,
    ulong Size,
    string BusType,
    string MediaType,
    string HealthStatus,
    string SerialNumber,
    string FirmwareVersion,
    string DeviceId);

internal sealed record DriveSmartStatusSnapshot(
    string InstanceName,
    bool PredictFailure,
    string Reason);

internal sealed record DriveSmartDataSnapshot(
    string InstanceName,
    byte[] VendorSpecific);

internal sealed record DriveSmartThresholdSnapshot(
    string InstanceName,
    byte[] VendorSpecific);

internal sealed record DriveSmartAttributeEvaluation(
    string Byte,
    string Status,
    string Description,
    string RawData,
    SmartAttributeSeverity Severity,
    string SummaryNote);

internal enum SmartAttributeSeverity
{
    Info = 0,
    Ok = 1,
    Warning = 2,
    Critical = 3,
}

internal sealed record DriveSmartScanSnapshot(
    DriveSmartDiskSnapshot Drive,
    DriveSmartPhysicalDiskSnapshot? PhysicalDisk,
    DriveSmartStatusSnapshot? SmartStatus,
    DriveSmartDataSnapshot? SmartData,
    DriveSmartThresholdSnapshot? SmartThresholds,
    IReadOnlyList<string> Warnings);
