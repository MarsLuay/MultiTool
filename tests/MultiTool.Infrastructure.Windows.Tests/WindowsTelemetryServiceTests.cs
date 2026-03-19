using FluentAssertions;
using MultiTool.Infrastructure.Windows.Tools;

namespace MultiTool.Infrastructure.Windows.Tests;

public sealed class WindowsTelemetryServiceTests
{
    [Fact]
    public void GetStatus_WhenPolicyOverrideIsClearedAndServicesUseDefaultModes_ShouldReportDefaultsRestored()
    {
        var service = new WindowsTelemetryService(
            commandRunner: (_, _) => throw new InvalidOperationException("Command runner should not be called."),
            readAllowTelemetryPolicy: () => -1,
            getServiceState: serviceName => serviceName switch
            {
                "DiagTrack" => new WindowsTelemetryService.ServiceState(true, false, "auto"),
                "dmwappushservice" => new WindowsTelemetryService.ServiceState(true, false, "demand"),
                _ => new WindowsTelemetryService.ServiceState(false, false, string.Empty),
            });

        var status = service.GetStatus();

        status.IsReduced.Should().BeFalse();
        status.Message.Should().Contain("defaults are restored");
        status.Message.Should().NotContain("AllowTelemetry policy is not set to 0");
    }

    [Fact]
    public void GetStatus_WhenPolicyOverrideIsClearedButAServiceIsStillDisabled_ShouldReportHardeningNotFullyApplied()
    {
        var service = new WindowsTelemetryService(
            commandRunner: (_, _) => throw new InvalidOperationException("Command runner should not be called."),
            readAllowTelemetryPolicy: () => -1,
            getServiceState: serviceName => serviceName switch
            {
                "DiagTrack" => new WindowsTelemetryService.ServiceState(true, false, "disabled"),
                "dmwappushservice" => new WindowsTelemetryService.ServiceState(true, false, "demand"),
                _ => new WindowsTelemetryService.ServiceState(false, false, string.Empty),
            });

        var status = service.GetStatus();

        status.IsReduced.Should().BeFalse();
        status.Message.Should().Contain("AllowTelemetry policy is not set to 0");
    }
}
