using System.Management;
using System.Reflection;
using System.IO;
using Microsoft.Win32;
using AutoClicker.Core.Models;
using AutoClicker.Core.Services;

namespace AutoClicker.Infrastructure.Windows.Tools;

public delegate HardwareInventoryReport HardwareInventoryReader();

public sealed class WindowsHardwareInventoryService : IHardwareInventoryService
{
    private const string DefaultManagementScope = @"\\.\root\cimv2";
    private const string StorageManagementScope = @"\\.\root\Microsoft\Windows\Storage";
    private const string WmiManagementScope = @"\\.\root\WMI";

    private readonly HardwareInventoryReader reportReader;

    public WindowsHardwareInventoryService()
        : this(BuildReport)
    {
    }

    public WindowsHardwareInventoryService(HardwareInventoryReader reportReader)
    {
        this.reportReader = reportReader;
    }

    public Task<HardwareInventoryReport> GetReportAsync(CancellationToken cancellationToken = default) =>
        Task.Run(() => reportReader(), cancellationToken);

    private static HardwareInventoryReport BuildReport()
    {
        var warnings = new List<string>();

        var systemInfo = TryReadSingle(
            "SELECT Name, Manufacturer, Model, TotalPhysicalMemory FROM Win32_ComputerSystem",
            item => new
            {
                Name = GetString(item, "Name", Environment.MachineName),
                Manufacturer = GetString(item, "Manufacturer", "Unknown manufacturer"),
                Model = GetString(item, "Model", "Unknown model"),
                TotalPhysicalMemory = GetUInt64(item, "TotalPhysicalMemory"),
            },
            new
            {
                Name = Environment.MachineName,
                Manufacturer = "Unknown manufacturer",
                Model = "Unknown model",
                TotalPhysicalMemory = 0UL,
            },
            warnings,
            "Computer system");

        var diskSnapshots = ReadDiskSnapshots(warnings);
        var processorSummary = BuildProcessorSummary(warnings);
        var memorySummary = BuildMemorySummary(systemInfo.TotalPhysicalMemory, warnings);
        var operatingSystemSummary = BuildOperatingSystemSummary(warnings);
        var motherboardSummary = BuildMotherboardSummary(warnings);
        var biosSummary = BuildBiosSummary(warnings);
        var graphicsAdapters = BuildGraphicsAdapters(warnings);
        var storageDrives = BuildStorageDrives(diskSnapshots, warnings);
        var storagePartitions = BuildStoragePartitions(diskSnapshots, warnings);
        var sensors = BuildSensors(warnings);
        var pciDevices = BuildPciDevices(warnings);
        var raidDetails = BuildRaidDetails(pciDevices, warnings);
        var healthSummary = BuildHealthSummary(storageDrives, pciDevices, sensors, warnings);

        return new HardwareInventoryReport(
            $"{systemInfo.Name}  |  {systemInfo.Manufacturer} {systemInfo.Model}".Trim(),
            healthSummary,
            operatingSystemSummary,
            processorSummary,
            memorySummary,
            motherboardSummary,
            biosSummary,
            graphicsAdapters,
            storageDrives,
            storagePartitions,
            sensors,
            pciDevices,
            raidDetails,
            warnings);
    }

    private static string BuildProcessorSummary(ICollection<string> warnings)
    {
        var processors = TryReadMany(
            "SELECT Name, NumberOfCores, NumberOfLogicalProcessors FROM Win32_Processor",
            item => new
            {
                Name = GetString(item, "Name", "Unknown CPU"),
                Cores = GetInt(item, "NumberOfCores"),
                Threads = GetInt(item, "NumberOfLogicalProcessors"),
            },
            warnings,
            "Processor");

        if (processors.Count == 0)
        {
            return "Processor details unavailable.";
        }

        return string.Join(
            "  |  ",
            processors.Select(
                processor =>
                {
                    var topology = processor.Cores > 0 || processor.Threads > 0
                        ? $" ({processor.Cores} cores / {processor.Threads} threads)"
                        : string.Empty;
                    return $"{processor.Name}{topology}";
                }));
    }

    private static string BuildMemorySummary(ulong totalPhysicalMemory, ICollection<string> warnings)
    {
        var modules = TryReadMany(
            "SELECT Capacity, Speed FROM Win32_PhysicalMemory WHERE Capacity IS NOT NULL",
            item => new
            {
                Capacity = GetUInt64(item, "Capacity"),
                Speed = GetInt(item, "Speed"),
            },
            warnings,
            "Physical memory");

        var total = totalPhysicalMemory > 0
            ? totalPhysicalMemory
            : modules.Aggregate(0UL, (sum, module) => sum + module.Capacity);

        if (total == 0 && modules.Count == 0)
        {
            return "Memory details unavailable.";
        }

        var speed = modules
            .Select(static module => module.Speed)
            .Where(static speed => speed > 0)
            .DefaultIfEmpty()
            .Max();

        var speedText = speed > 0 ? $" at up to {speed} MHz" : string.Empty;
        return $"{FormatBytes(total)} installed across {modules.Count} module{(modules.Count == 1 ? string.Empty : "s")}{speedText}.";
    }

    private static string BuildOperatingSystemSummary(ICollection<string> warnings)
    {
        var os = TryReadSingle(
            "SELECT Caption, Version, BuildNumber FROM Win32_OperatingSystem",
            item => new
            {
                Caption = GetString(item, "Caption", "Windows"),
                Version = GetString(item, "Version"),
                BuildNumber = GetString(item, "BuildNumber"),
            },
            new
            {
                Caption = "Windows",
                Version = string.Empty,
                BuildNumber = string.Empty,
            },
            warnings,
            "Operating system");

        var details = string.Join(
            "  |  ",
            new[]
            {
                os.Caption,
                string.IsNullOrWhiteSpace(os.Version) ? string.Empty : $"Version {os.Version}",
                string.IsNullOrWhiteSpace(os.BuildNumber) ? string.Empty : $"Build {os.BuildNumber}",
            }.Where(static value => !string.IsNullOrWhiteSpace(value)));

        return string.IsNullOrWhiteSpace(details)
            ? "Windows details unavailable."
            : details;
    }

    private static string BuildMotherboardSummary(ICollection<string> warnings)
    {
        var board = TryReadSingle(
            "SELECT Manufacturer, Product FROM Win32_BaseBoard",
            item => new
            {
                Manufacturer = GetString(item, "Manufacturer", "Unknown manufacturer"),
                Product = GetString(item, "Product", "Unknown board"),
            },
            new
            {
                Manufacturer = "Unknown manufacturer",
                Product = "Unknown board",
            },
            warnings,
            "Motherboard");

        return $"{board.Manufacturer} {board.Product}".Trim();
    }

    private static string BuildBiosSummary(ICollection<string> warnings)
    {
        var bios = TryReadSingle(
            "SELECT SMBIOSBIOSVersion, ReleaseDate FROM Win32_BIOS",
            item => new
            {
                Version = GetString(item, "SMBIOSBIOSVersion", "Unknown BIOS"),
                ReleaseDate = GetDateString(item, "ReleaseDate"),
            },
            new
            {
                Version = "Unknown BIOS",
                ReleaseDate = string.Empty,
            },
            warnings,
            "BIOS");

        return string.IsNullOrWhiteSpace(bios.ReleaseDate)
            ? bios.Version
            : $"{bios.Version}  |  Released {bios.ReleaseDate}";
    }

    private static IReadOnlyList<HardwareDisplayAdapterInfo> BuildGraphicsAdapters(ICollection<string> warnings)
    {
        var adapters = TryReadMany(
            "SELECT Name, DriverVersion, AdapterRAM, PNPDeviceID FROM Win32_VideoController WHERE Name IS NOT NULL",
            item => new
            {
                Name = GetString(item, "Name", "Unknown GPU"),
                DriverVersion = GetString(item, "DriverVersion", "Unknown driver"),
                AdapterRam = GetUInt64(item, "AdapterRAM"),
                PnpDeviceId = GetString(item, "PNPDeviceID"),
            },
            warnings,
            "Graphics adapter");

        if (adapters.Count == 0)
        {
            return [new HardwareDisplayAdapterInfo("No graphics adapters detected.", string.Empty, string.Empty)];
        }

        var resolvedAdapters = adapters.Select(
            adapter => new HardwareDisplayAdapterInfo(
                adapter.Name,
                adapter.DriverVersion,
                ResolveGraphicsMemory(adapter.AdapterRam, adapter.PnpDeviceId)))
            .ToArray();

        return resolvedAdapters.Length == 0
            ? [new HardwareDisplayAdapterInfo("No graphics adapters detected.", string.Empty, string.Empty)]
            : resolvedAdapters;
    }

    private static IReadOnlyList<DiskSnapshot> ReadDiskSnapshots(ICollection<string> warnings)
    {
        var disks = TryReadMany(
            "SELECT Index, DeviceID, Model, Size, InterfaceType, MediaType, Status, SerialNumber, FirmwareRevision, PNPDeviceID FROM Win32_DiskDrive WHERE DeviceID IS NOT NULL OR Model IS NOT NULL",
            item => new DiskSnapshot(
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
            "Storage drive");

        return disks
            .OrderBy(static disk => disk.Index)
            .ThenBy(static disk => disk.Model, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IReadOnlyList<HardwareStorageDriveInfo> BuildStorageDrives(
        IReadOnlyList<DiskSnapshot> diskSnapshots,
        ICollection<string> warnings)
    {
        var physicalDisks = TryReadMany(
            StorageManagementScope,
            "SELECT FriendlyName, Size, BusType, MediaType, HealthStatus, OperationalStatus, SerialNumber, FirmwareVersion, DeviceId FROM MSFT_PhysicalDisk",
            item => new PhysicalDiskSnapshot(
                GetString(item, "FriendlyName"),
                GetUInt64(item, "Size"),
                MapBusType(GetInt(item, "BusType")),
                MapStorageMediaType(GetInt(item, "MediaType")),
                MapStorageHealthStatus(GetInt(item, "HealthStatus")),
                MapOperationalStatuses(GetUInt16Array(item, "OperationalStatus")),
                GetString(item, "SerialNumber"),
                GetString(item, "FirmwareVersion"),
                GetString(item, "DeviceId")),
            warnings,
            "Physical disk health");

        var smartStatuses = TryReadMany(
            WmiManagementScope,
            "SELECT InstanceName, PredictFailure, Reason FROM MSStorageDriver_FailurePredictStatus",
            item => new SmartStatusSnapshot(
                GetString(item, "InstanceName"),
                GetBool(item, "PredictFailure"),
                GetString(item, "Reason")),
            warnings,
            "SMART status");

        var drives = diskSnapshots.Select(
                snapshot =>
                {
                    var physicalDisk = MatchPhysicalDisk(snapshot, physicalDisks);
                    var smartStatus = MatchSmartStatus(snapshot, smartStatuses);

                    var interfaceType = FirstNonEmpty(
                        physicalDisk?.BusType is not null && !IsUnknownValue(physicalDisk.BusType) ? physicalDisk.BusType : string.Empty,
                        snapshot.InterfaceType,
                        "Unknown interface");

                    var mediaType = FirstNonEmpty(
                        physicalDisk?.MediaType is not null && !IsUnknownValue(physicalDisk.MediaType) ? physicalDisk.MediaType : string.Empty,
                        snapshot.MediaType,
                        "Unknown media");

                    var healthStatus = FirstNonEmpty(
                        physicalDisk?.HealthStatus is not null && !IsUnknownValue(physicalDisk.HealthStatus) ? physicalDisk.HealthStatus : string.Empty,
                        snapshot.Status,
                        "Unknown");

                    var smartStatusText = smartStatus is null
                        ? BuildSmartFallbackStatus(snapshot, physicalDisk)
                        : smartStatus.PredictFailure
                            ? string.IsNullOrWhiteSpace(smartStatus.Reason)
                                ? "Predicted failure"
                                : $"Predicted failure ({smartStatus.Reason})"
                            : "Healthy";

                    var notes = string.Join(
                        "  |  ",
                        new[]
                        {
                            physicalDisk?.OperationalStatus,
                            physicalDisk is not null && string.IsNullOrWhiteSpace(physicalDisk.HealthStatus)
                                ? "Windows reported this disk without a health state."
                                : string.Empty,
                        }.Where(static value => !string.IsNullOrWhiteSpace(value)));

                    return new HardwareStorageDriveInfo(
                        FirstNonEmpty(snapshot.Model, snapshot.DeviceId, "Unknown drive"),
                        FormatBytes(snapshot.Size),
                        interfaceType,
                        mediaType,
                        healthStatus,
                        smartStatusText,
                        FirstNonEmpty(physicalDisk?.FirmwareVersion, snapshot.FirmwareRevision, "Unavailable"),
                        FirstNonEmpty(snapshot.SerialNumber, physicalDisk?.SerialNumber, "Unavailable"),
                        notes);
                })
            .ToArray();

        return drives.Length == 0
            ? [new HardwareStorageDriveInfo("No storage drives detected.", string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty)]
            : drives;
    }

    private static IReadOnlyList<HardwarePartitionInfo> BuildStoragePartitions(
        IReadOnlyList<DiskSnapshot> diskSnapshots,
        ICollection<string> warnings)
    {
        var diskLookup = diskSnapshots.ToDictionary(static disk => disk.Index);
        var partitions = TryReadMany(
            "SELECT DiskIndex, DeviceID, Name, Size, Type, BootPartition, PrimaryPartition, Status FROM Win32_DiskPartition",
            item => new PartitionSnapshot(
                GetInt(item, "DiskIndex"),
                GetString(item, "DeviceID"),
                GetString(item, "Name", GetString(item, "DeviceID", "Unknown partition")),
                GetUInt64(item, "Size"),
                GetString(item, "Type", "Unknown type"),
                GetBool(item, "BootPartition"),
                GetBool(item, "PrimaryPartition"),
                GetString(item, "Status")),
            warnings,
            "Disk partition");

        var logicalDisks = TryReadMany(
            "SELECT DeviceID, FileSystem, FreeSpace, VolumeName FROM Win32_LogicalDisk WHERE DeviceID IS NOT NULL",
            item => new LogicalDiskSnapshot(
                GetString(item, "DeviceID"),
                GetString(item, "FileSystem"),
                GetUInt64(item, "FreeSpace"),
                GetString(item, "VolumeName")),
            warnings,
            "Logical disk")
            .Where(static disk => !string.IsNullOrWhiteSpace(disk.DeviceId))
            .ToDictionary(static disk => disk.DeviceId, StringComparer.OrdinalIgnoreCase);

        var partitionLinks = TryReadMany(
            "SELECT Antecedent, Dependent FROM Win32_LogicalDiskToPartition",
            item => new PartitionLinkSnapshot(
                ParseEmbeddedPropertyValue(GetString(item, "Antecedent"), "DeviceID"),
                ParseEmbeddedPropertyValue(GetString(item, "Dependent"), "DeviceID")),
            warnings,
            "Partition map");

        var partitionVolumeLookup = partitionLinks
            .Where(static link => !string.IsNullOrWhiteSpace(link.PartitionDeviceId) && !string.IsNullOrWhiteSpace(link.LogicalDiskId))
            .GroupBy(static link => link.PartitionDeviceId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => group.Select(static link => link.LogicalDiskId).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
                StringComparer.OrdinalIgnoreCase);

        return partitions
            .OrderBy(static partition => partition.DiskIndex)
            .ThenBy(static partition => partition.DeviceId, StringComparer.OrdinalIgnoreCase)
                    .Select(
                        partition =>
                        {
                    var diskName = diskLookup.TryGetValue(partition.DiskIndex, out var diskSnapshot)
                        ? FirstNonEmpty(diskSnapshot.Model, diskSnapshot.DeviceId, $"Disk {partition.DiskIndex}")
                        : $"Disk {partition.DiskIndex}";

                    var volumeDetails = partitionVolumeLookup.TryGetValue(partition.DeviceId, out var logicalDiskIds)
                        ? logicalDiskIds
                            .Select(id => logicalDisks.TryGetValue(id, out var logicalDisk) ? logicalDisk : null)
                            .Where(static logicalDisk => logicalDisk is not null)
                            .Cast<LogicalDiskSnapshot>()
                            .ToArray()
                        : [];

                    var volumeText = volumeDetails.Length == 0
                        ? "Not mounted"
                        : string.Join(", ", volumeDetails.Select(BuildVolumeLabel));

                    var fileSystemText = volumeDetails.Length == 0
                        ? "Unavailable"
                        : string.Join(", ", volumeDetails.Select(static logicalDisk => FirstNonEmpty(logicalDisk.FileSystem, "Unknown")));

                    var freeSpaceText = volumeDetails.Length == 0
                        ? "Unavailable"
                        : string.Join(", ", volumeDetails.Select(static logicalDisk => logicalDisk.FreeSpace > 0 ? FormatBytes(logicalDisk.FreeSpace) : "Unknown"));

                    var partitionType = BuildPartitionTypeLabel(partition, volumeDetails.Length > 0);

                    var status = string.Join(
                        "  |  ",
                        new[]
                        {
                            partition.BootPartition ? "Boot" : string.Empty,
                            partition.PrimaryPartition ? "Primary" : string.Empty,
                            partition.Status,
                        }.Where(static value => !string.IsNullOrWhiteSpace(value)));

                    return new HardwarePartitionInfo(
                        diskName,
                        FirstNonEmpty(partition.Name, partition.DeviceId, "Unknown partition"),
                        FormatBytes(partition.Size),
                        partitionType,
                        volumeText,
                        fileSystemText,
                        freeSpaceText,
                            FirstNonEmpty(status, "Status unavailable"));
                        })
            .ToArray();
    }

    private static IReadOnlyList<HardwareSensorInfo> BuildSensors(ICollection<string> warnings)
    {
        var sensors = new List<HardwareSensorInfo>();

        var thermalZones = TryReadMany(
            WmiManagementScope,
            "SELECT InstanceName, CurrentTemperature FROM MSAcpi_ThermalZoneTemperature",
            item => new HardwareSensorInfo(
                "Temperature",
                FirstNonEmpty(GetString(item, "InstanceName"), "ACPI thermal zone"),
                FormatAcpiTemperature(GetUInt64(item, "CurrentTemperature")),
                "ACPI thermal zone",
                "Reported by Windows ACPI telemetry"),
            warnings,
            "Thermal sensor");

        sensors.AddRange(thermalZones.Where(static sensor => !string.IsNullOrWhiteSpace(sensor.CurrentValue)));

        var temperatureProbes = TryReadMany(
            "SELECT Name, CurrentReading, Status FROM Win32_TemperatureProbe",
            item => new HardwareSensorInfo(
                "Temperature",
                FirstNonEmpty(GetString(item, "Name"), "Temperature probe"),
                FormatRawSensorReading(GetInt(item, "CurrentReading")),
                "Win32_TemperatureProbe",
                FirstNonEmpty(GetString(item, "Status"), "Status unavailable")),
            warnings,
            "Temperature probe");

        sensors.AddRange(temperatureProbes.Where(static sensor => !string.IsNullOrWhiteSpace(sensor.CurrentValue)));

        var fans = TryReadMany(
            "SELECT Name, DesiredSpeed, VariableSpeed, Status FROM Win32_Fan",
            item => new HardwareSensorInfo(
                "Fan",
                FirstNonEmpty(GetString(item, "Name"), "System fan"),
                BuildFanReading(GetInt(item, "DesiredSpeed")),
                "Win32_Fan",
                string.Join(
                    "  |  ",
                    new[]
                    {
                        GetBool(item, "VariableSpeed") ? "Variable speed" : string.Empty,
                        GetString(item, "Status"),
                    }.Where(static value => !string.IsNullOrWhiteSpace(value)))),
            warnings,
            "Fan");

        sensors.AddRange(fans.Where(static sensor => !string.IsNullOrWhiteSpace(sensor.CurrentValue) || !string.IsNullOrWhiteSpace(sensor.Status)));

        var voltageProbes = TryReadMany(
            "SELECT Name, CurrentReading, Status FROM Win32_VoltageProbe",
            item => new HardwareSensorInfo(
                "Voltage",
                FirstNonEmpty(GetString(item, "Name"), "Voltage probe"),
                FormatRawSensorReading(GetInt(item, "CurrentReading")),
                "Win32_VoltageProbe",
                FirstNonEmpty(GetString(item, "Status"), "Status unavailable")),
            warnings,
            "Voltage probe");

        sensors.AddRange(voltageProbes.Where(static sensor => !string.IsNullOrWhiteSpace(sensor.CurrentValue)));

        sensors.AddRange(ReadLibreHardwareMonitorSensors(warnings));

        return sensors
            .DistinctBy(static sensor => $"{sensor.Category}|{sensor.Name}|{sensor.Source}", StringComparer.OrdinalIgnoreCase)
            .OrderBy(static sensor => sensor.Category, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static sensor => sensor.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IReadOnlyList<HardwarePciDeviceInfo> BuildPciDevices(ICollection<string> warnings)
    {
        var primaryDevices = TryReadMany(
            "SELECT Name, Manufacturer, PNPClass, Status, ConfigManagerErrorCode, PNPDeviceID FROM Win32_PnPEntity WHERE PNPDeviceID IS NOT NULL",
            item => new PciDeviceSnapshot(
                FirstNonEmpty(GetString(item, "Name"), "Unknown PCI device"),
                FirstNonEmpty(GetString(item, "PNPClass"), "Unclassified"),
                FirstNonEmpty(GetString(item, "Manufacturer"), "Unknown manufacturer"),
                "Location unavailable",
                BuildPciDeviceStatus(GetString(item, "Status"), GetInt(item, "ConfigManagerErrorCode")),
                GetString(item, "PNPDeviceID")),
            warnings,
            "PCI device");

        var fallbackDevices = TryReadMany(
            "SELECT DeviceName, Manufacturer, DeviceClass, DeviceID, Location, Status FROM Win32_PnPSignedDriver WHERE DeviceID IS NOT NULL",
            item => new PciDeviceSnapshot(
                FirstNonEmpty(GetString(item, "DeviceName"), "Unknown PCI device"),
                FirstNonEmpty(GetString(item, "DeviceClass"), "Unclassified"),
                FirstNonEmpty(GetString(item, "Manufacturer"), "Unknown manufacturer"),
                FirstNonEmpty(GetString(item, "Location"), "Location unavailable"),
                FirstNonEmpty(GetString(item, "Status"), "Status unavailable"),
                GetString(item, "DeviceID")),
            warnings,
            "PCI signed driver");

        var devices = primaryDevices
            .Concat(fallbackDevices)
            .Where(static device => IsPciDevice(device.PnpDeviceId))
            .GroupBy(static device => BuildPciUniquenessKey(device), StringComparer.OrdinalIgnoreCase)
            .Select(static group => group.Aggregate(MergePciDeviceSnapshots))
            .ToArray();

        return devices
            .OrderBy(static device => device.DeviceClass, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static device => device.Name, StringComparer.OrdinalIgnoreCase)
            .Select(
                static device => new HardwarePciDeviceInfo(
                    device.Name,
                    device.DeviceClass,
                    device.Manufacturer,
                    device.Location,
                    device.Status))
            .ToArray();
    }

    private static string BuildPciUniquenessKey(PciDeviceSnapshot device) =>
        FirstNonEmpty(device.PnpDeviceId, device.Name, device.DeviceClass, device.Manufacturer);

    private static PciDeviceSnapshot MergePciDeviceSnapshots(PciDeviceSnapshot left, PciDeviceSnapshot right) =>
        new(
            FirstNonEmpty(left.Name, right.Name, "Unknown PCI device"),
            FirstNonEmpty(left.DeviceClass, right.DeviceClass, "Unclassified"),
            FirstNonEmpty(left.Manufacturer, right.Manufacturer, "Unknown manufacturer"),
            PreferInformativeValue(left.Location, right.Location, "Location unavailable"),
            PreferInformativeValue(left.Status, right.Status, "Status unavailable"),
            FirstNonEmpty(left.PnpDeviceId, right.PnpDeviceId));

    private static string PreferInformativeValue(string left, string right, string unavailableValue)
    {
        var leftIsUnavailable = string.IsNullOrWhiteSpace(left) || left.Equals(unavailableValue, StringComparison.OrdinalIgnoreCase);
        var rightIsUnavailable = string.IsNullOrWhiteSpace(right) || right.Equals(unavailableValue, StringComparison.OrdinalIgnoreCase);

        if (!leftIsUnavailable)
        {
            return left;
        }

        if (!rightIsUnavailable)
        {
            return right;
        }

        return unavailableValue;
    }

    private static string BuildPciDeviceStatus(string wmiStatus, int configManagerErrorCode)
    {
        if (configManagerErrorCode == 0)
        {
            return "OK";
        }

        var mappedCode = configManagerErrorCode switch
        {
            10 => "Cannot start (Code 10)",
            12 => "Insufficient resources (Code 12)",
            14 => "Restart required (Code 14)",
            22 => "Disabled (Code 22)",
            28 => "Driver not installed (Code 28)",
            31 => "Device malfunction (Code 31)",
            43 => "Device stopped (Code 43)",
            _ => string.Empty,
        };

        return FirstNonEmpty(mappedCode, wmiStatus, $"Device issue (Code {configManagerErrorCode})", "Status unavailable");
    }

    private static string BuildPartitionTypeLabel(PartitionSnapshot partition, bool hasMountedVolume)
    {
        var currentType = FirstNonEmpty(partition.Type, "Unknown type");
        if (!currentType.Contains("Unknown", StringComparison.OrdinalIgnoreCase))
        {
            return currentType;
        }

        if (partition.BootPartition && partition.Size > 0 && partition.Size <= 300UL * 1024 * 1024)
        {
            return "GPT: EFI System (likely)";
        }

        if (!hasMountedVolume && partition.Size >= 450UL * 1024 * 1024 && partition.Size <= 1200UL * 1024 * 1024)
        {
            return "GPT: Recovery (likely)";
        }

        if (!hasMountedVolume && !partition.BootPartition && partition.Size > 0 && partition.Size <= 200UL * 1024 * 1024)
        {
            return "GPT: Microsoft Reserved (likely)";
        }

        if (!hasMountedVolume)
        {
            return "GPT: OEM/Recovery (unmounted, likely)";
        }

        return currentType;
    }

    private static string BuildSmartFallbackStatus(DiskSnapshot snapshot, PhysicalDiskSnapshot? physicalDisk)
    {
        var isNvme = snapshot.InterfaceType.Contains("NVMe", StringComparison.OrdinalIgnoreCase)
            || (physicalDisk?.BusType?.Contains("NVMe", StringComparison.OrdinalIgnoreCase) ?? false);
        if (!isNvme)
        {
            return "Unavailable";
        }

        var health = FirstNonEmpty(physicalDisk?.HealthStatus, snapshot.Status);
        if (health.Equals("Healthy", StringComparison.OrdinalIgnoreCase) || health.Equals("OK", StringComparison.OrdinalIgnoreCase))
        {
            return "Healthy (Windows NVMe health)";
        }

        if (health.Equals("Warning", StringComparison.OrdinalIgnoreCase))
        {
            return "Warning (Windows NVMe health)";
        }

        if (health.Equals("Unhealthy", StringComparison.OrdinalIgnoreCase))
        {
            return "Predicted failure (Windows NVMe health)";
        }

        return "Unavailable";
    }

    private static IReadOnlyList<HardwareSensorInfo> ReadLibreHardwareMonitorSensors(ICollection<string> warnings)
    {
        var assemblyPath = TryResolveLibreHardwareMonitorAssemblyPath();
        if (string.IsNullOrWhiteSpace(assemblyPath))
        {
            return [];
        }

        try
        {
            var assembly = Assembly.LoadFrom(assemblyPath);
            var computerType = assembly.GetType("LibreHardwareMonitor.Hardware.Computer");
            var iHardwareType = assembly.GetType("LibreHardwareMonitor.Hardware.IHardware");
            var iSensorType = assembly.GetType("LibreHardwareMonitor.Hardware.ISensor");
            if (computerType is null || iHardwareType is null || iSensorType is null)
            {
                return [];
            }

            using var computer = Activator.CreateInstance(computerType) as IDisposable;
            if (computer is null)
            {
                return [];
            }

            SetPropertyIfExists(computerType, computer, "IsCpuEnabled", true);
            SetPropertyIfExists(computerType, computer, "IsMotherboardEnabled", true);
            SetPropertyIfExists(computerType, computer, "IsMemoryEnabled", true);
            SetPropertyIfExists(computerType, computer, "IsGpuEnabled", true);
            SetPropertyIfExists(computerType, computer, "IsStorageEnabled", true);
            SetPropertyIfExists(computerType, computer, "IsControllerEnabled", true);

            computerType.GetMethod("Open")?.Invoke(computer, null);

            var hardwareItems = computerType.GetProperty("Hardware")?.GetValue(computer) as System.Collections.IEnumerable;
            if (hardwareItems is null)
            {
                return [];
            }

            var sensors = new List<HardwareSensorInfo>();
            foreach (var hardware in hardwareItems)
            {
                CollectLibreHardwareSensors(hardware, iHardwareType, iSensorType, sensors);
            }

            computerType.GetMethod("Close")?.Invoke(computer, null);
            return sensors;
        }
        catch (Exception ex)
        {
            warnings.Add($"LibreHardwareMonitor sensor provider failed: {ex.Message}");
            return [];
        }
    }

    private static void CollectLibreHardwareSensors(
        object hardware,
        Type iHardwareType,
        Type iSensorType,
        ICollection<HardwareSensorInfo> sensors)
    {
        iHardwareType.GetMethod("Update")?.Invoke(hardware, null);

        var hardwareName = Convert.ToString(iHardwareType.GetProperty("Name")?.GetValue(hardware)) ?? "Hardware";
        var sensorItems = iHardwareType.GetProperty("Sensors")?.GetValue(hardware) as System.Collections.IEnumerable;
        if (sensorItems is not null)
        {
            foreach (var sensor in sensorItems)
            {
                var sensorType = Convert.ToString(iSensorType.GetProperty("SensorType")?.GetValue(sensor)) ?? string.Empty;
                var value = iSensorType.GetProperty("Value")?.GetValue(sensor);
                if (value is null)
                {
                    continue;
                }

                var reading = Convert.ToSingle(value);
                var category = sensorType switch
                {
                    "Temperature" => "Temperature",
                    "Fan" => "Fan",
                    "Voltage" => "Voltage",
                    _ => string.Empty,
                };

                if (string.IsNullOrWhiteSpace(category))
                {
                    continue;
                }

                var sensorName = Convert.ToString(iSensorType.GetProperty("Name")?.GetValue(sensor)) ?? "Sensor";
                var readingText = category switch
                {
                    "Temperature" => $"{reading:0.#} C",
                    "Fan" => $"{reading:0} RPM",
                    "Voltage" => $"{reading:0.###} V",
                    _ => Convert.ToString(reading) ?? string.Empty,
                };

                sensors.Add(
                    new HardwareSensorInfo(
                        category,
                        $"{hardwareName} - {sensorName}",
                        readingText,
                        "LibreHardwareMonitor",
                        "Reported by optional LibreHardwareMonitor provider"));
            }
        }

        var subHardwareItems = iHardwareType.GetProperty("SubHardware")?.GetValue(hardware) as System.Collections.IEnumerable;
        if (subHardwareItems is null)
        {
            return;
        }

        foreach (var child in subHardwareItems)
        {
            CollectLibreHardwareSensors(child, iHardwareType, iSensorType, sensors);
        }
    }

    private static void SetPropertyIfExists(Type type, object instance, string propertyName, bool value)
    {
        var property = type.GetProperty(propertyName);
        if (property?.CanWrite == true)
        {
            property.SetValue(instance, value);
        }
    }

    private static string TryResolveLibreHardwareMonitorAssemblyPath()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "LibreHardwareMonitorLib.dll"),
            Path.Combine(AppContext.BaseDirectory, "LibreHardwareMonitor.dll"),
        };

        return candidates.FirstOrDefault(File.Exists) ?? string.Empty;
    }

    private static string BuildHealthSummary(
        IReadOnlyList<HardwareStorageDriveInfo> storageDrives,
        IReadOnlyList<HardwarePciDeviceInfo> pciDevices,
        IReadOnlyList<HardwareSensorInfo> sensors,
        IReadOnlyList<string> warnings)
    {
        var criticalCount = storageDrives.Count(
            drive => drive.HealthStatus.Contains("Unhealthy", StringComparison.OrdinalIgnoreCase)
                || drive.SmartStatus.Contains("Predicted failure", StringComparison.OrdinalIgnoreCase));

        var warningCount = storageDrives.Count(
                drive => drive.HealthStatus.Contains("Warning", StringComparison.OrdinalIgnoreCase)
                    || drive.SmartStatus.Contains("Warning", StringComparison.OrdinalIgnoreCase))
            + pciDevices.Count(device => !device.Status.Equals("OK", StringComparison.OrdinalIgnoreCase));

        var infoCount = warnings.Count
            + sensors.Count(sensor => sensor.Source.Equals("Win32_Fan", StringComparison.OrdinalIgnoreCase) && sensor.CurrentValue.Contains("unavailable", StringComparison.OrdinalIgnoreCase));

        if (criticalCount == 0 && warningCount == 0)
        {
            return infoCount == 0
                ? "No critical issues detected."
                : $"No critical issues detected. {infoCount} informational telemetry limitation{(infoCount == 1 ? string.Empty : "s")}.";
        }

        var parts = new List<string>();
        if (criticalCount > 0)
        {
            parts.Add($"{criticalCount} critical");
        }

        if (warningCount > 0)
        {
            parts.Add($"{warningCount} warning");
        }

        if (infoCount > 0)
        {
            parts.Add($"{infoCount} info");
        }

        return $"Health Summary: {string.Join(", ", parts)}.";
    }

    private static IReadOnlyList<HardwareRaidInfo> BuildRaidDetails(
        IReadOnlyList<HardwarePciDeviceInfo> pciDevices,
        ICollection<string> warnings)
    {
        var raidDetails = new List<HardwareRaidInfo>();

        var virtualDisks = TryReadMany(
            StorageManagementScope,
            "SELECT FriendlyName, HealthStatus, OperationalStatus, ResiliencySettingName, ProvisioningType, Size FROM MSFT_VirtualDisk",
            item => new VirtualDiskSnapshot(
                GetString(item, "FriendlyName"),
                MapStorageHealthStatus(GetInt(item, "HealthStatus")),
                MapOperationalStatuses(GetUInt16Array(item, "OperationalStatus")),
                GetString(item, "ResiliencySettingName"),
                MapProvisioningType(GetInt(item, "ProvisioningType")),
                GetUInt64(item, "Size")),
            warnings,
            "Virtual disk");

        foreach (var virtualDisk in virtualDisks.Where(static disk => !string.IsNullOrWhiteSpace(disk.FriendlyName)))
        {
            raidDetails.Add(
                new HardwareRaidInfo(
                    virtualDisk.FriendlyName,
                    string.IsNullOrWhiteSpace(virtualDisk.ResiliencySettingName)
                        ? "Storage Space"
                        : $"Storage Space ({virtualDisk.ResiliencySettingName})",
                    FirstNonEmpty(virtualDisk.HealthStatus, virtualDisk.OperationalStatus, "Unknown"),
                    string.Join(
                        "  |  ",
                        new[]
                        {
                            FormatBytes(virtualDisk.Size),
                            string.IsNullOrWhiteSpace(virtualDisk.ProvisioningType) ? string.Empty : $"Provisioning: {virtualDisk.ProvisioningType}",
                        }.Where(static value => !string.IsNullOrWhiteSpace(value))),
                    "MSFT_VirtualDisk"));
        }

        var storagePools = TryReadMany(
            StorageManagementScope,
            "SELECT FriendlyName, HealthStatus, OperationalStatus, IsPrimordial, Size FROM MSFT_StoragePool",
            item => new StoragePoolSnapshot(
                GetString(item, "FriendlyName"),
                MapStorageHealthStatus(GetInt(item, "HealthStatus")),
                MapOperationalStatuses(GetUInt16Array(item, "OperationalStatus")),
                GetBool(item, "IsPrimordial"),
                GetUInt64(item, "Size")),
            warnings,
            "Storage pool");

        foreach (var storagePool in storagePools.Where(static pool => !pool.IsPrimordial && !string.IsNullOrWhiteSpace(pool.FriendlyName)))
        {
            raidDetails.Add(
                new HardwareRaidInfo(
                    storagePool.FriendlyName,
                    "Storage Pool",
                    FirstNonEmpty(storagePool.HealthStatus, storagePool.OperationalStatus, "Unknown"),
                    FormatBytes(storagePool.Size),
                    "MSFT_StoragePool"));
        }

        foreach (var controller in pciDevices.Where(IsRaidController))
        {
            raidDetails.Add(
                new HardwareRaidInfo(
                    controller.Name,
                    "PCIe RAID Controller",
                    controller.Status,
                    string.Join(
                        "  |  ",
                        new[]
                        {
                            controller.Manufacturer,
                            controller.Location,
                        }.Where(static value => !string.IsNullOrWhiteSpace(value) && !value.Equals("Location unavailable", StringComparison.OrdinalIgnoreCase))),
                    "Win32_PnPEntity"));
        }

        return raidDetails
            .DistinctBy(static detail => $"{detail.Type}|{detail.Name}|{detail.Source}", StringComparer.OrdinalIgnoreCase)
            .OrderBy(static detail => detail.Type, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static detail => detail.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static T TryReadSingle<T>(string query, Func<ManagementObject, T> selector, T fallback, ICollection<string> warnings, string context) =>
        TryReadSingle(DefaultManagementScope, query, selector, fallback, warnings, context);

    private static T TryReadSingle<T>(
        string scopePath,
        string query,
        Func<ManagementObject, T> selector,
        T fallback,
        ICollection<string> warnings,
        string context)
    {
        try
        {
            using var searcher = CreateSearcher(scopePath, query);
            foreach (ManagementObject item in searcher.Get())
            {
                return selector(item);
            }
        }
        catch (Exception ex)
        {
            warnings.Add(FormatWarningMessage(context, ex));
        }

        return fallback;
    }

    private static IReadOnlyList<T> TryReadMany<T>(string query, Func<ManagementObject, T> selector, ICollection<string> warnings, string context) =>
        TryReadMany(DefaultManagementScope, query, selector, warnings, context);

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

    private static string FormatWarningMessage(string context, Exception ex)
    {
        var message = ex.Message?.Trim();
        if (IsAccessDeniedException(ex))
        {
            if (context.Contains("Thermal sensor", StringComparison.OrdinalIgnoreCase))
            {
                return $"{context}: Access denied. Windows blocked ACPI thermal telemetry for this non-admin session. Start MultiTool as administrator to read temperature sensors on this PC.";
            }

            return $"{context}: Access denied. Start MultiTool as administrator to access this hardware data on this PC.";
        }

        return string.IsNullOrWhiteSpace(message)
            ? $"{context}: Unknown error."
            : $"{context}: {message}";
    }

    private static bool IsAccessDeniedException(Exception ex) =>
        ex is UnauthorizedAccessException
        || ex is ManagementException { ErrorCode: ManagementStatus.AccessDenied }
        || ex.Message.Contains("Access denied", StringComparison.OrdinalIgnoreCase);

    private static ManagementObjectSearcher CreateSearcher(string scopePath, string query)
    {
        var scope = new ManagementScope(scopePath);
        scope.Connect();
        return new ManagementObjectSearcher(scope, new ObjectQuery(query));
    }

    private static PhysicalDiskSnapshot? MatchPhysicalDisk(DiskSnapshot snapshot, IReadOnlyList<PhysicalDiskSnapshot> physicalDisks)
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

    private static SmartStatusSnapshot? MatchSmartStatus(DiskSnapshot snapshot, IReadOnlyList<SmartStatusSnapshot> smartStatuses)
    {
        var candidateTokens = GetMatchTokens(snapshot.Model, snapshot.SerialNumber, snapshot.PnpDeviceId).ToArray();
        if (candidateTokens.Length == 0)
        {
            return null;
        }

        return smartStatuses
            .Select(
                smartStatus => new
                {
                    SmartStatus = smartStatus,
                    Score = candidateTokens.Max(token => ScoreMatchToken(token, smartStatus.InstanceName)),
                })
            .Where(static item => item.Score > 0)
            .OrderByDescending(static item => item.Score)
            .Select(static item => item.SmartStatus)
            .FirstOrDefault();
    }

    private static int ScorePhysicalDiskMatch(DiskSnapshot snapshot, PhysicalDiskSnapshot physicalDisk)
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

    private static bool IsPciDevice(string pnpDeviceId)
    {
        if (string.IsNullOrWhiteSpace(pnpDeviceId))
        {
            return false;
        }

        return pnpDeviceId.StartsWith("PCI\\", StringComparison.OrdinalIgnoreCase)
            || pnpDeviceId.StartsWith("PCIROOT\\", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsRaidController(HardwarePciDeviceInfo device) =>
        device.DeviceClass.Equals("SCSIAdapter", StringComparison.OrdinalIgnoreCase)
        || device.Name.Contains("RAID", StringComparison.OrdinalIgnoreCase)
        || device.Name.Contains("RST", StringComparison.OrdinalIgnoreCase)
        || device.Name.Contains("Storage Spaces", StringComparison.OrdinalIgnoreCase)
        || device.Name.Contains("NVMe RAID", StringComparison.OrdinalIgnoreCase);

    private static string ResolveGraphicsMemory(ulong adapterRam, string pnpDeviceId)
    {
        var registryMemory = TryGetGraphicsMemoryFromRegistry(pnpDeviceId);
        var bestMemory = Math.Max(adapterRam, registryMemory);
        return FormatBytes(bestMemory);
    }

    private static ulong TryGetGraphicsMemoryFromRegistry(string pnpDeviceId)
    {
        if (string.IsNullOrWhiteSpace(pnpDeviceId))
        {
            return 0UL;
        }

        try
        {
            var classMemory = TryGetGraphicsMemoryFromDisplayClassRegistry(pnpDeviceId);
            if (classMemory > 0)
            {
                return classMemory;
            }

            var videoMemory = TryGetGraphicsMemoryFromControlVideoRegistry(pnpDeviceId);
            if (videoMemory > 0)
            {
                return videoMemory;
            }
        }
        catch
        {
            return 0UL;
        }

        return 0UL;
    }

    private static ulong TryGetGraphicsMemoryFromDisplayClassRegistry(string pnpDeviceId)
    {
        var normalizedPnp = NormalizeRegistryDeviceId(pnpDeviceId);
        if (string.IsNullOrWhiteSpace(normalizedPnp))
        {
            return 0UL;
        }

        using var classRoot = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}");
        if (classRoot is null)
        {
            return 0UL;
        }

        foreach (var childName in classRoot.GetSubKeyNames())
        {
            using var childKey = classRoot.OpenSubKey(childName);
            if (childKey is null)
            {
                continue;
            }

            var matchingDeviceId = Convert.ToString(childKey.GetValue("MatchingDeviceId"));
            if (!RegistryDeviceIdsMatch(normalizedPnp, matchingDeviceId))
            {
                continue;
            }

            if (TryReadRegistryUlong(childKey, "HardwareInformation.qwMemorySize", out var qwordMemory) && qwordMemory > 0)
            {
                return qwordMemory;
            }

            if (TryReadRegistryUlong(childKey, "HardwareInformation.MemorySize", out var legacyMemory) && legacyMemory > 0)
            {
                return legacyMemory;
            }
        }

        return 0UL;
    }

    private static ulong TryGetGraphicsMemoryFromControlVideoRegistry(string pnpDeviceId)
    {
        var normalizedPnp = NormalizeRegistryDeviceId(pnpDeviceId);
        if (string.IsNullOrWhiteSpace(normalizedPnp))
        {
            return 0UL;
        }

        using var videoRoot = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Video");
        if (videoRoot is null)
        {
            return 0UL;
        }

        foreach (var adapterKeyName in videoRoot.GetSubKeyNames())
        {
            using var adapterKey = videoRoot.OpenSubKey(adapterKeyName);
            if (adapterKey is null)
            {
                continue;
            }

            foreach (var childName in adapterKey.GetSubKeyNames())
            {
                using var childKey = adapterKey.OpenSubKey(childName);
                if (childKey is null)
                {
                    continue;
                }

                var matchingDeviceId = Convert.ToString(childKey.GetValue("MatchingDeviceId"));
                if (!RegistryDeviceIdsMatch(normalizedPnp, matchingDeviceId))
                {
                    continue;
                }

                if (TryReadRegistryUlong(childKey, "HardwareInformation.qwMemorySize", out var memorySize) && memorySize > 0)
                {
                    return memorySize;
                }

                if (TryReadRegistryUlong(childKey, "HardwareInformation.MemorySize", out var legacyMemory) && legacyMemory > 0)
                {
                    return legacyMemory;
                }
            }
        }

        return 0UL;
    }

    private static bool RegistryDeviceIdsMatch(string normalizedPnp, string? candidateDeviceId)
    {
        var normalizedCandidate = NormalizeRegistryDeviceId(candidateDeviceId);
        if (string.IsNullOrWhiteSpace(normalizedCandidate))
        {
            return false;
        }

        return normalizedPnp.Equals(normalizedCandidate, StringComparison.OrdinalIgnoreCase)
            || normalizedPnp.Contains(normalizedCandidate, StringComparison.OrdinalIgnoreCase)
            || normalizedCandidate.Contains(normalizedPnp, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeRegistryDeviceId(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value
            .Replace('/', '\\')
            .Trim()
            .ToUpperInvariant();
    }

    private static bool TryReadRegistryUlong(RegistryKey key, string valueName, out ulong value)
    {
        value = 0UL;
        var rawValue = key.GetValue(valueName);
        if (rawValue is null)
        {
            return false;
        }

        switch (rawValue)
        {
            case ulong typedUlong:
                value = typedUlong;
                return typedUlong > 0;
            case long typedLong when typedLong > 0:
                value = Convert.ToUInt64(typedLong);
                return true;
            case int typedInt when typedInt > 0:
                value = Convert.ToUInt64(typedInt);
                return true;
            case byte[] bytes when bytes.Length == sizeof(ulong):
                value = BitConverter.ToUInt64(bytes, 0);
                return value > 0;
            case string text when ulong.TryParse(text, out var parsed) && parsed > 0:
                value = parsed;
                return true;
            default:
                return false;
        }
    }

    private static string BuildVolumeLabel(LogicalDiskSnapshot logicalDisk) =>
        string.IsNullOrWhiteSpace(logicalDisk.VolumeName)
            ? logicalDisk.DeviceId
            : $"{logicalDisk.DeviceId} ({logicalDisk.VolumeName})";

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

    private static ushort[] GetUInt16Array(ManagementBaseObject source, string propertyName)
    {
        var value = GetValue(source, propertyName);
        return value switch
        {
            null => [],
            ushort[] typedValues => typedValues,
            Array values => values.Cast<object?>().Where(static item => item is not null).Select(static item => Convert.ToUInt16(item)).ToArray(),
            _ => [],
        };
    }

    private static string GetDateString(ManagementBaseObject source, string propertyName)
    {
        var rawValue = GetString(source, propertyName);
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return string.Empty;
        }

        try
        {
            return ManagementDateTimeConverter.ToDateTime(rawValue).ToString("yyyy-MM-dd");
        }
        catch
        {
            return rawValue;
        }
    }

    private static string FormatAcpiTemperature(ulong rawValue)
    {
        if (rawValue == 0)
        {
            return string.Empty;
        }

        var celsius = ((decimal)rawValue / 10m) - 273.15m;
        if (celsius < -100m || celsius > 250m)
        {
            return $"Raw {rawValue}";
        }

        return $"{celsius:0.#} C";
    }

    private static string BuildFanReading(int desiredSpeed) =>
        desiredSpeed > 0 ? $"{desiredSpeed} RPM target" : "Speed unavailable";

    private static string FormatRawSensorReading(int reading) =>
        reading > 0 ? $"Raw {reading}" : string.Empty;

    private static string MapStorageHealthStatus(int healthStatus) =>
        healthStatus switch
        {
            1 => "Healthy",
            2 => "Warning",
            3 => "Unhealthy",
            _ => string.Empty,
        };

    private static string MapOperationalStatuses(IReadOnlyList<ushort> operationalStatuses)
    {
        if (operationalStatuses.Count == 0)
        {
            return string.Empty;
        }

        return string.Join(
            ", ",
            operationalStatuses.Select(
                static status => status switch
                {
                    1 => "Other",
                    2 => "OK",
                    3 => "Degraded",
                    4 => "Stressed",
                    5 => "Predictive failure",
                    6 => "Error",
                    7 => "Non-recoverable error",
                    8 => "Starting",
                    9 => "Stopping",
                    10 => "Stopped",
                    11 => "In service",
                    _ => $"Code {status}",
                }));
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

    private static string MapProvisioningType(int provisioningType) =>
        provisioningType switch
        {
            1 => "Fixed",
            2 => "Thin",
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

    private sealed record DiskSnapshot(
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

    private sealed record PhysicalDiskSnapshot(
        string FriendlyName,
        ulong Size,
        string BusType,
        string MediaType,
        string HealthStatus,
        string OperationalStatus,
        string SerialNumber,
        string FirmwareVersion,
        string DeviceId);

    private sealed record SmartStatusSnapshot(
        string InstanceName,
        bool PredictFailure,
        string Reason);

    private sealed record PartitionSnapshot(
        int DiskIndex,
        string DeviceId,
        string Name,
        ulong Size,
        string Type,
        bool BootPartition,
        bool PrimaryPartition,
        string Status);

    private sealed record LogicalDiskSnapshot(
        string DeviceId,
        string FileSystem,
        ulong FreeSpace,
        string VolumeName);

    private sealed record PartitionLinkSnapshot(
        string PartitionDeviceId,
        string LogicalDiskId);

    private sealed record PciDeviceSnapshot(
        string Name,
        string DeviceClass,
        string Manufacturer,
        string Location,
        string Status,
        string PnpDeviceId);

    private sealed record VirtualDiskSnapshot(
        string FriendlyName,
        string HealthStatus,
        string OperationalStatus,
        string ResiliencySettingName,
        string ProvisioningType,
        ulong Size);

    private sealed record StoragePoolSnapshot(
        string FriendlyName,
        string HealthStatus,
        string OperationalStatus,
        bool IsPrimordial,
        ulong Size);
}
