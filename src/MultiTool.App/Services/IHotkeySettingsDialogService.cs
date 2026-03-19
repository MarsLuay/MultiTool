using MultiTool.Core.Models;

namespace MultiTool.App.Services;

public interface IHotkeySettingsDialogService
{
    HotkeySettings? Edit(HotkeySettings currentSettings);
}
