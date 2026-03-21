using FluentAssertions;
using MultiTool.App.Services;

namespace MultiTool.App.Tests;

public sealed class AppLaunchOptionsTests
{
    [Fact]
    public void FromArgs_ShouldDetectKnownFlags()
    {
        var result = AppLaunchOptions.FromArgs(["--startup-launch", "--log-memory", "--trace-tabs"]);

        result.IsStartupLaunch.Should().BeTrue();
        result.IsMemoryLoggingEnabled.Should().BeTrue();
        result.IsTabPerformanceLoggingEnabled.Should().BeTrue();
    }

    [Fact]
    public void FromArgs_ShouldIgnoreUnknownFlags_AndTreatKnownFlagsCaseInsensitively()
    {
        var result = AppLaunchOptions.FromArgs(["--unknown", "--LOG-MEMORY", "--TRACE-TABS"]);

        result.IsStartupLaunch.Should().BeFalse();
        result.IsMemoryLoggingEnabled.Should().BeTrue();
        result.IsTabPerformanceLoggingEnabled.Should().BeTrue();
    }
}
