using MultiTool.Core.Models;

namespace MultiTool.Core.Services;

public interface ISystemTrayMetricsService
{
    Task<SystemTrayMetricsSnapshot> CaptureAsync(CancellationToken cancellationToken = default);
}
