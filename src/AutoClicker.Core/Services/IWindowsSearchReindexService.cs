using AutoClicker.Core.Models;

namespace AutoClicker.Core.Services;

public interface IWindowsSearchReindexService
{
    WindowsSearchReindexStatus GetStatus();

    Task<WindowsSearchReindexResult> ReindexAsync(CancellationToken cancellationToken = default);
}