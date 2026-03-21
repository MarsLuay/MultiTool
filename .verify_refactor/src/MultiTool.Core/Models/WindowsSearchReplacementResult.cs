namespace MultiTool.Core.Models;

public sealed record WindowsSearchReplacementResult(
    bool Succeeded,
    bool Changed,
    string Message);
