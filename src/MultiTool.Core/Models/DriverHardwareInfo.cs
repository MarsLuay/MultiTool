namespace MultiTool.Core.Models;

public sealed record DriverHardwareInfo(
    string DeviceName,
    string Manufacturer,
    string DriverProvider,
    string DriverVersion,
    string DeviceClass,
    string DeviceId);
