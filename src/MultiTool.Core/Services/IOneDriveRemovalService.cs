using MultiTool.Core.Models;

namespace MultiTool.Core.Services;

public interface IOneDriveRemovalService
{
    OneDriveRemovalStatus GetStatus();

    Task<OneDriveRemovalResult> RemoveAsync(CancellationToken cancellationToken = default);
}
