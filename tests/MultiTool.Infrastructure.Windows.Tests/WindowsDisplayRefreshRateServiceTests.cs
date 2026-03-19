using MultiTool.Core.Models;
using MultiTool.Infrastructure.Windows.Tools;
using FluentAssertions;

namespace MultiTool.Infrastructure.Windows.Tests;

public sealed class WindowsDisplayRefreshRateServiceTests
{
    [Fact]
    public async Task GetRecommendationsAsync_ShouldReturnInjectedRecommendations()
    {
        var service = new WindowsDisplayRefreshRateService(
            () =>
            [
                new DisplayRefreshRecommendation("\\\\.\\DISPLAY1", "Primary Monitor", "2560 x 1440", 60, 144, true, "Ready."),
            ],
            () => []);

        var recommendations = await service.GetRecommendationsAsync();

        recommendations.Should().ContainSingle();
        recommendations[0].RecommendedFrequency.Should().Be(144);
    }

    [Fact]
    public async Task ApplyRecommendedAsync_ShouldReturnInjectedResults()
    {
        var service = new WindowsDisplayRefreshRateService(
            () => [],
            () =>
            [
                new DisplayRefreshApplyResult("\\\\.\\DISPLAY1", "Primary Monitor", true, true, "Switched to 144 Hz."),
            ]);

        var results = await service.ApplyRecommendedAsync();

        results.Should().ContainSingle();
        results[0].Succeeded.Should().BeTrue();
        results[0].Changed.Should().BeTrue();
    }
}
