using System.Collections.ObjectModel;
using System.IO;
using AutoClicker.Core.Enums;
using AutoClicker.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AutoClicker.App.ViewModels;

public partial class MainWindowViewModel
{
    private readonly List<MacroHotkeyAssignment> macroHotkeyAssignments = [];
    private readonly SemaphoreSlim assignedMacroHotkeyLock = new(1, 1);
    private CancellationTokenSource? assignedMacroPlaybackCancellationTokenSource;
    private Task? assignedMacroPlaybackTask;
    private string? activeAssignedMacroHotkeyAssignmentId;

    public ObservableCollection<MacroHotkeyAssignment> AssignedMacroHotkeys { get; } = [];

    public bool HasAssignedMacroHotkeys => AssignedMacroHotkeys.Count > 0;

    public bool HasSelectedSavedMacroHotkey =>
        SelectedSavedMacro is not null
        && macroHotkeyAssignments.Any(
            assignment => string.Equals(assignment.MacroFilePath, SelectedSavedMacro.FilePath, StringComparison.OrdinalIgnoreCase));

    public string SelectedSavedMacroHotkeySummary
    {
        get
        {
            if (SelectedSavedMacro is null)
            {
                return "Select a saved macro to set a keyboard shortcut.";
            }

            var assignment = macroHotkeyAssignments.FirstOrDefault(
                candidate => string.Equals(candidate.MacroFilePath, SelectedSavedMacro.FilePath, StringComparison.OrdinalIgnoreCase));
            if (assignment is null)
            {
                return $"No keyboard shortcut is set for '{SelectedSavedMacro.DisplayName}' yet.";
            }

            var activeText = assignment.IsEnabled ? "On" : "Off";
            return $"Shortcut: {assignment.Hotkey.DisplayName}. Action: {FormatPlaybackMode(assignment.PlaybackMode)}. Turned {activeText}.";
        }
    }

    [ObservableProperty]
    private string assignedMacroHotkeysSummary = "No saved macros have keyboard shortcuts yet.";

    private bool CanManageMacroHotkeys => HasSavedMacros && !IsMacroRecording && !IsMacroPlaying;

    private bool CanAssignSelectedMacroHotkey => SelectedSavedMacro is not null && !IsMacroRecording && !IsMacroPlaying;

    [RelayCommand(CanExecute = nameof(CanManageMacroHotkeys))]
    private async Task ManageMacroHotkeysAsync()
    {
        RefreshSavedMacrosInternal();
        if (!HasSavedMacros)
        {
            MacroStatusMessage = "Save a macro first, then set a keyboard shortcut for it.";
            AddMacroLog("Shortcut setup is unavailable because there are no saved macros yet.");
            return;
        }

        var updatedAssignments = macroHotkeyAssignmentsDialogService.Edit(
            SavedMacros.ToArray(),
            macroHotkeyAssignments.Select(assignment => assignment.Clone()).ToArray());
        if (updatedAssignments is null)
        {
            MacroStatusMessage = "Shortcut changes were canceled.";
            AddMacroLog("Shortcut changes were canceled.");
            return;
        }

        var normalizedAssignments = NormalizeMacroHotkeyAssignments(updatedAssignments);
        if (AreMacroHotkeyAssignmentsEquivalent(macroHotkeyAssignments, normalizedAssignments))
        {
            MacroStatusMessage = "No shortcut changes were made.";
            AddMacroLog("No shortcut changes were made.");
            return;
        }

        var candidateSettings = BuildMacroSettings();
        candidateSettings.AssignedHotkeys = normalizedAssignments.Select(assignment => assignment.Clone()).ToList();
        var validation = settingsValidator.ValidateMacro(candidateSettings);
        if (!validation.IsValid)
        {
            MacroStatusMessage = validation.Summary;
            AddMacroLog($"Shortcut changes were rejected: {validation.Summary.Replace(Environment.NewLine, " ")}");
            return;
        }

        if (activeAssignedMacroHotkeyAssignmentId is not null
            && normalizedAssignments.All(
                assignment => !assignment.IsEnabled
                    || !string.Equals(assignment.Id, activeAssignedMacroHotkeyAssignmentId, StringComparison.OrdinalIgnoreCase)))
        {
            CancelActiveAssignedMacroPlayback("Stopped the active repeating macro because its shortcut changed.");
        }

        SetMacroHotkeyAssignments(normalizedAssignments);
        HotkeysChanged?.Invoke(this, EventArgs.Empty);

        var saved = await SaveSettingsAsync("Keyboard shortcuts updated.", updateStatusOnSuccess: false);
        MacroStatusMessage = saved
            ? "Keyboard shortcuts updated."
            : "The shortcuts were updated on screen, but saving them failed.";
        AddMacroLog(MacroStatusMessage);
    }

    [RelayCommand(CanExecute = nameof(CanAssignSelectedMacroHotkey))]
    private async Task AssignSelectedMacroHotkeyAsync()
    {
        if (SelectedSavedMacro is null)
        {
            MacroStatusMessage = "Choose a saved macro first.";
            AddMacroLog("Set shortcut was requested, but no saved macro is selected.");
            return;
        }

        var updatedAssignment = macroHotkeyAssignmentsDialogService.Edit(
            [SelectedSavedMacro],
            macroHotkeyAssignments
                .Where(assignment => string.Equals(assignment.MacroFilePath, SelectedSavedMacro.FilePath, StringComparison.OrdinalIgnoreCase))
                .Select(assignment => assignment.Clone())
                .ToArray());
        if (updatedAssignment is null)
        {
            MacroStatusMessage = $"Shortcut changes for '{SelectedSavedMacro.DisplayName}' were canceled.";
            AddMacroLog($"Shortcut changes for '{SelectedSavedMacro.DisplayName}' were canceled.");
            return;
        }

        var mergedAssignments = macroHotkeyAssignments
            .Where(assignment => !string.Equals(assignment.MacroFilePath, SelectedSavedMacro.FilePath, StringComparison.OrdinalIgnoreCase))
            .Select(assignment => assignment.Clone())
            .Concat(updatedAssignment.Select(assignment => assignment.Clone()))
            .ToArray();

        var normalizedAssignments = NormalizeMacroHotkeyAssignments(mergedAssignments);
        var candidateSettings = BuildMacroSettings();
        candidateSettings.AssignedHotkeys = normalizedAssignments.Select(assignment => assignment.Clone()).ToList();
        var validation = settingsValidator.ValidateMacro(candidateSettings);
        if (!validation.IsValid)
        {
            MacroStatusMessage = validation.Summary;
            AddMacroLog($"Shortcut changes for '{SelectedSavedMacro.DisplayName}' were rejected: {validation.Summary.Replace(Environment.NewLine, " ")}");
            return;
        }

        if (activeAssignedMacroHotkeyAssignmentId is not null
            && normalizedAssignments.All(
                assignment => !assignment.IsEnabled
                    || !string.Equals(assignment.Id, activeAssignedMacroHotkeyAssignmentId, StringComparison.OrdinalIgnoreCase)))
        {
            CancelActiveAssignedMacroPlayback("Stopped the active repeating macro because its shortcut changed.");
        }

        SetMacroHotkeyAssignments(normalizedAssignments);
        HotkeysChanged?.Invoke(this, EventArgs.Empty);

        var saved = await SaveSettingsAsync("Keyboard shortcut updated.", updateStatusOnSuccess: false);
        MacroStatusMessage = saved
            ? $"Updated the keyboard shortcut for '{SelectedSavedMacro.DisplayName}'."
            : $"Updated the shortcut for '{SelectedSavedMacro.DisplayName}' on screen, but saving it failed.";
        AddMacroLog(MacroStatusMessage);
    }

    private async Task HandleAssignedMacroHotkeyAsync(string? assignmentId)
    {
        if (string.IsNullOrWhiteSpace(assignmentId))
        {
            return;
        }

        await assignedMacroHotkeyLock.WaitAsync();

        try
        {
            var assignment = macroHotkeyAssignments.FirstOrDefault(
                candidate => candidate.IsEnabled
                    && string.Equals(candidate.Id, assignmentId, StringComparison.OrdinalIgnoreCase));
            if (assignment is null)
            {
                MacroStatusMessage = "That shortcut is no longer set.";
                AddMacroLog("Ignored a shortcut because its assignment no longer exists.");
                return;
            }

            if (IsMacroRecording)
            {
                MacroStatusMessage = "You can't run a saved macro from a shortcut while recording.";
                AddMacroLog($"Ignored shortcut for '{assignment.MacroDisplayName}' because a recording is active.");
                return;
            }

            if (assignment.PlaybackMode == MacroHotkeyPlaybackMode.ToggleRepeat)
            {
                if (string.Equals(activeAssignedMacroHotkeyAssignmentId, assignment.Id, StringComparison.OrdinalIgnoreCase))
                {
                    await StopActiveAssignedMacroPlaybackAsync($"Stopped '{assignment.MacroDisplayName}'.");
                    return;
                }

                if (activeAssignedMacroHotkeyAssignmentId is not null)
                {
                    var previousName = GetAssignedMacroDisplayName(activeAssignedMacroHotkeyAssignmentId);
                    await StopActiveAssignedMacroPlaybackAsync($"Stopped '{previousName}' and started '{assignment.MacroDisplayName}'.");
                }

                if (IsMacroPlaying)
                {
                    MacroStatusMessage = "Another macro is already playing.";
                    AddMacroLog($"Ignored shortcut for '{assignment.MacroDisplayName}' because another macro is already playing.");
                    return;
                }

                await StartAssignedMacroTogglePlaybackAsync(assignment);
                return;
            }

            if (activeAssignedMacroHotkeyAssignmentId is not null)
            {
                var previousName = GetAssignedMacroDisplayName(activeAssignedMacroHotkeyAssignmentId);
                await StopActiveAssignedMacroPlaybackAsync($"Stopped '{previousName}' so '{assignment.MacroDisplayName}' could run once.");
            }

            if (IsMacroPlaying)
            {
                MacroStatusMessage = "Another macro is already playing.";
                AddMacroLog($"Ignored shortcut for '{assignment.MacroDisplayName}' because another macro is already playing.");
                return;
            }

            await PlayAssignedMacroOnceAsync(assignment);
        }
        finally
        {
            assignedMacroHotkeyLock.Release();
        }
    }

    private async Task PlayAssignedMacroOnceAsync(MacroHotkeyAssignment assignment)
    {
        var macro = await LoadAssignedMacroAsync(assignment);
        if (macro is null)
        {
            return;
        }

        try
        {
            IsMacroPlaying = true;
            MacroStatusMessage = $"Running '{assignment.MacroDisplayName}' once.";
            AddMacroLog($"Running '{assignment.MacroDisplayName}' once from {assignment.Hotkey.DisplayName}.");
            await macroService.PlayAsync(macro, 1);
            MacroStatusMessage = $"Finished '{assignment.MacroDisplayName}'.";
            AddMacroLog($"Finished '{assignment.MacroDisplayName}'.");
        }
        catch (Exception ex)
        {
            MacroStatusMessage = $"Couldn't run '{assignment.MacroDisplayName}': {ex.Message}";
            AddMacroLog($"Couldn't run '{assignment.MacroDisplayName}': {ex.Message}");
        }
        finally
        {
            IsMacroPlaying = false;
        }
    }

    private async Task StartAssignedMacroTogglePlaybackAsync(MacroHotkeyAssignment assignment)
    {
        var macro = await LoadAssignedMacroAsync(assignment);
        if (macro is null)
        {
            return;
        }

        var cancellationTokenSource = new CancellationTokenSource();
        assignedMacroPlaybackCancellationTokenSource = cancellationTokenSource;
        activeAssignedMacroHotkeyAssignmentId = assignment.Id;
        IsMacroPlaying = true;
        MacroStatusMessage = $"'{assignment.MacroDisplayName}' is running. Press {assignment.Hotkey.DisplayName} again to stop.";
        AddMacroLog($"Started '{assignment.MacroDisplayName}' from {assignment.Hotkey.DisplayName}. Press the same key again to stop.");
        assignedMacroPlaybackTask = RunAssignedMacroTogglePlaybackAsync(assignment, macro, cancellationTokenSource);
    }

    private async Task RunAssignedMacroTogglePlaybackAsync(
        MacroHotkeyAssignment assignment,
        RecordedMacro macro,
        CancellationTokenSource cancellationTokenSource)
    {
        try
        {
            while (true)
            {
                cancellationTokenSource.Token.ThrowIfCancellationRequested();
                await macroService.PlayAsync(macro, 1, cancellationTokenSource.Token);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            MacroStatusMessage = $"'{assignment.MacroDisplayName}' stopped because of an error: {ex.Message}";
            AddMacroLog($"'{assignment.MacroDisplayName}' stopped because of an error: {ex.Message}");
        }
        finally
        {
            if (ReferenceEquals(assignedMacroPlaybackCancellationTokenSource, cancellationTokenSource))
            {
                assignedMacroPlaybackCancellationTokenSource = null;
            }

            assignedMacroPlaybackTask = null;

            if (string.Equals(activeAssignedMacroHotkeyAssignmentId, assignment.Id, StringComparison.OrdinalIgnoreCase))
            {
                activeAssignedMacroHotkeyAssignmentId = null;
            }

            cancellationTokenSource.Dispose();
            IsMacroPlaying = false;
        }
    }

    private async Task<RecordedMacro?> LoadAssignedMacroAsync(MacroHotkeyAssignment assignment)
    {
        try
        {
            if (!File.Exists(assignment.MacroFilePath))
            {
                MacroStatusMessage = $"'{assignment.MacroDisplayName}' couldn't be found anymore.";
                AddMacroLog($"Shortcut for '{assignment.MacroDisplayName}' points to a missing file: {assignment.MacroFilePath}");
                return null;
            }

            var macro = await macroFileStore.LoadAsync(assignment.MacroFilePath);
            if (macro.Events.Count == 0)
            {
                MacroStatusMessage = $"'{assignment.MacroDisplayName}' has nothing recorded to run.";
                AddMacroLog($"Shortcut for '{assignment.MacroDisplayName}' was skipped because the macro file has no events.");
                return null;
            }

            return macro;
        }
        catch (Exception ex)
        {
            MacroStatusMessage = $"Couldn't load '{assignment.MacroDisplayName}': {ex.Message}";
            AddMacroLog($"Couldn't load '{assignment.MacroDisplayName}': {ex.Message}");
            return null;
        }
    }

    private async Task StopActiveAssignedMacroPlaybackAsync(string? message = null)
    {
        var cancellationTokenSource = assignedMacroPlaybackCancellationTokenSource;
        var playbackTask = assignedMacroPlaybackTask;
        if (cancellationTokenSource is null || playbackTask is null)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                MacroStatusMessage = message;
                AddMacroLog(message);
            }

            return;
        }

        cancellationTokenSource.Cancel();

        try
        {
            await playbackTask;
        }
        catch (OperationCanceledException)
        {
        }

        if (!string.IsNullOrWhiteSpace(message))
        {
            MacroStatusMessage = message;
            AddMacroLog(message);
        }
    }

    private void CancelActiveAssignedMacroPlayback(string message)
    {
        if (assignedMacroPlaybackCancellationTokenSource is null)
        {
            return;
        }

        assignedMacroPlaybackCancellationTokenSource.Cancel();
        MacroStatusMessage = message;
        AddMacroLog(message);
    }

    private void SetMacroHotkeyAssignments(IEnumerable<MacroHotkeyAssignment> assignments)
    {
        macroHotkeyAssignments.Clear();
        macroHotkeyAssignments.AddRange(NormalizeMacroHotkeyAssignments(assignments));
        RefreshAssignedMacroHotkeysCollection();
    }

    private void UpdateAssignedMacroHotkeyPath(string originalPath, string updatedPath, string updatedMacroName)
    {
        var matchingAssignment = macroHotkeyAssignments.FirstOrDefault(
            assignment => string.Equals(assignment.MacroFilePath, originalPath, StringComparison.OrdinalIgnoreCase));
        if (matchingAssignment is null)
        {
            return;
        }

        matchingAssignment.MacroFilePath = updatedPath;
        matchingAssignment.MacroDisplayName = updatedMacroName;
        RefreshAssignedMacroHotkeysCollection();
    }

    private void SynchronizeAssignedMacroHotkeysWithSavedMacros()
    {
        if (macroHotkeyAssignments.Count == 0)
        {
            RefreshAssignedMacroHotkeysCollection();
            return;
        }

        var savedMacrosByPath = SavedMacros.ToDictionary(savedMacro => savedMacro.FilePath, StringComparer.OrdinalIgnoreCase);
        var synchronizedAssignments = macroHotkeyAssignments
            .Where(assignment => savedMacrosByPath.ContainsKey(assignment.MacroFilePath))
            .Select(
                assignment =>
                {
                    var savedMacro = savedMacrosByPath[assignment.MacroFilePath];
                    var updatedAssignment = assignment.Clone();
                    updatedAssignment.MacroDisplayName = savedMacro.DisplayName;
                    return updatedAssignment;
                })
            .ToArray();

        if (AreMacroHotkeyAssignmentsEquivalent(macroHotkeyAssignments, synchronizedAssignments))
        {
            return;
        }

        if (activeAssignedMacroHotkeyAssignmentId is not null
            && synchronizedAssignments.All(
                assignment => !string.Equals(assignment.Id, activeAssignedMacroHotkeyAssignmentId, StringComparison.OrdinalIgnoreCase)))
        {
            CancelActiveAssignedMacroPlayback("Stopped the active repeating macro because its file is no longer available.");
        }

        SetMacroHotkeyAssignments(synchronizedAssignments);

        if (initialized)
        {
            HotkeysChanged?.Invoke(this, EventArgs.Empty);
            ScheduleSettingsAutoSave();
            AddMacroLog("Updated macro shortcuts after the saved macros list changed.");
        }
    }

    private void RefreshAssignedMacroHotkeysCollection()
    {
        AssignedMacroHotkeys.Clear();
        foreach (var assignment in macroHotkeyAssignments)
        {
            AssignedMacroHotkeys.Add(assignment.Clone());
        }

        AssignedMacroHotkeysSummary = BuildAssignedMacroHotkeysSummary(macroHotkeyAssignments);
        OnPropertyChanged(nameof(HasAssignedMacroHotkeys));
        OnPropertyChanged(nameof(HasSelectedSavedMacroHotkey));
        OnPropertyChanged(nameof(SelectedSavedMacroHotkeySummary));
    }

    private string GetAssignedMacroDisplayName(string assignmentId)
    {
        return macroHotkeyAssignments
            .FirstOrDefault(assignment => string.Equals(assignment.Id, assignmentId, StringComparison.OrdinalIgnoreCase))
            ?.MacroDisplayName
            ?? "the active saved macro";
    }

    private static string BuildAssignedMacroHotkeysSummary(IReadOnlyCollection<MacroHotkeyAssignment> assignments)
    {
        if (assignments.Count == 0)
        {
            return "No saved macros have keyboard shortcuts yet.";
        }

        var enabledCount = assignments.Count(assignment => assignment.IsEnabled);
        var repeatingCount = assignments.Count(assignment => assignment.PlaybackMode == MacroHotkeyPlaybackMode.ToggleRepeat);
        return $"{assignments.Count} shortcut{(assignments.Count == 1 ? string.Empty : "s")} set. {enabledCount} turned on. {repeatingCount} set to start and stop with the same key.";
    }

    private static string FormatPlaybackMode(MacroHotkeyPlaybackMode playbackMode) =>
        playbackMode switch
        {
            MacroHotkeyPlaybackMode.PlayOnce => "Run once",
            MacroHotkeyPlaybackMode.ToggleRepeat => "Start and stop with the same key",
            _ => playbackMode.ToString(),
        };

    private static IReadOnlyList<MacroHotkeyAssignment> NormalizeMacroHotkeyAssignments(IEnumerable<MacroHotkeyAssignment> assignments)
    {
        return assignments
            .Where(
                assignment => assignment is not null
                    && !string.IsNullOrWhiteSpace(assignment.MacroFilePath)
                    && !string.IsNullOrWhiteSpace(assignment.MacroDisplayName)
                    && assignment.Hotkey.InputKind == HotkeyInputKind.Keyboard
                    && assignment.Hotkey.VirtualKey > 0
                    && !string.IsNullOrWhiteSpace(assignment.Hotkey.DisplayName))
            .Select(
                assignment =>
                {
                    var normalizedAssignment = assignment.Clone();
                    normalizedAssignment.Id = string.IsNullOrWhiteSpace(normalizedAssignment.Id)
                        ? Guid.NewGuid().ToString("N")
                        : normalizedAssignment.Id;
                    return normalizedAssignment;
                })
            .OrderBy(assignment => assignment.MacroDisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(assignment => assignment.MacroFilePath, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static bool AreMacroHotkeyAssignmentsEquivalent(
        IReadOnlyList<MacroHotkeyAssignment> currentAssignments,
        IReadOnlyList<MacroHotkeyAssignment> updatedAssignments)
    {
        if (currentAssignments.Count != updatedAssignments.Count)
        {
            return false;
        }

        for (var index = 0; index < currentAssignments.Count; index++)
        {
            var current = currentAssignments[index];
            var updated = updatedAssignments[index];
            if (!string.Equals(current.Id, updated.Id, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(current.MacroFilePath, updated.MacroFilePath, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(current.MacroDisplayName, updated.MacroDisplayName, StringComparison.Ordinal)
                || current.Hotkey.VirtualKey != updated.Hotkey.VirtualKey
                || !string.Equals(current.Hotkey.DisplayName, updated.Hotkey.DisplayName, StringComparison.Ordinal)
                || current.IsEnabled != updated.IsEnabled
                || current.PlaybackMode != updated.PlaybackMode)
            {
                return false;
            }
        }

        return true;
    }
}
