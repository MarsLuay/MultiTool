using MultiTool.Core.Models;

namespace MultiTool.Core.Services;

public interface IInstallerService
{
    IReadOnlyList<InstallerCatalogItem> GetCatalog();

    IReadOnlyList<InstallerCatalogItem> GetCleanupCatalog();

    InstallerPackageCapabilities GetPackageCapabilities(string packageId);

    Task<InstallerEnvironmentInfo> GetEnvironmentInfoAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InstallerPackageStatus>> GetPackageStatusesAsync(IEnumerable<string> packageIds, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InstallerOperationResult>> RunPackageOperationAsync(string packageId, InstallerPackageAction action, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InstallerOperationResult>> InstallPackagesAsync(IEnumerable<string> packageIds, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InstallerOperationResult>> UpgradePackagesAsync(IEnumerable<string> packageIds, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InstallerOperationResult>> UninstallPackagesAsync(IEnumerable<string> packageIds, CancellationToken cancellationToken = default);
}
