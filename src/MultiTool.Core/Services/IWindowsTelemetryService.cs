using MultiTool.Core.Models;

namespace MultiTool.Core.Services;

public interface IWindowsTelemetryService
{
    WindowsTelemetryStatus GetStatus();

    Task<WindowsTelemetryResult> ApplyAsync(CancellationToken cancellationToken = default);

    Task<WindowsTelemetryResult> RestoreAsync(CancellationToken cancellationToken = default);
}
