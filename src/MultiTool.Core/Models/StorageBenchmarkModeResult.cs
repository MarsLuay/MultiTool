namespace MultiTool.Core.Models;

public sealed record StorageBenchmarkModeResult(
    string Mode,
    double ThroughputMegabytesPerSecond,
    double Iops,
    int BlockSizeBytes,
    string Notes);
