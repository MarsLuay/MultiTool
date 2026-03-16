using AutoClicker.Core.Models;

namespace AutoClicker.Core.Services;

public interface IWindows11EeaMediaService
{
    event EventHandler<string>? StatusChanged;

    Task<Windows11EeaMediaPreparationResult> PrepareAsync(CancellationToken cancellationToken = default);
}
