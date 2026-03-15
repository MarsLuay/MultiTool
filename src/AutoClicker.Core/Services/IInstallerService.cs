using AutoClicker.Core.Models;

namespace AutoClicker.Core.Services;

public interface IInstallerService
{
    IReadOnlyList<InstallerCatalogItem> GetCatalog();

    IReadOnlyList<InstallerCatalogItem> GetCleanupCatalog();

    Task<InstallerEnvironmentInfo> GetEnvironmentInfoAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InstallerPackageStatus>> GetPackageStatusesAsync(IEnumerable<string> packageIds, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InstallerOperationResult>> InstallPackagesAsync(IEnumerable<string> packageIds, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InstallerOperationResult>> UpgradePackagesAsync(IEnumerable<string> packageIds, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InstallerOperationResult>> UninstallPackagesAsync(IEnumerable<string> packageIds, CancellationToken cancellationToken = default);
}
