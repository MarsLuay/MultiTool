using AutoClicker.Core.Models;

namespace AutoClicker.Core.Services;

public interface IWindowsTelemetryService
{
    WindowsTelemetryStatus GetStatus();

    Task<WindowsTelemetryResult> ApplyAsync(CancellationToken cancellationToken = default);

    Task<WindowsTelemetryResult> RestoreAsync(CancellationToken cancellationToken = default);
}
