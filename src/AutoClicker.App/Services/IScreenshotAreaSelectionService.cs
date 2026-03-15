using AutoClicker.Core.Models;

namespace AutoClicker.App.Services;

public interface IScreenshotAreaSelectionService
{
    ScreenRectangle? SelectArea();
}
