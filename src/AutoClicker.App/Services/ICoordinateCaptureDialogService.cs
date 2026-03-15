using AutoClicker.Core.Models;

namespace AutoClicker.App.Services;

public interface ICoordinateCaptureDialogService
{
    ScreenPoint? Capture();
}
