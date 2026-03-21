using MultiTool.Core.Enums;
using MultiTool.Core.Models;
using MultiTool.App.Localization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MultiTool.App.ViewModels;

public partial class MacroHotkeyAssignmentItemViewModel : ObservableObject
{
    public MacroHotkeyAssignmentItemViewModel(
        string assignmentId,
        string macroDisplayName,
        string macroFilePath,
        int hotkeyVirtualKey,
        string hotkeyDisplay,
        MacroHotkeyPlaybackMode playbackMode,
        bool isEnabled)
    {
        AssignmentId = assignmentId;
        MacroDisplayName = macroDisplayName;
        MacroFilePath = macroFilePath;
        this.hotkeyVirtualKey = hotkeyVirtualKey;
        this.hotkeyDisplay = hotkeyDisplay;
        this.playbackMode = playbackMode;
        this.isEnabled = isEnabled;
    }

    public string AssignmentId { get; }

    public string MacroDisplayName { get; }

    public string MacroFilePath { get; }

    [ObservableProperty]
    private int hotkeyVirtualKey;

    [ObservableProperty]
    private string hotkeyDisplay = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.MacroHotkeyAssignmentsClickToAssign);

    [ObservableProperty]
    private MacroHotkeyPlaybackMode playbackMode;

    [ObservableProperty]
    private bool isEnabled;

    public bool HasHotkeyAssigned => HotkeyVirtualKey > 0;

    public void SetHotkey(int virtualKey, string displayName)
    {
        HotkeyVirtualKey = virtualKey;
        HotkeyDisplay = displayName;
        IsEnabled = true;
    }

    public void ClearHotkey()
    {
        HotkeyVirtualKey = 0;
        HotkeyDisplay = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.MacroHotkeyAssignmentsClickToAssign);
        IsEnabled = false;
    }

    public MacroHotkeyAssignment? BuildAssignment()
    {
        if (!HasHotkeyAssigned)
        {
            return null;
        }

        return new MacroHotkeyAssignment
        {
            Id = string.IsNullOrWhiteSpace(AssignmentId) ? Guid.NewGuid().ToString("N") : AssignmentId,
            MacroDisplayName = MacroDisplayName,
            MacroFilePath = MacroFilePath,
            Hotkey = new HotkeyBinding(HotkeyVirtualKey, HotkeyDisplay),
            PlaybackMode = PlaybackMode,
            IsEnabled = IsEnabled,
        };
    }

    partial void OnHotkeyVirtualKeyChanged(int value)
    {
        OnPropertyChanged(nameof(HasHotkeyAssigned));
    }
}
