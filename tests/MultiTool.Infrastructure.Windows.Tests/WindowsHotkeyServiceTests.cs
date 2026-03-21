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

    [Fact]
    public void GetToggleHotkeyModifiers_ShouldUseExplicitModifiers_WhenBindingHasThem()
    {
        var modifiers = WindowsHotkeyService.GetToggleHotkeyModifiers(
            new HotkeySettings
            {
                Toggle = new HotkeyBinding(0x43, "Ctrl + C", HotkeyInputKind.Keyboard, ClickMouseButton.Left, HotkeyModifiers.Control),
                AllowModifierVariants = true,
            });

        modifiers.Should().Equal(HotkeyModifiers.Control);
    }

    [Fact]
    public void GetForceStopHotkeyModifiers_ShouldAppendShiftToExplicitModifierCombo()
    {
        var modifiers = WindowsHotkeyService.GetForceStopHotkeyModifiers(
            new HotkeySettings
            {
                Toggle = new HotkeyBinding(0x43, "Ctrl + C", HotkeyInputKind.Keyboard, ClickMouseButton.Left, HotkeyModifiers.Control),
            });

        modifiers.Should().Equal(HotkeyModifiers.Control | HotkeyModifiers.Shift);
    }

    [Fact]
    public void ShouldUseLowLevelOverride_ShouldOnlyApplyToExplicitModifierBindings()
    {
        var binding = new HotkeyBinding(0x43, "Ctrl + C", HotkeyInputKind.Keyboard, ClickMouseButton.Left, HotkeyModifiers.Control);
        var plainBinding = new HotkeyBinding(0x43, "C");
        var settings = new HotkeySettings
        {
            OverrideApplicationShortcuts = true,
        };

        WindowsHotkeyService.ShouldUseLowLevelOverride(settings, binding, HotkeyModifiers.Control).Should().BeTrue();
        WindowsHotkeyService.ShouldUseLowLevelOverride(settings, plainBinding, HotkeyModifiers.Control).Should().BeFalse();
        WindowsHotkeyService.ShouldUseLowLevelOverride(settings, binding, HotkeyModifiers.None).Should().BeFalse();
    }
}
