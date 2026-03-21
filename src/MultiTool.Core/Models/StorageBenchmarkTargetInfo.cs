namespace MultiTool.Core.Models;

public sealed record StorageBenchmarkTargetInfo(
    string TargetId,
    string DisplayName,
    string Model,
    string Size,
    string InterfaceType,
    string MediaType,
    string FirmwareVersion,
    string VolumeRootPath,
    string VolumeLabel,
    string FileSystem,
    string FreeSpace);
