using AutoClicker.Infrastructure.Windows.Tools;
using FluentAssertions;

namespace AutoClicker.Infrastructure.Windows.Tests;

public sealed class WindowsMouseSensitivityServiceTests
{
    [Fact]
    public void GetStatus_ShouldReportCurrentMouseSensitivity()
    {
        var service = new WindowsMouseSensitivityService(
            () => 12,
            static (_, _) => Task.CompletedTask);

        var status = service.GetStatus();

        status.CurrentLevel.Should().Be(12);
        status.Message.Should().Contain("12/20");
        status.Message.Should().Contain("pointer speed");
    }

    [Fact]
    public async Task ApplyAsync_ShouldRejectUnsupportedSensitivityLevel()
    {
        var writerCalled = false;
        var service = new WindowsMouseSensitivityService(
            () => 10,
            (_, _) =>
            {
                writerCalled = true;
                return Task.CompletedTask;
            });

        var result = await service.ApplyAsync(25);

        result.Succeeded.Should().BeFalse();
        result.Changed.Should().BeFalse();
        result.Message.Should().Contain("Unsupported mouse sensitivity");
        writerCalled.Should().BeFalse();
    }

    [Fact]
    public async Task ApplyAsync_ShouldSkipWrite_WhenSensitivityIsAlreadyConfigured()
    {
        var writerCalled = false;
        var service = new WindowsMouseSensitivityService(
            () => 8,
            (_, _) =>
            {
                writerCalled = true;
                return Task.CompletedTask;
            });

        var result = await service.ApplyAsync(8);

        result.Succeeded.Should().BeTrue();
        result.Changed.Should().BeFalse();
        result.AppliedLevel.Should().Be(8);
        writerCalled.Should().BeFalse();
    }

    [Fact]
    public async Task ApplyAsync_ShouldWriteSensitivity_WhenLevelIsSupported()
    {
        var writtenLevel = 0;
        var service = new WindowsMouseSensitivityService(
            () => 10,
            (level, _) =>
            {
                writtenLevel = level;
                return Task.CompletedTask;
            });

        var result = await service.ApplyAsync(14);

        result.Succeeded.Should().BeTrue();
        result.Changed.Should().BeTrue();
        result.AppliedLevel.Should().Be(14);
        writtenLevel.Should().Be(14);
    }
}
