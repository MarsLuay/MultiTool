namespace MultiTool.Core.Models;

public sealed class ToolSettings
{
    public int ShortcutHotkeyScanMaxFolderCount { get; set; }

    public Dictionary<string, int> EmptyDirectoryScanMaxFolderCounts { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public ToolSettings Clone() =>
        new()
        {
            ShortcutHotkeyScanMaxFolderCount = ShortcutHotkeyScanMaxFolderCount,
            EmptyDirectoryScanMaxFolderCounts = new Dictionary<string, int>(EmptyDirectoryScanMaxFolderCounts, StringComparer.OrdinalIgnoreCase),
        };
}
