namespace AutoClicker.Core.Models;

public sealed record HardwareInventoryReport(
    string SystemSummary,
    string OperatingSystemSummary,
    string ProcessorSummary,
    string MemorySummary,
    string MotherboardSummary,
    string BiosSummary,
    IReadOnlyList<HardwareDisplayAdapterInfo> GraphicsAdapters,
    IReadOnlyList<HardwareStorageDriveInfo> StorageDrives,
    IReadOnlyList<string> Warnings);
