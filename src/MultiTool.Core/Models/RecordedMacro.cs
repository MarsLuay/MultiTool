namespace MultiTool.Core.Models;

public sealed record RecordedMacro(
    string Name,
    IReadOnlyList<MacroEvent> Events,
    TimeSpan Duration,
    DateTimeOffset RecordedAt);
