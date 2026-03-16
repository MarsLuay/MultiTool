using AutoClicker.Core.Models;

namespace AutoClicker.Core.Services;

public interface IEmptyDirectoryService
{
    Task<EmptyDirectoryScanResult> FindEmptyDirectoriesAsync(
        string rootPath,
        IProgress<EmptyDirectoryScanProgress>? progress = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EmptyDirectoryDeleteResult>> DeleteDirectoriesAsync(IEnumerable<string> directoryPaths, CancellationToken cancellationToken = default);
}
