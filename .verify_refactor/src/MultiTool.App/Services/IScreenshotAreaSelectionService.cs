using MultiTool.App.Models;
using MultiTool.Core.Models;

namespace MultiTool.App.Services;

public interface IScreenshotAreaSelectionService
{
    Task<ScreenRectangle?> SelectAreaAsync(CancellationToken cancellationToken = default);

    Task<VideoCaptureSelection?> SelectVideoCaptureAsync(CancellationToken cancellationToken = default);
}
