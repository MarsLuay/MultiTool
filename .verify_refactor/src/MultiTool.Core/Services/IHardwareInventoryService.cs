using MultiTool.Core.Models;

namespace MultiTool.Core.Services;

public interface IHardwareInventoryService
{
    Task<HardwareInventoryReport> GetReportAsync(CancellationToken cancellationToken = default);
}
