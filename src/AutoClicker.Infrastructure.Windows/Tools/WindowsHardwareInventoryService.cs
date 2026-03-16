using System.Management;
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

        return new HardwareInventoryReport(
            $"{systemInfo.Name}  |  {systemInfo.Manufacturer} {systemInfo.Model}".Trim(),
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
            "SELECT Name, DriverVersion, AdapterRAM FROM Win32_VideoController WHERE Name IS NOT NULL",
            item => new HardwareDisplayAdapterInfo(
                GetString(item, "Name", "Unknown GPU"),
                GetString(item, "DriverVersion", "Unknown driver"),
                FormatBytes(GetUInt64(item, "AdapterRAM"))),
            warnings,
            "Graphics adapter");

        return adapters.Count == 0
            ? [new HardwareDisplayAdapterInfo("No graphics adapters detected.", string.Empty, string.Empty)]
            : adapters;
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
                        ? "Unavailable"
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
                        FirstNonEmpty(partition.Type, "Unknown type"),
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

        return sensors
            .OrderBy(static sensor => sensor.Category, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static sensor => sensor.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IReadOnlyList<HardwarePciDeviceInfo> BuildPciDevices(ICollection<string> warnings)
    {
        var devices = TryReadMany(
            "SELECT Name, Manufacturer, PNPClass, Status, LocationInformation, PNPDeviceID FROM Win32_PnPEntity WHERE PNPDeviceID IS NOT NULL",
            item => new PciDeviceSnapshot(
                FirstNonEmpty(GetString(item, "Name"), "Unknown PCI device"),
                FirstNonEmpty(GetString(item, "PNPClass"), "Unclassified"),
                FirstNonEmpty(GetString(item, "Manufacturer"), "Unknown manufacturer"),
                FirstNonEmpty(GetString(item, "LocationInformation"), "Location unavailable"),
                FirstNonEmpty(GetString(item, "Status"), "Status unavailable"),
                GetString(item, "PNPDeviceID")),
            warnings,
            "PCI device");

        return devices
            .Where(static device => IsPciDevice(device.PnpDeviceId))
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
