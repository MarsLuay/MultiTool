using MultiTool.Core.Models;
using FluentAssertions;

namespace MultiTool.Core.Tests;

public sealed class ClickSettingsTests
{
    [Fact]
    public void GetNextInterval_ShouldReturnBaseInterval_WhenRandomTimingIsDisabled()
    {
        var settings = new ClickSettings
        {
            Seconds = 1,
            Milliseconds = 250,
            IsRandomTimingEnabled = false,
            RandomTimingVarianceMilliseconds = 200,
        };

        var interval = settings.GetNextInterval(new Random(1234));

        interval.Should().Be(TimeSpan.FromMilliseconds(1250));
    }

    [Fact]
    public void GetNextInterval_ShouldStayWithinVarianceRange_WhenRandomTimingIsEnabled()
    {
        var settings = new ClickSettings
        {
            Milliseconds = 100,
            IsRandomTimingEnabled = true,
            RandomTimingVarianceMilliseconds = 40,
        };

        var random = new Random(1234);
        var samples = Enumerable.Range(0, 25)
            .Select(_ => settings.GetNextInterval(random))
            .ToArray();

        samples.Should().OnlyContain(sample =>
            sample >= TimeSpan.FromMilliseconds(60)
            && sample <= TimeSpan.FromMilliseconds(140));
        samples.Should().Contain(sample => sample != TimeSpan.FromMilliseconds(100));
    }
}
