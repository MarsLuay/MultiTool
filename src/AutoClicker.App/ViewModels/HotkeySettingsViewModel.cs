using System.Windows.Input;
using AutoClicker.App.Helpers;
using AutoClicker.Core.Enums;
using AutoClicker.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AutoClicker.App.ViewModels;

public partial class HotkeySettingsViewModel : ObservableObject
{
    private readonly HotkeySettings workingCopy;

    public HotkeySettingsViewModel(HotkeySettings currentSettings)
    {
        workingCopy = currentSettings;

        toggleHotkeyDisplay = workingCopy.Toggle.DisplayName;
        pinWindowHotkeyDisplay = GetDisplayNameOrUnassigned(workingCopy.PinWindow);
    }

    [ObservableProperty]
    private string toggleHotkeyDisplay = string.Empty;

    [ObservableProperty]
    private string pinWindowHotkeyDisplay = string.Empty;

    [RelayCommand]
    private void Reset()
    {
        workingCopy.Toggle = HotkeySettings.CreateDefaultToggleBinding();
        workingCopy.PinWindow = HotkeySettings.CreateUnassignedBinding();
        RefreshDisplays();
    }

    public void CaptureHotkey(HotkeyAction action, Key key)
    {
        var virtualKey = HotkeyDisplayNameFormatter.ToVirtualKey(key);
        if (virtualKey <= 0)
        {
            return;
        }

        var binding = new HotkeyBinding(virtualKey, HotkeyDisplayNameFormatter.FormatVirtualKey(virtualKey));

        if (action == HotkeyAction.Toggle)
        {
            workingCopy.Toggle = binding;
            ToggleHotkeyDisplay = binding.DisplayName;
            return;
        }

        if (action == HotkeyAction.WindowPinToggle)
        {
            workingCopy.PinWindow = binding;
            PinWindowHotkeyDisplay = binding.DisplayName;
            return;
        }

        throw new NotSupportedException($"Hotkey action {action} is not supported.");
    }

    public void CaptureMouseHotkey(HotkeyAction action, ClickMouseButton mouseButton)
    {
        var binding = new HotkeyBinding(
            virtualKey: 0,
            displayName: HotkeyDisplayNameFormatter.FormatMouseButton(mouseButton),
            inputKind: HotkeyInputKind.MouseButton,
            mouseButton: mouseButton);

        if (action != HotkeyAction.Toggle)
        {
            throw new NotSupportedException($"Hotkey action {action} is not supported.");
        }

        workingCopy.Toggle = binding;
        ToggleHotkeyDisplay = binding.DisplayName;
    }

    public HotkeySettings BuildSettings() => workingCopy.Clone();

    private void RefreshDisplays()
    {
        ToggleHotkeyDisplay = workingCopy.Toggle.DisplayName;
        PinWindowHotkeyDisplay = GetDisplayNameOrUnassigned(workingCopy.PinWindow);
    }

    private static string GetDisplayNameOrUnassigned(HotkeyBinding binding) =>
        binding.VirtualKey > 0 && !string.IsNullOrWhiteSpace(binding.DisplayName)
            ? binding.DisplayName
            : HotkeySettings.UnassignedDisplayName;
}
