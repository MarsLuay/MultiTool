using System.Collections.ObjectModel;
using System.IO;
using MultiTool.App.Localization;
using MultiTool.Core.Enums;
using MultiTool.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MultiTool.App.ViewModels;

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
                return L(AppLanguageKeys.MacroHotkeyNoSelectedSummary);
            }

            var assignment = macroHotkeyAssignments.FirstOrDefault(
                candidate => string.Equals(candidate.MacroFilePath, SelectedSavedMacro.FilePath, StringComparison.OrdinalIgnoreCase));
            if (assignment is null)
            {
                return F(AppLanguageKeys.MacroHotkeyNotSetForSelectedFormat, SelectedSavedMacro.DisplayName);
            }

            var activeText = assignment.IsEnabled
                ? L(AppLanguageKeys.MacroHotkeyOnState)
                : L(AppLanguageKeys.MacroHotkeyOffState);
            return F(
                AppLanguageKeys.MacroHotkeySelectedSummaryFormat,
                assignment.Hotkey.DisplayName,
                FormatPlaybackMode(assignment.PlaybackMode),
                activeText);
        }
    }

    [ObservableProperty]
    private string assignedMacroHotkeysSummary = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.MacroHotkeyDefaultAssignmentsSummary);

    private bool CanManageMacroHotkeys => HasSavedMacros && !IsMacroRecording && !IsMacroPlaying;

    private bool CanAssignSelectedMacroHotkey => SelectedSavedMacro is not null && !IsMacroRecording && !IsMacroPlaying;

    [RelayCommand(CanExecute = nameof(CanManageMacroHotkeys))]
    private async Task ManageMacroHotkeysAsync()
    {
        RefreshSavedMacrosInternal();
        if (!HasSavedMacros)
        {
            MacroStatusMessage = L(AppLanguageKeys.MacroHotkeyStatusSaveMacroFirst);
            AddMacroLog(L(AppLanguageKeys.MacroHotkeyLogSetupUnavailableNoSaved));
            return;
        }

        var updatedAssignments = macroHotkeyAssignmentsDialogService.Edit(
            SavedMacros.ToArray(),
            macroHotkeyAssignments.Select(assignment => assignment.Clone()).ToArray());
        if (updatedAssignments is null)
        {
            MacroStatusMessage = L(AppLanguageKeys.MacroHotkeyStatusChangesCanceled);
            AddMacroLog(L(AppLanguageKeys.MacroHotkeyStatusChangesCanceled));
            return;
        }

        var normalizedAssignments = NormalizeMacroHotkeyAssignments(updatedAssignments);
        if (AreMacroHotkeyAssignmentsEquivalent(macroHotkeyAssignments, normalizedAssignments))
        {
            MacroStatusMessage = L(AppLanguageKeys.MacroHotkeyStatusNoChanges);
            AddMacroLog(L(AppLanguageKeys.MacroHotkeyStatusNoChanges));
            return;
        }

        var candidateSettings = BuildMacroSettings();
        candidateSettings.AssignedHotkeys = normalizedAssignments.Select(assignment => assignment.Clone()).ToList();
        var validation = settingsValidator.ValidateMacro(candidateSettings);
        if (!validation.IsValid)
        {
            MacroStatusMessage = validation.Summary;
            AddMacroLog(F(
                AppLanguageKeys.MacroHotkeyLogChangesRejectedFormat,
                validation.Summary.Replace(Environment.NewLine, " ")));
            return;
        }

        if (activeAssignedMacroHotkeyAssignmentId is not null
            && normalizedAssignments.All(
                assignment => !assignment.IsEnabled
                    || !string.Equals(assignment.Id, activeAssignedMacroHotkeyAssignmentId, StringComparison.OrdinalIgnoreCase)))
        {
            CancelActiveAssignedMacroPlayback(L(AppLanguageKeys.MacroHotkeyStatusStoppedActiveBecauseShortcutChanged));
        }

        SetMacroHotkeyAssignments(normalizedAssignments);
        HotkeysChanged?.Invoke(this, EventArgs.Empty);

        var saved = await SaveSettingsAsync(L(AppLanguageKeys.MacroHotkeyStatusShortcutsUpdated), updateStatusOnSuccess: false);
        MacroStatusMessage = saved
            ? L(AppLanguageKeys.MacroHotkeyStatusShortcutsUpdated)
            : L(AppLanguageKeys.MacroHotkeyStatusShortcutsUpdatedButSaveFailed);
        AddMacroLog(MacroStatusMessage);
    }

    [RelayCommand(CanExecute = nameof(CanAssignSelectedMacroHotkey))]
    private async Task AssignSelectedMacroHotkeyAsync()
    {
        if (SelectedSavedMacro is null)
        {
            MacroStatusMessage = L(AppLanguageKeys.MacroHotkeyStatusChooseSavedMacro);
            AddMacroLog(L(AppLanguageKeys.MacroHotkeyLogSetRequestedNoSaved));
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
            MacroStatusMessage = F(AppLanguageKeys.MacroHotkeyStatusChangesCanceledForFormat, SelectedSavedMacro.DisplayName);
            AddMacroLog(MacroStatusMessage);
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
            AddMacroLog(F(
                AppLanguageKeys.MacroHotkeyLogChangesRejectedForFormat,
                SelectedSavedMacro.DisplayName,
                validation.Summary.Replace(Environment.NewLine, " ")));
            return;
        }

        if (activeAssignedMacroHotkeyAssignmentId is not null
            && normalizedAssignments.All(
                assignment => !assignment.IsEnabled
                    || !string.Equals(assignment.Id, activeAssignedMacroHotkeyAssignmentId, StringComparison.OrdinalIgnoreCase)))
        {
            CancelActiveAssignedMacroPlayback(L(AppLanguageKeys.MacroHotkeyStatusStoppedActiveBecauseShortcutChanged));
        }

        SetMacroHotkeyAssignments(normalizedAssignments);
        HotkeysChanged?.Invoke(this, EventArgs.Empty);

        var saved = await SaveSettingsAsync(L(AppLanguageKeys.MacroHotkeyStatusShortcutUpdated), updateStatusOnSuccess: false);
        MacroStatusMessage = saved
            ? F(AppLanguageKeys.MacroHotkeyStatusUpdatedForFormat, SelectedSavedMacro.DisplayName)
            : F(AppLanguageKeys.MacroHotkeyStatusUpdatedButSaveFailedForFormat, SelectedSavedMacro.DisplayName);
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
                MacroStatusMessage = L(AppLanguageKeys.MacroHotkeyStatusNoLongerSet);
                AddMacroLog(L(AppLanguageKeys.MacroHotkeyLogIgnoredAssignmentMissing));
                return;
            }

            if (IsMacroRecording)
            {
                MacroStatusMessage = L(AppLanguageKeys.MacroHotkeyStatusRecordingConflict);
                AddMacroLog(F(AppLanguageKeys.MacroHotkeyLogIgnoredRecordingActiveFormat, assignment.MacroDisplayName));
                return;
            }

            if (assignment.PlaybackMode == MacroHotkeyPlaybackMode.ToggleRepeat)
            {
                if (string.Equals(activeAssignedMacroHotkeyAssignmentId, assignment.Id, StringComparison.OrdinalIgnoreCase))
                {
                    await StopActiveAssignedMacroPlaybackAsync(F(AppLanguageKeys.MacroHotkeyStatusStoppedFormat, assignment.MacroDisplayName));
                    return;
                }

                if (activeAssignedMacroHotkeyAssignmentId is not null)
                {
                    var previousName = GetAssignedMacroDisplayName(activeAssignedMacroHotkeyAssignmentId);
                    await StopActiveAssignedMacroPlaybackAsync(F(AppLanguageKeys.MacroHotkeyStatusStoppedAndStartedFormat, previousName, assignment.MacroDisplayName));
                }

                if (IsMacroPlaying)
                {
                    MacroStatusMessage = L(AppLanguageKeys.MacroHotkeyStatusAnotherPlaying);
                    AddMacroLog(F(AppLanguageKeys.MacroHotkeyLogIgnoredAnotherPlayingFormat, assignment.MacroDisplayName));
                    return;
                }

                await StartAssignedMacroTogglePlaybackAsync(assignment);
                return;
            }

            if (activeAssignedMacroHotkeyAssignmentId is not null)
            {
                var previousName = GetAssignedMacroDisplayName(activeAssignedMacroHotkeyAssignmentId);
                await StopActiveAssignedMacroPlaybackAsync(F(AppLanguageKeys.MacroHotkeyStatusStoppedToRunOnceFormat, previousName, assignment.MacroDisplayName));
            }

            if (IsMacroPlaying)
            {
                MacroStatusMessage = L(AppLanguageKeys.MacroHotkeyStatusAnotherPlaying);
                AddMacroLog(F(AppLanguageKeys.MacroHotkeyLogIgnoredAnotherPlayingFormat, assignment.MacroDisplayName));
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
            MacroStatusMessage = F(AppLanguageKeys.MacroHotkeyStatusRunningOnceFormat, assignment.MacroDisplayName);
            AddMacroLog(F(AppLanguageKeys.MacroHotkeyLogRunningOnceFromFormat, assignment.MacroDisplayName, assignment.Hotkey.DisplayName));
            await macroService.PlayAsync(macro, 1);
            MacroStatusMessage = F(AppLanguageKeys.MacroHotkeyStatusFinishedFormat, assignment.MacroDisplayName);
            AddMacroLog(F(AppLanguageKeys.MacroHotkeyStatusFinishedFormat, assignment.MacroDisplayName));
        }
        catch (Exception ex)
        {
            MacroStatusMessage = F(AppLanguageKeys.MacroHotkeyStatusRunFailedFormat, assignment.MacroDisplayName, ex.Message);
            AddMacroLog(F(AppLanguageKeys.MacroHotkeyStatusRunFailedFormat, assignment.MacroDisplayName, ex.Message));
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
        MacroStatusMessage = F(AppLanguageKeys.MacroHotkeyStatusToggleRunningFormat, assignment.MacroDisplayName, assignment.Hotkey.DisplayName);
        AddMacroLog(F(AppLanguageKeys.MacroHotkeyLogStartedFromAndPressAgainFormat, assignment.MacroDisplayName, assignment.Hotkey.DisplayName));
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
            MacroStatusMessage = F(AppLanguageKeys.MacroHotkeyStatusToggleStoppedErrorFormat, assignment.MacroDisplayName, ex.Message);
            AddMacroLog(F(AppLanguageKeys.MacroHotkeyStatusToggleStoppedErrorFormat, assignment.MacroDisplayName, ex.Message));
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
                MacroStatusMessage = F(AppLanguageKeys.MacroHotkeyStatusMissingFileFormat, assignment.MacroDisplayName);
                AddMacroLog(F(AppLanguageKeys.MacroHotkeyLogMissingFilePathFormat, assignment.MacroDisplayName, assignment.MacroFilePath));
                return null;
            }

            var macro = await macroFileStore.LoadAsync(assignment.MacroFilePath);
            if (macro.Events.Count == 0)
            {
                MacroStatusMessage = F(AppLanguageKeys.MacroHotkeyStatusNoEventsFormat, assignment.MacroDisplayName);
                AddMacroLog(F(AppLanguageKeys.MacroHotkeyLogNoEventsFormat, assignment.MacroDisplayName));
                return null;
            }

            return macro;
        }
        catch (Exception ex)
        {
            MacroStatusMessage = F(AppLanguageKeys.MacroHotkeyStatusLoadFailedFormat, assignment.MacroDisplayName, ex.Message);
            AddMacroLog(F(AppLanguageKeys.MacroHotkeyStatusLoadFailedFormat, assignment.MacroDisplayName, ex.Message));
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
            CancelActiveAssignedMacroPlayback(L(AppLanguageKeys.MacroHotkeyStatusStoppedActiveFileUnavailable));
        }

        SetMacroHotkeyAssignments(synchronizedAssignments);

        if (initialized)
        {
            HotkeysChanged?.Invoke(this, EventArgs.Empty);
            ScheduleSettingsAutoSave();
            AddMacroLog(L(AppLanguageKeys.MacroHotkeyLogUpdatedAfterSavedMacrosChanged));
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
            ?? L(AppLanguageKeys.MacroHotkeyActiveFallbackName);
    }

    private static string BuildAssignedMacroHotkeysSummary(IReadOnlyCollection<MacroHotkeyAssignment> assignments)
    {
        if (assignments.Count == 0)
        {
            return AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.MacroHotkeyDefaultAssignmentsSummary);
        }

        var enabledCount = assignments.Count(assignment => assignment.IsEnabled);
        var repeatingCount = assignments.Count(assignment => assignment.PlaybackMode == MacroHotkeyPlaybackMode.ToggleRepeat);
        return AppLanguageStrings.FormatForCurrentLanguage(
            AppLanguageKeys.MacroHotkeySummaryFormat,
            assignments.Count,
            assignments.Count == 1 ? string.Empty : "s",
            enabledCount,
            repeatingCount);
    }

    private static string FormatPlaybackMode(MacroHotkeyPlaybackMode playbackMode) =>
        playbackMode switch
        {
            MacroHotkeyPlaybackMode.PlayOnce => AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.MacroHotkeyPlaybackModeRunOnce),
            MacroHotkeyPlaybackMode.ToggleRepeat => AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.MacroHotkeyPlaybackModeToggleRepeat),
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
