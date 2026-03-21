using MultiTool.App.Models;
using MultiTool.Core.Models;

namespace MultiTool.App.Services;

public interface IShortcutHotkeyDialogService
{
    void Show(
        ShortcutHotkeyScanResult result,
        bool isCachedResult,
        Func<Task<ShortcutHotkeyScanResult>> rescanAsync,
        Func<IReadOnlyList<ShortcutHotkeyInfo>, Task<ShortcutHotkeyDisableOperationResult>> disableAsync);
}
