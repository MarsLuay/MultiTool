namespace MultiTool.Core.Models;

public sealed record DriverUpdateCandidate(
    string UpdateId,
    string Title,
    string DriverModel,
    string DriverManufacturer,
    string DriverClass,
    string DriverDate,
    string Description,
    bool IsOptional,
    bool RequiresUserInput);
