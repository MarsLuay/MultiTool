namespace AutoClicker.Core.Models;

public sealed record DisplayRefreshApplyResult(
    string DeviceName,
    string DisplayName,
    bool Succeeded,
    bool Changed,
    string Message);
