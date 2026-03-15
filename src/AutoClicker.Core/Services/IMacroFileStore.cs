using AutoClicker.Core.Models;

namespace AutoClicker.Core.Services;

public interface IMacroFileStore
{
    Task SaveAsync(string filePath, RecordedMacro macro, CancellationToken cancellationToken = default);

    Task<RecordedMacro> LoadAsync(string filePath, CancellationToken cancellationToken = default);
}
