namespace MultiTool.Core.Models;

public sealed record WindowsSearchReindexResult(
    bool Succeeded,
    bool Changed,
    string Message);