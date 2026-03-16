namespace AutoClicker.Core.Models;

public sealed record EmptyDirectoryScanProgress(
    int CompletedDirectoryCount,
    int TotalDirectoryCount,
    string CurrentPath);
