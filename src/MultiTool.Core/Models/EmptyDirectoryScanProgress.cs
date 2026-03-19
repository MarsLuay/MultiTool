namespace MultiTool.Core.Models;

public sealed record EmptyDirectoryScanProgress(
    int CompletedDirectoryCount,
    int TotalDirectoryCount,
    string CurrentPath);
