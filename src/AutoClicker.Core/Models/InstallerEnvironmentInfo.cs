namespace AutoClicker.Core.Models;

public sealed record InstallerEnvironmentInfo(
    bool IsAvailable,
    string Version,
    string Message);
