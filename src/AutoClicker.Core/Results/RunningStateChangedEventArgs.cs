namespace AutoClicker.Core.Results;

public sealed class RunningStateChangedEventArgs : EventArgs
{
    public RunningStateChangedEventArgs(bool isRunning)
    {
        IsRunning = isRunning;
    }

    public bool IsRunning { get; }
}
