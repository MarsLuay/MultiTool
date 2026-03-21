using MultiTool.Core.Models;

namespace MultiTool.App.Services;

public interface ICoordinateCaptureDialogService
{
    ScreenPoint? Capture();
}
