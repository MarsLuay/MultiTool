using MultiTool.Core.Models;

namespace MultiTool.Core.Services;

public interface IShortcutHotkeyDisableService
{
    Task<ShortcutHotkeyDisableResult> DisableAsync(
        IReadOnlyList<ShortcutHotkeyInfo> shortcuts,
        CancellationToken cancellationToken = default);
}
