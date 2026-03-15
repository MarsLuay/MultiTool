using AutoClicker.Core.Models;

namespace AutoClicker.Core.Services;

public interface IDriverUpdateService
{
    Task<DriverUpdateScanResult> ScanAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DriverUpdateInstallResult>> InstallAsync(IEnumerable<string> updateIds, CancellationToken cancellationToken = default);
}
