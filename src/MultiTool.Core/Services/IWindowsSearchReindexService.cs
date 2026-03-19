using MultiTool.Core.Models;

namespace MultiTool.Core.Services;

public interface IWindowsSearchReindexService
{
    WindowsSearchReindexStatus GetStatus();

    Task<WindowsSearchReindexResult> ReindexAsync(CancellationToken cancellationToken = default);
}