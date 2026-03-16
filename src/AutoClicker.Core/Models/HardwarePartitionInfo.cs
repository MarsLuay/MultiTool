namespace AutoClicker.Core.Models;

public sealed record HardwarePartitionInfo(
    string DiskName,
    string PartitionName,
    string Size,
    string Type,
    string Volume,
    string FileSystem,
    string FreeSpace,
    string Status);
