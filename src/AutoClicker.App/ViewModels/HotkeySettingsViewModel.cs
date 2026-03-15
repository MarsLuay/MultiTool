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
        allowModifierVariants = workingCopy.AllowModifierVariants;
    }

    [ObservableProperty]
    private string toggleHotkeyDisplay = string.Empty;

    [ObservableProperty]
    private bool allowModifierVariants;

    public bool SupportsModifierVariants => workingCopy.Toggle.InputKind == HotkeyInputKind.Keyboard;

    [RelayCommand]
    private void Reset()
    {
        workingCopy.Toggle = HotkeySettings.CreateDefaultToggleBinding();
        AllowModifierVariants = false;
        RefreshDisplays();
    }

    partial void OnAllowModifierVariantsChanged(bool value)
    {
        workingCopy.AllowModifierVariants = value;
    }

    public void CaptureHotkey(HotkeyAction action, Key key)
    {
        var virtualKey = HotkeyDisplayNameFormatter.ToVirtualKey(key);
        var binding = new HotkeyBinding(virtualKey, HotkeyDisplayNameFormatter.FormatVirtualKey(virtualKey));

        if (action != HotkeyAction.Toggle)
        {
            throw new NotSupportedException($"Hotkey action {action} is not supported.");
        }

        workingCopy.Toggle = binding;
        ToggleHotkeyDisplay = binding.DisplayName;
        OnPropertyChanged(nameof(SupportsModifierVariants));
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
        AllowModifierVariants = false;
        ToggleHotkeyDisplay = binding.DisplayName;
        OnPropertyChanged(nameof(SupportsModifierVariants));
    }

    public HotkeySettings BuildSettings() => workingCopy.Clone();

    private void RefreshDisplays()
    {
        ToggleHotkeyDisplay = workingCopy.Toggle.DisplayName;
        OnPropertyChanged(nameof(SupportsModifierVariants));
    }
}
