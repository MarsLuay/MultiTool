using AutoClicker.Core.Models;
using AutoClicker.Infrastructure.Windows.Tools;
using FluentAssertions;

namespace AutoClicker.Infrastructure.Windows.Tests;

public sealed class WindowsDriverUpdateServiceTests
{
    [Fact]
    public async Task ScanAsync_ShouldReturnHardwareAndUpdates()
    {
        var service = new WindowsDriverUpdateService(
            () =>
            [
                new DriverHardwareInfo("GPU", "NVIDIA", "NVIDIA", "1.0.0", "Display", "PCI\\VEN_10DE"),
            ],
            () =>
            [
                new DriverUpdateCandidate("update-1", "NVIDIA Display Driver", "RTX 4080", "NVIDIA", "Display", "2026-03-01", "Recommended display update.", false, true),
            ],
            _ => []);

        var result = await service.ScanAsync();

        result.Warnings.Should().BeEmpty();
        result.Hardware.Should().ContainSingle();
        result.Updates.Should().ContainSingle();
        result.Updates[0].Title.Should().Be("NVIDIA Display Driver");
        result.Updates[0].RequiresUserInput.Should().BeTrue();
    }

    [Fact]
    public async Task ScanAsync_ShouldCaptureWarningsWhenSourcesFail()
    {
        var service = new WindowsDriverUpdateService(
            () => throw new InvalidOperationException("WMI unavailable"),
            () => throw new InvalidOperationException("Windows Update unavailable"),
            _ => []);

        var result = await service.ScanAsync();

        result.Hardware.Should().BeEmpty();
        result.Updates.Should().BeEmpty();
        result.Warnings.Should().Contain(message => message.Contains("WMI unavailable", StringComparison.Ordinal));
        result.Warnings.Should().Contain(message => message.Contains("Windows Update unavailable", StringComparison.Ordinal));
    }

    [Fact]
    public async Task InstallAsync_ShouldWrapInstallerResults()
    {
        var service = new WindowsDriverUpdateService(
            () => [],
            () => [],
            updateIds =>
            [
                .. updateIds.Select(
                    static updateId => new DriverUpdateInstallResult(updateId, $"Title {updateId}", true, true, false, false, "Installed.")),
            ]);

        var result = await service.InstallAsync(["driver-a", "driver-b"]);

        result.Should().HaveCount(2);
        result.Should().OnlyContain(item => item.Succeeded && item.Changed);
    }

    [Fact]
    public async Task InstallAsync_ShouldPreserveInteractiveInstallRequirement()
    {
        var service = new WindowsDriverUpdateService(
            () => [],
            () => [],
            updateIds =>
            [
                .. updateIds.Select(
                    static updateId => new DriverUpdateInstallResult(updateId, $"Title {updateId}", false, false, false, true, "Needs Windows Update UI.")),
            ]);

        var result = await service.InstallAsync(["driver-a"]);

        result.Should().ContainSingle();
        result[0].RequiresUserInput.Should().BeTrue();
        result[0].Succeeded.Should().BeFalse();
    }
}
