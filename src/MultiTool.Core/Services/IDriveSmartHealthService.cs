using MultiTool.Core.Models;

namespace MultiTool.Core.Services;

public interface IDriveSmartHealthService
{
    Task<IReadOnlyList<DriveSmartTargetInfo>> GetAvailableDrivesAsync(CancellationToken cancellationToken = default);

    Task<DriveSmartHealthReport> ScanAsync(string deviceId, CancellationToken cancellationToken = default);
}
