using AutoClicker.Core.Models;
using AutoClicker.Core.Results;

namespace AutoClicker.Core.Services;

public interface IAutoClickerController
{
    bool IsRunning { get; }

    event EventHandler<RunningStateChangedEventArgs>? RunningStateChanged;

    Task StartAsync(ClickSettings settings, CancellationToken cancellationToken = default);

    Task StopAsync(CancellationToken cancellationToken = default);

    Task ToggleAsync(ClickSettings settings, CancellationToken cancellationToken = default);
}
