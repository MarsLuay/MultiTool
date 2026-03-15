namespace AutoClicker.App.Models;

public sealed class SavedMacroEntry
{
    public required string DisplayName { get; init; }

    public required string FileName { get; init; }

    public required string FilePath { get; init; }

    public override string ToString() => DisplayName;
}
