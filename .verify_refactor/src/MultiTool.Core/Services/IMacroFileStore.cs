using MultiTool.Core.Models;

namespace MultiTool.Core.Services;

public interface IMacroFileStore
{
    Task SaveAsync(string filePath, RecordedMacro macro, CancellationToken cancellationToken = default);

    Task<RecordedMacro> LoadAsync(string filePath, CancellationToken cancellationToken = default);
}
