using AutoClicker.Core.Models;

namespace AutoClicker.Core.Services;

public interface IHardwareInventoryService
{
    Task<HardwareInventoryReport> GetReportAsync(CancellationToken cancellationToken = default);
}
