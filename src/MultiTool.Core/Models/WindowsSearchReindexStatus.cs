namespace MultiTool.Core.Models;

public sealed record WindowsSearchReindexStatus(
    bool IsSearchServiceAvailable,
    bool RequiresAdministratorPrompt,
    string Message);