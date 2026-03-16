namespace AutoClicker.Core.Models;

public sealed record HardwareRaidInfo(
    string Name,
    string Type,
    string Status,
    string Details,
    string Source);
