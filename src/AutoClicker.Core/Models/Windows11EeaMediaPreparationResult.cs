namespace AutoClicker.Core.Models;

public sealed record Windows11EeaMediaPreparationResult(
    bool Succeeded,
    bool Changed,
    string WorkspacePath,
    string MediaCreationToolPath,
    string AnswerFilePath,
    string Message);
