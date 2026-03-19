using MultiTool.Core.Models;

namespace MultiTool.Core.Services;

public interface IFirefoxExtensionService
{
    IReadOnlyList<InstallerOptionDefinition> GetCatalog();

    Task<IReadOnlyList<InstallerOperationResult>> SyncExtensionSelectionsAsync(
        IEnumerable<string> selectedOptionIds,
        CancellationToken cancellationToken = default);
}
