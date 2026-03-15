namespace AutoClicker.Core.Models;

public sealed record EmptyDirectoryDeleteResult(
    string DirectoryPath,
    bool Succeeded,
    bool Deleted,
    string Message);
