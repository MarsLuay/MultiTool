using MultiTool.Core.Models;

namespace MultiTool.Core.Services;

public interface IAppUpdateService
{
    Task<AppUpdateInfo> CheckForUpdatesAsync(CancellationToken cancellationToken = default);
}
