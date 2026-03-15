using System.IO;
using System.Text.Json;
using AutoClicker.Core.Models;
using AutoClicker.Core.Services;

namespace AutoClicker.Infrastructure.Windows.Persistence;

public sealed class JsonMacroFileStore : IMacroFileStore
{
    private readonly JsonSerializerOptions serializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };

    public async Task SaveAsync(string filePath, RecordedMacro macro, CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var stream = File.Create(filePath);
        await JsonSerializer.SerializeAsync(stream, macro, serializerOptions, cancellationToken).ConfigureAwait(false);
    }

    public async Task<RecordedMacro> LoadAsync(string filePath, CancellationToken cancellationToken = default)
    {
        await using var stream = File.OpenRead(filePath);
        var macro = await JsonSerializer.DeserializeAsync<RecordedMacro>(stream, serializerOptions, cancellationToken).ConfigureAwait(false);
        return macro ?? throw new InvalidOperationException("The macro file did not contain a valid macro payload.");
    }
}
