namespace MultiTool.Core.Models;

public sealed record DriveSmartHealthReport(
    DriveSmartTargetInfo Drive,
    string OverallHealth,
    string Summary,
    DateTimeOffset CapturedAt,
    IReadOnlyList<DriveSmartAttributeInfo> Attributes,
    IReadOnlyList<string> Warnings);
