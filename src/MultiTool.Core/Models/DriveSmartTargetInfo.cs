namespace MultiTool.Core.Models;

public sealed record DriveSmartTargetInfo(
    string DeviceId,
    string DisplayName,
    string Model,
    string Size,
    string InterfaceType,
    string MediaType,
    string FirmwareVersion,
    string SerialNumber,
    string PrimaryVolumeRootPath = "",
    string VolumePathsSummary = "");
