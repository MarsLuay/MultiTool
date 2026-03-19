namespace MultiTool.Core.Models;

public sealed record DisplayRefreshRecommendation(
    string DeviceName,
    string DisplayName,
    string Resolution,
    int CurrentFrequency,
    int RecommendedFrequency,
    bool NeedsChange,
    string Message);
