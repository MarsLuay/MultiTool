using MultiTool.Core.Models;

namespace MultiTool.Core.Services;

public interface IStorageBenchmarkService
{
    Task<IReadOnlyList<StorageBenchmarkTargetInfo>> GetAvailableTargetsAsync(CancellationToken cancellationToken = default);

    Task<StorageBenchmarkReport> RunAsync(
        string targetId,
        IProgress<StorageBenchmarkProgressUpdate>? progress = null,
        CancellationToken cancellationToken = default);
}
