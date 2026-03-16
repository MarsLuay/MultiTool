namespace AutoClicker.Core.Models;

public sealed record HardwareSensorInfo(
    string Category,
    string Name,
    string CurrentValue,
    string Source,
    string Status);
