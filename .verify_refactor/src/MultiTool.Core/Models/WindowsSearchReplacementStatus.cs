namespace MultiTool.Core.Models;

public sealed record WindowsSearchReplacementStatus(
    bool IsConfigured,
    bool IsFlowLauncherInstalled,
    bool IsEverythingInstalled,
    bool IsAutoHotkeyInstalled,
    bool HasHotkeyRemap,
    bool IsWindowsSearchDisabled,
    string Message);
