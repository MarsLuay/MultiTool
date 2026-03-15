using AutoClicker.App.Models;
using AutoClicker.Core.Models;

namespace AutoClicker.App.Services;

public interface IScreenshotOptionsDialogService
{
    ScreenshotOptionsResult SelectMode(ScreenshotSettings settings);

    Task<bool> TryHandleCaptureHotkeyAsync();
}
