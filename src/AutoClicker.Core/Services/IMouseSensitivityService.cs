using AutoClicker.Core.Models;

namespace AutoClicker.Core.Services;

public interface IMouseSensitivityService
{
    IReadOnlyList<int> GetSupportedLevels();

    MouseSensitivityStatus GetStatus();

    Task<MouseSensitivityApplyResult> ApplyAsync(int level, CancellationToken cancellationToken = default);
}
