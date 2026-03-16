using AutoClicker.Core.Enums;
using AutoClicker.Core.Models;
using AutoClicker.Core.Validation;
using FluentAssertions;

namespace AutoClicker.Core.Tests;

public sealed class SettingsValidatorTests
{
    private readonly SettingsValidator validator = new();

    [Fact]
    public void ValidateClickSettings_ShouldRejectZeroInterval()
    {
        var settings = new ClickSettings
        {
            Hours = 0,
            Minutes = 0,
            Seconds = 0,
            Milliseconds = 0,
        };

        var result = validator.ValidateClickSettings(settings);

        result.IsValid.Should().BeFalse();
        result.Summary.Should().Contain("greater than zero");
    }

    [Fact]
    public void ValidateHotkeys_ShouldRequireToggleBinding()
    {
        var settings = new HotkeySettings
        {
            Toggle = new HotkeyBinding(0, string.Empty),
        };

        var result = validator.ValidateHotkeys(settings);

        result.IsValid.Should().BeFalse();
        result.Summary.Should().Contain("Toggle hotkey is required");
    }

    [Fact]
    public void ValidateClickSettings_ShouldRequireRepeatCountInCountMode()
    {
        var settings = new ClickSettings
        {
            Milliseconds = 1,
            RepeatMode = RepeatMode.Count,
            RepeatCount = 0,
        };

        var result = validator.ValidateClickSettings(settings);

        result.IsValid.Should().BeFalse();
        result.Summary.Should().Contain("Repeat count");
    }

    [Fact]
    public void ValidateClickSettings_ShouldAllowHoldWithoutInterval()
    {
        var settings = new ClickSettings
        {
            Milliseconds = 0,
            ClickType = ClickKind.Hold,
            RepeatMode = RepeatMode.Count,
            RepeatCount = 0,
        };

        var result = validator.ValidateClickSettings(settings);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateClickSettings_ShouldRequireCustomKeyWhenCustomIsSelected()
    {
        var settings = new ClickSettings
        {
            Milliseconds = 1,
            MouseButton = ClickMouseButton.Custom,
        };

        var result = validator.ValidateClickSettings(settings);

        result.IsValid.Should().BeFalse();
        result.Summary.Should().Contain("custom input");
    }

    [Fact]
    public void ValidateClickSettings_ShouldAllowCustomMouseButtonWhenCustomIsSelected()
    {
        var settings = new ClickSettings
        {
            Milliseconds = 1,
            MouseButton = ClickMouseButton.Custom,
            CustomInputKind = CustomInputKind.MouseButton,
            CustomMouseButton = ClickMouseButton.XButton1,
            CustomKeyDisplayName = "Mouse Button 4",
        };

        var result = validator.ValidateClickSettings(settings);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateScreenshot_ShouldRequireKeyboardHotkey()
    {
        var settings = new ScreenshotSettings
        {
            CaptureHotkey = new HotkeyBinding(
                virtualKey: 0,
                displayName: "Mouse Button 4",
                inputKind: HotkeyInputKind.MouseButton,
                mouseButton: ClickMouseButton.XButton1),
        };

        var result = validator.ValidateScreenshot(settings);

        result.IsValid.Should().BeFalse();
        result.Summary.Should().Contain("keyboard key");
    }

    [Fact]
    public void ValidateMacro_ShouldRequireKeyboardHotkey()
    {
        var settings = new MacroSettings
        {
            PlayHotkey = new HotkeyBinding(
                virtualKey: 0,
                displayName: "Mouse Button 4",
                inputKind: HotkeyInputKind.MouseButton,
                mouseButton: ClickMouseButton.XButton1),
        };

        var result = validator.ValidateMacro(settings);

        result.IsValid.Should().BeFalse();
        result.Summary.Should().Contain("keyboard key");
    }

    [Fact]
    public void ValidateMacro_ShouldRejectAssignedHotkeyConflictWithPrimaryMacroHotkey()
    {
        var settings = new MacroSettings
        {
            PlayHotkey = new HotkeyBinding(0x70, "F1"),
            RecordHotkey = new HotkeyBinding(0x71, "F2"),
            AssignedHotkeys =
            [
                new MacroHotkeyAssignment
                {
                    Id = "farm",
                    MacroDisplayName = "Farm",
                    MacroFilePath = @"C:\Macros\Farm.acmacro.json",
                    Hotkey = new HotkeyBinding(0x70, "F1"),
                    PlaybackMode = MacroHotkeyPlaybackMode.ToggleRepeat,
                    IsEnabled = true,
                },
            ],
        };

        var result = validator.ValidateMacro(settings);

        result.IsValid.Should().BeFalse();
        result.Summary.Should().Contain("conflicts");
    }

    [Fact]
    public void ValidateMacro_ShouldAllowDistinctAssignedSavedMacroHotkeys()
    {
        var settings = new MacroSettings
        {
            PlayHotkey = new HotkeyBinding(0x70, "F1"),
            RecordHotkey = new HotkeyBinding(0x71, "F2"),
            AssignedHotkeys =
            [
                new MacroHotkeyAssignment
                {
                    Id = "farm",
                    MacroDisplayName = "Farm",
                    MacroFilePath = @"C:\Macros\Farm.acmacro.json",
                    Hotkey = new HotkeyBinding(0x72, "F3"),
                    PlaybackMode = MacroHotkeyPlaybackMode.PlayOnce,
                    IsEnabled = true,
                },
                new MacroHotkeyAssignment
                {
                    Id = "mine",
                    MacroDisplayName = "Mine",
                    MacroFilePath = @"C:\Macros\Mine.acmacro.json",
                    Hotkey = new HotkeyBinding(0x73, "F4"),
                    PlaybackMode = MacroHotkeyPlaybackMode.ToggleRepeat,
                    IsEnabled = true,
                },
            ],
        };

        var result = validator.ValidateMacro(settings);

        result.IsValid.Should().BeTrue();
    }
}
