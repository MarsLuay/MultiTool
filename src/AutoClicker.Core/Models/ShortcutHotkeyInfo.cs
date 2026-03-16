namespace AutoClicker.Core.Models;

public sealed record ShortcutHotkeyInfo(
    string Hotkey,
    string ShortcutName,
    string ShortcutPath,
    string FolderPath,
    string TargetPath,
    bool TargetExists,
    string SourceLabel = "Detected shortcut file",
    string AppliesTo = "Windows shortcut file",
    string Details = "",
    bool IsReferenceShortcut = false,
    bool HasConflict = false,
    int ConflictCount = 0,
    string ConflictSummary = "");
