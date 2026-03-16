using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using AutoClicker.App.Helpers;
using AutoClicker.App.Models;
using AutoClicker.Core.Enums;
using AutoClicker.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AutoClicker.App.ViewModels;

public partial class MacroHotkeyAssignmentsWindowViewModel : ObservableObject
{
    public MacroHotkeyAssignmentsWindowViewModel(
        IReadOnlyList<SavedMacroEntry> savedMacros,
        IReadOnlyList<MacroHotkeyAssignment> currentAssignments)
    {
        PlaybackModes = Enum.GetValues<MacroHotkeyPlaybackMode>();
        MacroItems = [];

        var assignmentsByPath = currentAssignments.ToDictionary(
            assignment => assignment.MacroFilePath,
            StringComparer.OrdinalIgnoreCase);

        foreach (var savedMacro in savedMacros)
        {
            if (!assignmentsByPath.TryGetValue(savedMacro.FilePath, out var existingAssignment))
            {
                existingAssignment = new MacroHotkeyAssignment
                {
                    Id = Guid.NewGuid().ToString("N"),
                    MacroDisplayName = savedMacro.DisplayName,
                    MacroFilePath = savedMacro.FilePath,
                    PlaybackMode = MacroHotkeyPlaybackMode.PlayOnce,
                    IsEnabled = false,
                };
            }

            MacroItems.Add(
                new MacroHotkeyAssignmentItemViewModel(
                    existingAssignment.Id,
                    savedMacro.DisplayName,
                    savedMacro.FilePath,
                    existingAssignment.Hotkey.VirtualKey,
                    string.IsNullOrWhiteSpace(existingAssignment.Hotkey.DisplayName) ? "Click to assign" : existingAssignment.Hotkey.DisplayName,
                    existingAssignment.PlaybackMode,
                    existingAssignment.IsEnabled));
        }

        StatusMessage = MacroItems.Count == 0
            ? "No saved macros were found in the Macros folder."
            : "Pick a keyboard shortcut for any saved macro. 'Run once' plays it one time. 'Start/stop' keeps it running until you press the same key again.";
    }

    public ObservableCollection<MacroHotkeyAssignmentItemViewModel> MacroItems { get; }

    public IReadOnlyList<MacroHotkeyPlaybackMode> PlaybackModes { get; }

    public bool HasSavedMacros => MacroItems.Count > 0;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    public void CaptureHotkey(MacroHotkeyAssignmentItemViewModel item, Key key)
    {
        var capturedKey = key == Key.System ? Key.None : key;
        var virtualKey = HotkeyDisplayNameFormatter.ToVirtualKey(capturedKey);
        if (virtualKey <= 0)
        {
            return;
        }

        item.SetHotkey(virtualKey, HotkeyDisplayNameFormatter.FormatVirtualKey(virtualKey));
        StatusMessage = $"'{item.MacroDisplayName}' will now run when you press {item.HotkeyDisplay}.";
    }

    [RelayCommand]
    private void ClearHotkey(MacroHotkeyAssignmentItemViewModel? item)
    {
        if (item is null)
        {
            return;
        }

        item.ClearHotkey();
        StatusMessage = $"Removed the keyboard shortcut for '{item.MacroDisplayName}'.";
    }

    public IReadOnlyList<MacroHotkeyAssignment> BuildAssignments() =>
        MacroItems
            .Select(item => item.BuildAssignment())
            .Where(assignment => assignment is not null)
            .Select(assignment => assignment!)
            .OrderBy(assignment => assignment.MacroDisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(assignment => Path.GetFileName(assignment.MacroFilePath), StringComparer.OrdinalIgnoreCase)
            .ToArray();
}
