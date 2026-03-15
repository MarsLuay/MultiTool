namespace AutoClicker.Core.Models;

public sealed record InstallerOperationResult(
    string PackageId,
    string DisplayName,
    bool Succeeded,
    bool Changed,
    string Message,
    string Output);
