using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using MultiTool.App.Helpers;
using MultiTool.App.Localization;
using MultiTool.App.Models;
using MultiTool.Core.Enums;
using MultiTool.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MultiTool.App.ViewModels;

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
                    existingAssignment.Hotkey.Modifiers,
                    string.IsNullOrWhiteSpace(existingAssignment.Hotkey.DisplayName)
                        ? L(AppLanguageKeys.MacroHotkeyAssignmentsClickToAssign)
                        : existingAssignment.Hotkey.DisplayName,
                    existingAssignment.PlaybackMode,
                    existingAssignment.IsEnabled));
        }

        StatusMessage = MacroItems.Count == 0
            ? L(AppLanguageKeys.MacroHotkeyAssignmentsStatusNoSaved)
            : L(AppLanguageKeys.MacroHotkeyAssignmentsStatusPick);
    }

    public ObservableCollection<MacroHotkeyAssignmentItemViewModel> MacroItems { get; }

    public IReadOnlyList<MacroHotkeyPlaybackMode> PlaybackModes { get; }

    public string WindowTitleText => L(AppLanguageKeys.MacroHotkeyAssignmentsTitle);

    public string HeadingText => L(AppLanguageKeys.MacroHotkeyAssignmentsHeading);

    public string DescriptionText => L(AppLanguageKeys.MacroHotkeyAssignmentsDescription);

    public string EmptyText => L(AppLanguageKeys.MacroHotkeyAssignmentsEmpty);

    public string ActiveLabelText => L(AppLanguageKeys.MacroHotkeyAssignmentsActive);

    public string ShortcutKeyLabelText => L(AppLanguageKeys.MacroHotkeyAssignmentsShortcutKey);

    public string BehaviorLabelText => L(AppLanguageKeys.MacroHotkeyAssignmentsBehavior);

    public string RemoveKeyLabelText => L(AppLanguageKeys.MacroHotkeyAssignmentsRemoveKey);

    public string ClearButtonText => L(AppLanguageKeys.MacroHotkeyAssignmentsClear);

    public string CancelButtonText => L(AppLanguageKeys.MacroHotkeyAssignmentsCancel);

    public string SaveButtonText => L(AppLanguageKeys.MacroHotkeyAssignmentsSave);

    public string CaptureTooltipText => L(AppLanguageKeys.MacroHotkeyAssignmentsCaptureTooltip);

    public bool HasSavedMacros => MacroItems.Count > 0;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    public void CaptureHotkey(MacroHotkeyAssignmentItemViewModel item, Key key, ModifierKeys modifiers)
    {
        var capturedKey = key == Key.System ? Key.None : key;
        var binding = HotkeyDisplayNameFormatter.CreateKeyboardBinding(capturedKey, modifiers);
        if (binding.VirtualKey <= 0)
        {
            return;
        }

        item.SetHotkey(binding.VirtualKey, binding.Modifiers, binding.DisplayName);
        StatusMessage = F(AppLanguageKeys.MacroHotkeyAssignmentsStatusAssignedFormat, item.MacroDisplayName, item.HotkeyDisplay);
    }

    [RelayCommand]
    private void ClearHotkey(MacroHotkeyAssignmentItemViewModel? item)
    {
        if (item is null)
        {
            return;
        }

        item.ClearHotkey();
        StatusMessage = F(AppLanguageKeys.MacroHotkeyAssignmentsStatusRemovedFormat, item.MacroDisplayName);
    }

    public IReadOnlyList<MacroHotkeyAssignment> BuildAssignments() =>
        MacroItems
            .Select(item => item.BuildAssignment())
            .Where(assignment => assignment is not null)
            .Select(assignment => assignment!)
            .OrderBy(assignment => assignment.MacroDisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(assignment => Path.GetFileName(assignment.MacroFilePath), StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private static string L(string key) => AppLanguageStrings.GetForCurrentLanguage(key);

    private static string F(string key, params object[] args) => AppLanguageStrings.FormatForCurrentLanguage(key, args);
}
