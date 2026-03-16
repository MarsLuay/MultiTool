namespace AutoClicker.Core.Models;

public sealed record ShortcutHotkeyScanResult(
    IReadOnlyList<ShortcutHotkeyInfo> Shortcuts,
    int ScannedShortcutCount,
    IReadOnlyList<string> Warnings,
    int ConflictGroupCount = 0,
    int ConflictingShortcutCount = 0);
