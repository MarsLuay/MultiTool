namespace MultiTool.Core.Models;

public sealed record HardwareInventoryReport(
    string SystemSummary,
    string HealthSummary,
    string OperatingSystemSummary,
    string ProcessorSummary,
    string MemorySummary,
    string MotherboardSummary,
    string BiosSummary,
    IReadOnlyList<HardwareDisplayAdapterInfo> GraphicsAdapters,
    IReadOnlyList<HardwareStorageDriveInfo> StorageDrives,
    IReadOnlyList<HardwarePartitionInfo> StoragePartitions,
    IReadOnlyList<HardwareSensorInfo> Sensors,
    IReadOnlyList<HardwarePciDeviceInfo> PciDevices,
    IReadOnlyList<HardwareRaidInfo> RaidDetails,
    IReadOnlyList<DriverHardwareInfo> DriverHardwareInventory,
    IReadOnlyList<string> Warnings);
