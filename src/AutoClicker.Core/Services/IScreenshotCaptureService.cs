using AutoClicker.Core.Models;

namespace AutoClicker.Core.Services;

public interface IScreenshotCaptureService
{
    Task<string> CaptureDesktopAsync(string outputDirectory, string fileNamePrefix, CancellationToken cancellationToken = default);

    Task<string> CaptureAreaAsync(ScreenRectangle area, string outputDirectory, string fileNamePrefix, CancellationToken cancellationToken = default);

    bool IsVideoCaptureRunning { get; }

    Task StartVideoCaptureAsync(string outputDirectory, string fileNamePrefix, ScreenRectangle? area = null, CancellationToken cancellationToken = default);

    Task<string?> StopVideoCaptureAsync(CancellationToken cancellationToken = default);
}
