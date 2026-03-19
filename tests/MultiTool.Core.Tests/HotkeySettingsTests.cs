using MultiTool.Core.Models;
using FluentAssertions;

namespace MultiTool.Core.Tests;

public sealed class HotkeySettingsTests
{
    [Fact]
    public void NewSettings_ShouldEnableModifierVariantsByDefault()
    {
        var settings = new HotkeySettings();

        settings.AllowModifierVariants.Should().BeTrue();
    }
}
