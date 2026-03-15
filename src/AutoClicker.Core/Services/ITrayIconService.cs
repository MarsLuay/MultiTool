namespace AutoClicker.Core.Services;

public interface ITrayIconService : IDisposable
{
    event EventHandler? ShowRequested;

    event EventHandler? HideRequested;

    event EventHandler? ExitRequested;

    void Initialize();

    void SetRunningState(bool isRunning);
}
