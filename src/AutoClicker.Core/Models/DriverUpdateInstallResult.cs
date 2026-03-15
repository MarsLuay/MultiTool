namespace AutoClicker.Core.Models;

public sealed record DriverUpdateInstallResult(
    string UpdateId,
    string Title,
    bool Succeeded,
    bool Changed,
    bool RequiresRestart,
    string Message);
