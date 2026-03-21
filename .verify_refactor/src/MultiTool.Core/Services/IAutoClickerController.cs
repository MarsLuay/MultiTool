using MultiTool.Core.Models;
using MultiTool.Core.Results;

namespace MultiTool.Core.Services;

public interface IAutoClickerController
{
    bool IsRunning { get; }

    event EventHandler<RunningStateChangedEventArgs>? RunningStateChanged;

    Task StartAsync(ClickSettings settings, CancellationToken cancellationToken = default);

    Task StopAsync(CancellationToken cancellationToken = default);

    Task ToggleAsync(ClickSettings settings, CancellationToken cancellationToken = default);

    void SuspendFor(TimeSpan duration);
}
