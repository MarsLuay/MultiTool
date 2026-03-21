namespace MultiTool.Core.Models;

public sealed record HardwareStorageDriveInfo(
    string Model,
    string Size,
    string InterfaceType,
    string MediaType,
    string HealthStatus,
    string SmartStatus,
    string FirmwareVersion,
    string SerialNumber,
    string Notes);
