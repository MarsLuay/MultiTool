namespace MultiTool.Core.Models;

public sealed record ShortcutHotkeyDisableResult(
    int DisabledCount,
    int SupportedCount,
    int UnsupportedCount,
    IReadOnlyList<string> Warnings);
