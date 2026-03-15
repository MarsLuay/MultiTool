using AutoClicker.Core.Models;

namespace AutoClicker.Core.Services;

public interface IDisplayRefreshRateService
{
    Task<IReadOnlyList<DisplayRefreshRecommendation>> GetRecommendationsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DisplayRefreshApplyResult>> ApplyRecommendedAsync(CancellationToken cancellationToken = default);
}
