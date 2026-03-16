using AutoClicker.Core.Models;

namespace AutoClicker.App.Services;

public interface IShortcutHotkeyDialogService
{
    void Show(ShortcutHotkeyScanResult result);
}
