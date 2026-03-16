using System.Diagnostics;
using AutoClicker.Infrastructure.Windows.Tools;
using FluentAssertions;

namespace AutoClicker.Infrastructure.Windows.Tests;

public sealed class WindowsOneDriveRemovalServiceTests
{
    [Fact]
    public void GetStatus_ShouldReturnProbeState()
    {
        var service = new WindowsOneDriveRemovalService(
            () => new OneDriveEnvironmentStatus(true, @"C:\Windows\System32\OneDriveSetup.exe", "OneDrive appears to be installed."),
            (_, _) => Task.FromResult(new OneDriveCommandResult(0, string.Empty, string.Empty)),
            () => true);

        var status = service.GetStatus();

        status.IsInstalled.Should().BeTrue();
        status.Message.Should().Be("OneDrive appears to be installed. The system OneDrive disable policy is active.");
    }

    [Fact]
    public async Task RemoveAsync_ShouldRunBundledUninstallerAndReportSuccess()
    {
        var environments = new Queue<OneDriveEnvironmentStatus>(
        [
            new OneDriveEnvironmentStatus(true, @"C:\Windows\System32\OneDriveSetup.exe", "Installed."),
            new OneDriveEnvironmentStatus(false, @"C:\Windows\System32\OneDriveSetup.exe", "Removed."),
        ]);
        ProcessStartInfo? capturedStartInfo = null;
        var service = new WindowsOneDriveRemovalService(
            () => environments.Dequeue(),
            (startInfo, _) =>
            {
                capturedStartInfo = startInfo;
                return Task.FromResult(new OneDriveCommandResult(0, "removed", string.Empty));
            },
            () => true);

        var result = await service.RemoveAsync();

        result.Succeeded.Should().BeTrue();
        result.Changed.Should().BeTrue();
        result.Message.Should().Be("OneDrive removal completed. OneDrive is no longer detected.");
        capturedStartInfo.Should().NotBeNull();
        capturedStartInfo!.FileName.Should().Be(@"C:\Windows\System32\OneDriveSetup.exe");
        capturedStartInfo.Arguments.Should().Be("/uninstall");
    }

    [Fact]
    public async Task RemoveAsync_ShouldReturnNoOpWhenAlreadyRemoved()
    {
        var service = new WindowsOneDriveRemovalService(
            () => new OneDriveEnvironmentStatus(false, @"C:\Windows\System32\OneDriveSetup.exe", "Already removed."),
            (_, _) => throw new InvalidOperationException("Command runner should not be called."),
            () => true);

        var result = await service.RemoveAsync();

        result.Succeeded.Should().BeTrue();
        result.Changed.Should().BeFalse();
        result.Message.Should().Be("OneDrive is already removed or not detected. The system OneDrive disable policy is already active.");
    }

    [Fact]
    public async Task RemoveAsync_ShouldApplyPolicyWhenAlreadyRemovedButPolicyIsMissing()
    {
        var service = new WindowsOneDriveRemovalService(
            () => new OneDriveEnvironmentStatus(false, @"C:\Windows\System32\OneDriveSetup.exe", "Already removed."),
            (_, _) => throw new InvalidOperationException("Command runner should not be called."),
            () => false,
            _ => Task.FromResult(new OneDrivePolicyApplyOutcome(true, true, "Applied policy.")));

        var result = await service.RemoveAsync();

        result.Succeeded.Should().BeTrue();
        result.Changed.Should().BeTrue();
        result.Message.Should().Be("OneDrive is not detected. Applied the system policy that disables OneDrive file sync.");
    }

    [Fact]
    public async Task RemoveAsync_ShouldMentionPolicyFailureWhenUninstallSucceeds()
    {
        var environments = new Queue<OneDriveEnvironmentStatus>(
        [
            new OneDriveEnvironmentStatus(true, @"C:\Windows\System32\OneDriveSetup.exe", "Installed."),
            new OneDriveEnvironmentStatus(false, @"C:\Windows\System32\OneDriveSetup.exe", "Removed."),
        ]);

        var service = new WindowsOneDriveRemovalService(
            () => environments.Dequeue(),
            (_, _) => Task.FromResult(new OneDriveCommandResult(0, "removed", string.Empty)),
            () => false,
            _ => Task.FromResult(new OneDrivePolicyApplyOutcome(false, false, "Administrator permission was canceled.")));

        var result = await service.RemoveAsync();

        result.Succeeded.Should().BeTrue();
        result.Changed.Should().BeTrue();
        result.Message.Should().Be("OneDrive removal completed, but the disable policy could not be applied: Administrator permission was canceled.");
    }
}
