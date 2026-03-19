using MultiTool.App.Views;
using MultiTool.Core.Models;

namespace MultiTool.App.Services;

public sealed class CoordinateCaptureDialogService : ICoordinateCaptureDialogService
{
    public ScreenPoint? Capture()
    {
        var window = new CoordinateCaptureWindow();
        return window.ShowDialog() == true ? window.CapturedPoint : null;
    }
}
