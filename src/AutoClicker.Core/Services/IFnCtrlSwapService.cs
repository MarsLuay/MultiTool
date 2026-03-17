using AutoClicker.Core.Models;

namespace AutoClicker.Core.Services;

public interface IFnCtrlSwapService
{
    FnCtrlSwapStatus GetStatus();

    Task<FnCtrlSwapResult> ToggleAsync(CancellationToken cancellationToken = default);
}
