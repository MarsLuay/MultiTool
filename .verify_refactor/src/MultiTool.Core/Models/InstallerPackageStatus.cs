namespace MultiTool.Core.Models;

public sealed record InstallerPackageStatus(
    string PackageId,
    bool IsInstalled,
    bool HasUpdateAvailable,
    string StatusText);
