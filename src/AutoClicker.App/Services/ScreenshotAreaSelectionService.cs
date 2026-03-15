using AutoClicker.App.Views;
using AutoClicker.Core.Models;

namespace AutoClicker.App.Services;

public sealed class ScreenshotAreaSelectionService : IScreenshotAreaSelectionService
{
    public ScreenRectangle? SelectArea()
    {
        var window = new ScreenshotAreaSelectionWindow();
        return window.ShowDialog() == true ? window.SelectedArea : null;
    }
}
