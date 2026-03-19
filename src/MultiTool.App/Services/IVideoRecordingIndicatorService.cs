using MultiTool.Core.Models;

namespace MultiTool.App.Services;

public interface IVideoRecordingIndicatorService : IDisposable
{
    void ShowForRecordingArea(ScreenRectangle? captureArea);

    void Hide();
}
