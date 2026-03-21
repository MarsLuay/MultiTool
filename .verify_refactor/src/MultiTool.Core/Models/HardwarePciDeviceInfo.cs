namespace MultiTool.Core.Models;

public sealed record HardwarePciDeviceInfo(
    string Name,
    string DeviceClass,
    string Manufacturer,
    string Location,
    string Status);
