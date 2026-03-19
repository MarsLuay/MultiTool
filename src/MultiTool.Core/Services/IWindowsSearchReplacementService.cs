using MultiTool.Core.Models;

namespace MultiTool.Core.Services;

public interface IWindowsSearchReplacementService
{
    WindowsSearchReplacementStatus GetStatus();

    Task<WindowsSearchReplacementResult> ApplyAsync(CancellationToken cancellationToken = default);

    Task<WindowsSearchReplacementResult> RestoreAsync(CancellationToken cancellationToken = default);
}
