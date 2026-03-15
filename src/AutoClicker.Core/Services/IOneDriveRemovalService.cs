using AutoClicker.Core.Models;

namespace AutoClicker.Core.Services;

public interface IOneDriveRemovalService
{
    OneDriveRemovalStatus GetStatus();

    Task<OneDriveRemovalResult> RemoveAsync(CancellationToken cancellationToken = default);
}
