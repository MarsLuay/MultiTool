using AutoClicker.Core.Models;

namespace AutoClicker.Core.Services;

public interface IEdgeRemovalService
{
    EdgeRemovalStatus GetStatus();

    Task<EdgeRemovalResult> RemoveAsync(CancellationToken cancellationToken = default);
}
