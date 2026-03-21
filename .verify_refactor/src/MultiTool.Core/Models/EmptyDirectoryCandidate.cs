namespace MultiTool.Core.Models;

public sealed record EmptyDirectoryCandidate(
    string FullPath,
    bool ContainsNestedEmptyDirectories);
