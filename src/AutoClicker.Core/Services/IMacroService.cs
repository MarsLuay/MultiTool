using AutoClicker.Core.Models;

namespace AutoClicker.Core.Services;

public interface IMacroService : IDisposable
{
    bool IsRecording { get; }

    bool IsPlaying { get; }

    RecordedMacro? CurrentMacro { get; }

    void StartRecording(string? name = null, bool recordMouseMovement = true);

    RecordedMacro StopRecording();

    void SetCurrentMacro(RecordedMacro macro);

    Task PlayAsync(int repeatCount, CancellationToken cancellationToken = default);

    void Clear();
}
