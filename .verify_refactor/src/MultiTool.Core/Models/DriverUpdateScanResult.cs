namespace MultiTool.Core.Models;

public sealed record DriverUpdateScanResult(
    IReadOnlyList<DriverHardwareInfo> Hardware,
    IReadOnlyList<DriverUpdateCandidate> Updates,
    IReadOnlyList<string> Warnings);
