using MultiTool.Core.Models;

namespace MultiTool.Core.Services;

public interface IMouseSensitivityService
{
    IReadOnlyList<int> GetSupportedLevels();

    MouseSensitivityStatus GetStatus();

    Task<MouseSensitivityApplyResult> ApplyAsync(int level, CancellationToken cancellationToken = default);
}
