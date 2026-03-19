using MultiTool.Core.Enums;
using MultiTool.Core.Models;
using MultiTool.Infrastructure.Windows.Hotkeys;
using FluentAssertions;

namespace MultiTool.Infrastructure.Windows.Tests;

public sealed class WindowsHotkeyServiceTests
{
    [Fact]
    public void GetToggleHotkeyModifiers_ShouldIncludeAltAndControl_WhenModifierVariantsEnabled()
    {
        var modifiers = WindowsHotkeyService.GetToggleHotkeyModifiers(
            new HotkeySettings
            {
                AllowModifierVariants = true,
            });

        modifiers.Should().Equal(HotkeyModifiers.None, HotkeyModifiers.Alt, HotkeyModifiers.Control);
    }

    [Fact]
    public void GetToggleHotkeyModifiers_ShouldOnlyUseBaseHotkey_WhenModifierVariantsDisabled()
    {
        var modifiers = WindowsHotkeyService.GetToggleHotkeyModifiers(
            new HotkeySettings
            {
                AllowModifierVariants = false,
            });

        modifiers.Should().Equal(HotkeyModifiers.None);
    }
}
