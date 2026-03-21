namespace MultiTool.Core.Models;

public sealed record StorageBenchmarkReport(
    StorageBenchmarkTargetInfo Target,
    string Summary,
    string BalanceAssessment,
    string DetectedSystemSummary,
    DateTimeOffset CapturedAt,
    IReadOnlyList<StorageBenchmarkModeResult> Results,
    IReadOnlyList<string> Warnings);
