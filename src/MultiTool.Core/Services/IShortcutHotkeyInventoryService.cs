using MultiTool.Core.Models;

namespace MultiTool.Core.Services;

public interface IShortcutHotkeyInventoryService
{
    Task<ShortcutHotkeyScanResult> ScanAsync(
        IProgress<ShortcutHotkeyScanProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
