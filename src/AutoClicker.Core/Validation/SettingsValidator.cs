using AutoClicker.Core.Models;
using AutoClicker.Core.Results;

namespace AutoClicker.Core.Validation;

public sealed class SettingsValidator
{
    public ValidationResult Validate(AppSettings settings)
    {
        var issues = new List<ValidationIssue>();
        issues.AddRange(ValidateClickSettings(settings.Clicker).Issues);
        issues.AddRange(ValidateHotkeys(settings.Hotkeys).Issues);
        issues.AddRange(ValidateScreenshot(settings.Screenshot).Issues);
        issues.AddRange(ValidateMacro(settings.Macro).Issues);
        return new ValidationResult(issues);
    }

    public ValidationResult ValidateClickSettings(ClickSettings settings)
    {
        var issues = new List<ValidationIssue>();
        var isHoldMode = settings.ClickType == Enums.ClickKind.Hold;

        if (settings.Hours < 0)
        {
            issues.Add(new ValidationIssue(nameof(settings.Hours), "Hours cannot be negative."));
        }

        if (settings.Minutes < 0)
        {
            issues.Add(new ValidationIssue(nameof(settings.Minutes), "Minutes cannot be negative."));
        }

        if (settings.Seconds < 0)
        {
            issues.Add(new ValidationIssue(nameof(settings.Seconds), "Seconds cannot be negative."));
        }

        if (settings.Milliseconds < 0)
        {
            issues.Add(new ValidationIssue(nameof(settings.Milliseconds), "Milliseconds cannot be negative."));
        }

        if (!isHoldMode && settings.GetInterval() <= TimeSpan.Zero)
        {
            issues.Add(new ValidationIssue(nameof(settings.Milliseconds), "Click interval must be greater than zero."));
        }

        if (!isHoldMode && settings.RepeatMode == Enums.RepeatMode.Count && settings.RepeatCount <= 0)
        {
            issues.Add(new ValidationIssue(nameof(settings.RepeatCount), "Repeat count must be greater than zero when count mode is selected."));
        }

        if (settings.MouseButton == Enums.ClickMouseButton.Custom
            && !HasValidCustomInput(settings))
        {
            issues.Add(new ValidationIssue(nameof(settings.CustomKeyDisplayName), "A custom input is required when Custom is selected."));
        }

        return new ValidationResult(issues);
    }

    public ValidationResult ValidateHotkeys(HotkeySettings settings)
    {
        var issues = new List<ValidationIssue>();

        if (!HasValidHotkeyBinding(settings.Toggle))
        {
            issues.Add(new ValidationIssue(nameof(settings.Toggle), "Toggle hotkey is required."));
        }

        return new ValidationResult(issues);
    }

    public ValidationResult ValidateScreenshot(ScreenshotSettings settings)
    {
        var issues = new List<ValidationIssue>();

        if (!HasValidHotkeyBinding(settings.CaptureHotkey))
        {
            issues.Add(new ValidationIssue(nameof(settings.CaptureHotkey), "Screenshot hotkey is required."));
        }

        if (settings.CaptureHotkey.InputKind != Enums.HotkeyInputKind.Keyboard)
        {
            issues.Add(new ValidationIssue(nameof(settings.CaptureHotkey), "Screenshot hotkey must be a keyboard key."));
        }

        if (string.IsNullOrWhiteSpace(settings.SaveFolderPath))
        {
            issues.Add(new ValidationIssue(nameof(settings.SaveFolderPath), "Screenshot folder is required."));
        }

        if (string.IsNullOrWhiteSpace(settings.FilePrefix))
        {
            issues.Add(new ValidationIssue(nameof(settings.FilePrefix), "Screenshot filename prefix is required."));
        }

        return new ValidationResult(issues);
    }

    public ValidationResult ValidateMacro(MacroSettings settings)
    {
        var issues = new List<ValidationIssue>();

        if (!HasValidHotkeyBinding(settings.PlayHotkey))
        {
            issues.Add(new ValidationIssue(nameof(settings.PlayHotkey), "Macro hotkey is required."));
        }

        if (settings.PlayHotkey.InputKind != Enums.HotkeyInputKind.Keyboard)
        {
            issues.Add(new ValidationIssue(nameof(settings.PlayHotkey), "Macro hotkey must be a keyboard key."));
        }

        if (!HasValidHotkeyBinding(settings.RecordHotkey))
        {
            issues.Add(new ValidationIssue(nameof(settings.RecordHotkey), "Macro record hotkey is required."));
        }

        if (settings.RecordHotkey.InputKind != Enums.HotkeyInputKind.Keyboard)
        {
            issues.Add(new ValidationIssue(nameof(settings.RecordHotkey), "Macro record hotkey must be a keyboard key."));
        }

        if (settings.PlayHotkey.InputKind == Enums.HotkeyInputKind.Keyboard
            && settings.RecordHotkey.InputKind == Enums.HotkeyInputKind.Keyboard
            && settings.PlayHotkey.VirtualKey == settings.RecordHotkey.VirtualKey
            && settings.PlayHotkey.VirtualKey > 0)
        {
            issues.Add(new ValidationIssue(nameof(settings.RecordHotkey), "Macro play and record hotkeys must be different."));
        }

        return new ValidationResult(issues);
    }

    private static bool HasValidCustomInput(ClickSettings settings) =>
        settings.CustomInputKind switch
        {
            Enums.CustomInputKind.Keyboard => settings.CustomKeyVirtualKey > 0
                && !string.IsNullOrWhiteSpace(settings.CustomKeyDisplayName),
            Enums.CustomInputKind.MouseButton => IsSupportedCustomMouseButton(settings.CustomMouseButton)
                && !string.IsNullOrWhiteSpace(settings.CustomKeyDisplayName),
            _ => false,
        };

    private static bool IsSupportedCustomMouseButton(Enums.ClickMouseButton mouseButton) =>
        mouseButton is Enums.ClickMouseButton.Left
            or Enums.ClickMouseButton.Right
            or Enums.ClickMouseButton.Middle
            or Enums.ClickMouseButton.XButton1
            or Enums.ClickMouseButton.XButton2;

    private static bool HasValidHotkeyBinding(HotkeyBinding binding) =>
        binding.InputKind switch
        {
            Enums.HotkeyInputKind.Keyboard => binding.VirtualKey > 0 && !string.IsNullOrWhiteSpace(binding.DisplayName),
            Enums.HotkeyInputKind.MouseButton => IsSupportedHotkeyMouseButton(binding.MouseButton)
                && !string.IsNullOrWhiteSpace(binding.DisplayName),
            _ => false,
        };

    private static bool IsSupportedHotkeyMouseButton(Enums.ClickMouseButton mouseButton) =>
        mouseButton is Enums.ClickMouseButton.Right
            or Enums.ClickMouseButton.Middle
            or Enums.ClickMouseButton.XButton1
            or Enums.ClickMouseButton.XButton2;
}
