namespace MultiTool.Core.Models;

public sealed record SystemTrayMetricsSnapshot(
    int? CpuUsagePercent,
    double? TemperatureCelsius,
    int? MemoryUsagePercent,
    int? DiskUsagePercent,
    DateTimeOffset CapturedAt);
