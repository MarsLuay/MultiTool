using MultiTool.Core.Models;

namespace MultiTool.Core.Services;

public interface IDriverUpdateService
{
    Task<DriverUpdateScanResult> ScanAsync(
        IReadOnlyList<DriverHardwareInfo>? hardwareInventory = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DriverUpdateInstallResult>> InstallAsync(IEnumerable<string> updateIds, CancellationToken cancellationToken = default);
}
