using MultiTool.Core.Models;
using MultiTool.Core.Results;

namespace MultiTool.Core.Services;

public interface IScreenshotCaptureService
{
    event EventHandler<VideoCaptureStateChangedEventArgs>? VideoCaptureStateChanged;

    Task<string> CaptureDesktopAsync(string outputDirectory, string fileNamePrefix, CancellationToken cancellationToken = default);

    Task<string> CaptureAreaAsync(ScreenRectangle area, string outputDirectory, string fileNamePrefix, CancellationToken cancellationToken = default);

    bool IsVideoCaptureRunning { get; }

    string? LastSavedVideoPath { get; }

    Task StartVideoCaptureAsync(string outputDirectory, string fileNamePrefix, ScreenRectangle? area = null, CancellationToken cancellationToken = default);

    Task<string?> StopVideoCaptureAsync(CancellationToken cancellationToken = default);
}
