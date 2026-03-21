namespace MultiTool.Core.Models;

public sealed record EmptyDirectoryScanResult(
    IReadOnlyList<EmptyDirectoryCandidate> Candidates,
    IReadOnlyList<string> Warnings);
