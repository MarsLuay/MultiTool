namespace AutoClicker.Core.Models;

public sealed record WindowsSearchReindexResult(
    bool Succeeded,
    bool Changed,
    string Message);