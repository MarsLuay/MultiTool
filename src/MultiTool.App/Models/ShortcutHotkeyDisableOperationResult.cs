using MultiTool.Core.Models;

namespace MultiTool.App.Models;

public sealed record ShortcutHotkeyDisableOperationResult(
    ShortcutHotkeyScanResult ScanResult,
    ShortcutHotkeyDisableResult DisableResult);
