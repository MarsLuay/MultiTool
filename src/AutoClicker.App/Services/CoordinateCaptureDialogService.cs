using AutoClicker.App.Views;
using AutoClicker.Core.Models;

namespace AutoClicker.App.Services;

public sealed class CoordinateCaptureDialogService : ICoordinateCaptureDialogService
{
    public ScreenPoint? Capture()
    {
        var window = new CoordinateCaptureWindow();
        return window.ShowDialog() == true ? window.CapturedPoint : null;
    }
}
