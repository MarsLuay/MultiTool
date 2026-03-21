namespace MultiTool.Core.Models;

public sealed record StorageBenchmarkProgressUpdate(
    int CurrentStage,
    int TotalStages,
    string StageName,
    string Detail);
