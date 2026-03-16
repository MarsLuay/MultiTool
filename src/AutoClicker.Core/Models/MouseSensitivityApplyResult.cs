namespace AutoClicker.Core.Models;

public sealed record MouseSensitivityApplyResult(
    bool Succeeded,
    bool Changed,
    int AppliedLevel,
    string Message);
