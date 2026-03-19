using MultiTool.Core.Models;

namespace MultiTool.App.Services;

public interface IScreenshotAreaSelectionService
{
    ScreenRectangle? SelectArea();
}
