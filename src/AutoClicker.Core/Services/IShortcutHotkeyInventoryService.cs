using AutoClicker.Core.Models;

namespace AutoClicker.Core.Services;

public interface IShortcutHotkeyInventoryService
{
    Task<ShortcutHotkeyScanResult> ScanAsync(
        IProgress<ShortcutHotkeyScanProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
