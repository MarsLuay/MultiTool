using AutoClicker.Core.Models;

namespace AutoClicker.Core.Services;

public interface IAppUpdateService
{
    Task<AppUpdateInfo> CheckForUpdatesAsync(CancellationToken cancellationToken = default);
}
