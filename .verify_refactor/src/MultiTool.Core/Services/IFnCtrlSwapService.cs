using MultiTool.Core.Models;

namespace MultiTool.Core.Services;

public interface IFnCtrlSwapService
{
    FnCtrlSwapStatus GetStatus();

    Task<FnCtrlSwapResult> ToggleAsync(CancellationToken cancellationToken = default);
}
