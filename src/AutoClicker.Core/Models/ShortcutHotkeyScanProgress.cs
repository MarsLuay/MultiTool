namespace AutoClicker.Core.Models;

public sealed record ShortcutHotkeyScanProgress(
    int CompletedFolderCount,
    int TotalFolderCount,
    int ScannedShortcutCount,
    string CurrentPath);
