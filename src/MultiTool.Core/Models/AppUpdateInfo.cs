namespace MultiTool.Core.Models;

public sealed record AppUpdateInfo(
    bool CheckedSuccessfully,
    bool IsUpdateAvailable,
    string CurrentVersion,
    string? LatestVersion,
    string Message,
    string? ReleaseUrl = null);
