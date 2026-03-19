namespace MultiTool.Core.Models;

public sealed record InstallerCatalogItem(
    string PackageId,
    string DisplayName,
    string Category,
    string Description,
    bool IsRecommended = false,
    bool IsDeveloperTool = false,
    string? Source = null,
    IReadOnlyList<string>? Dependencies = null,
    bool TrackStatusWithWinget = true,
    string? InstallUrl = null,
    string? UpdateUrl = null,
    bool UsesCustomInstallFlow = false);
