using AutoClicker.Core.Models;

namespace AutoClicker.App.Services;

public interface IHotkeySettingsDialogService
{
    HotkeySettings? Edit(HotkeySettings currentSettings);
}
