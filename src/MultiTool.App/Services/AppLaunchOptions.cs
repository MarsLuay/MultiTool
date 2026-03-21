namespace MultiTool.App.Services;

public sealed class AppLaunchOptions
{
    public bool IsStartupLaunch { get; init; }

    public bool IsMemoryLoggingEnabled { get; init; }

    public bool IsTabPerformanceLoggingEnabled { get; init; }

    public static AppLaunchOptions FromArgs(IEnumerable<string> args)
    {
        var launchArguments = args?.ToArray() ?? [];

        return new AppLaunchOptions
        {
            IsStartupLaunch = launchArguments.Any(argument => string.Equals(argument, "--startup-launch", StringComparison.OrdinalIgnoreCase)),
            IsMemoryLoggingEnabled = launchArguments.Any(argument => string.Equals(argument, "--log-memory", StringComparison.OrdinalIgnoreCase)),
            IsTabPerformanceLoggingEnabled = launchArguments.Any(argument => string.Equals(argument, "--trace-tabs", StringComparison.OrdinalIgnoreCase)),
        };
    }
}
