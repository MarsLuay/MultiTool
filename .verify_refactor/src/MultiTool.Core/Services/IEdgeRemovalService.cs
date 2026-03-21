using MultiTool.Core.Models;

namespace MultiTool.Core.Services;

public interface IEdgeRemovalService
{
    EdgeRemovalStatus GetStatus();

    Task<EdgeRemovalResult> RemoveAsync(CancellationToken cancellationToken = default);
}
