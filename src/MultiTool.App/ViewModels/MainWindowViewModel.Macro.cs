using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MultiTool.App.Helpers;
using MultiTool.App.Localization;
using MultiTool.App.Models;
using MultiTool.App.Services;
using MultiTool.Core.Defaults;
using MultiTool.Core.Enums;
using MultiTool.Core.Models;
using MultiTool.Core.Services;
using MultiTool.Core.Validation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MultiTool.App.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private bool CanNewMacro => HasRecordedMacro && !IsMacroRecording && !IsMacroPlaying;

    [RelayCommand(CanExecute = nameof(CanNewMacro))]
    private void NewMacro()
    {
        macroService.Clear();
        MacroName = L(AppLanguageKeys.MainMacroNameDefault);
        HasRecordedMacro = false;
        MacroSummaryText = L(AppLanguageKeys.MainMacroSummaryNoRecorded);
        MacroStatusMessage = L(AppLanguageKeys.MainMacroStatusStartedNew);
        AddMacroLog(MacroStatusMessage);
    }

    private bool CanStartMacroRecording => !IsMacroRecording && !IsMacroPlaying;

    [RelayCommand(CanExecute = nameof(CanStartMacroRecording))]
    private void StartMacroRecording()
    {
        try
        {
            var name = string.IsNullOrWhiteSpace(MacroName) ? L(AppLanguageKeys.MainMacroNameDefault) : MacroName.Trim();
            macroService.StartRecording(name, RecordMacroMouseMovement);
            MacroName = name;
            IsMacroRecording = true;
            HasRecordedMacro = false;
            MacroSummaryText = F(AppLanguageKeys.MainMacroSummaryRecordingFormat, name);
            MacroStatusMessage = RecordMacroMouseMovement
                ? F(AppLanguageKeys.MainMacroStatusRecordingWithMouseFormat, name)
                : F(AppLanguageKeys.MainMacroStatusRecordingWithoutMouseFormat, name);
            AddMacroLog(F(AppLanguageKeys.MainMacroLogStartedRecordingFormat, name));
        }
        catch (Exception ex)
        {
            MacroStatusMessage = F(AppLanguageKeys.MainMacroStatusStartRecordingFailedFormat, ex.Message);
            AddMacroLog(MacroStatusMessage);
        }
    }

    private async Task ToggleMacroRecordingAsync()
    {
        if (IsMacroRecording)
        {
            StopMacroRecording();
            return;
        }

        if (IsMacroPlaying)
        {
            MacroStatusMessage = L(AppLanguageKeys.MainMacroStatusCannotRecordWhilePlaying);
            AddMacroLog(L(AppLanguageKeys.MainMacroLogRecordHotkeyIgnoredPlaying));
            return;
        }

        StartMacroRecording();
        await Task.CompletedTask;
    }

    private bool CanStopMacroRecording => IsMacroRecording;

    [RelayCommand(CanExecute = nameof(CanStopMacroRecording))]
    private void StopMacroRecording()
    {
        try
        {
            var macro = macroService.StopRecording();
            IsMacroRecording = false;
            HasRecordedMacro = macro.Events.Count > 0;
            MacroName = macro.Name;
            MacroSummaryText = HasRecordedMacro
                ? F(AppLanguageKeys.MainMacroSummaryRecordedFormat, macro.Name, macro.Events.Count, macro.Duration.TotalMilliseconds)
                : F(AppLanguageKeys.MainMacroSummaryNoInputCapturedFormat, macro.Name);
            MacroStatusMessage = HasRecordedMacro
                ? F(AppLanguageKeys.MainMacroStatusStoppedRecordingFormat, macro.Name)
                : F(AppLanguageKeys.MainMacroStatusStoppedRecordingNoInputFormat, macro.Name);
            AddMacroLog(F(AppLanguageKeys.MainMacroLogStoppedRecordingFormat, macro.Name, macro.Events.Count));
        }
        catch (Exception ex)
        {
            MacroStatusMessage = F(AppLanguageKeys.MainMacroStatusStopRecordingFailedFormat, ex.Message);
            AddMacroLog(MacroStatusMessage);
        }
    }

    private bool CanPlayMacro => HasRecordedMacro && !IsMacroRecording && (!IsMacroPlaying || IsMainMacroInfinitePlaybackActive);

    [RelayCommand(CanExecute = nameof(CanPlayMacro))]
    private async Task PlayMacroAsync()
    {
        if (IsMainMacroInfinitePlaybackActive)
        {
            var activeMacroName = macroService.CurrentMacro?.Name;
            if (string.IsNullOrWhiteSpace(activeMacroName))
            {
                activeMacroName = string.IsNullOrWhiteSpace(MacroName)
                    ? L(AppLanguageKeys.MainMacroNameDefault)
                    : MacroName.Trim();
            }

            await StopMainMacroInfinitePlaybackAsync(F(AppLanguageKeys.MainMacroStatusStoppedInfiniteFormat, activeMacroName));
            return;
        }

        if (IsMacroPlaying)
        {
            MacroStatusMessage = L(AppLanguageKeys.MainMacroStatusAlreadyPlaying);
            AddMacroLog(MacroStatusMessage);
            return;
        }

        var startedFinitePlayback = false;

        try
        {
            var macro = macroService.CurrentMacro;
            if (macro is null || macro.Events.Count == 0)
            {
                MacroStatusMessage = L(AppLanguageKeys.MainMacroStatusNoRecordedToPlay);
                AddMacroLog(L(AppLanguageKeys.MainMacroLogPlaybackRequestedNoMacro));
                return;
            }

            if (IsMacroPlaybackInfinite)
            {
                StartMainMacroInfinitePlayback(macro);
                return;
            }

            var count = Math.Max(1, MacroPlaybackCount);
            MacroPlaybackCount = count;
            startedFinitePlayback = true;
            IsMacroPlaying = true;
            MacroStatusMessage = F(AppLanguageKeys.MainMacroStatusPlayingFormat, macro.Name, count);
            AddMacroLog(MacroStatusMessage);

            await macroService.PlayAsync(count);

            MacroStatusMessage = F(AppLanguageKeys.MainMacroStatusFinishedPlayingFormat, macro.Name);
            AddMacroLog(MacroStatusMessage);
        }
        catch (Exception ex)
        {
            MacroStatusMessage = F(AppLanguageKeys.MainMacroStatusPlayFailedFormat, ex.Message);
            AddMacroLog(MacroStatusMessage);
        }
        finally
        {
            if (startedFinitePlayback)
            {
                IsMacroPlaying = false;
            }
        }
    }

    private void StartMainMacroInfinitePlayback(RecordedMacro macro)
    {
        var cancellationTokenSource = new CancellationTokenSource();
        mainMacroPlaybackCancellationTokenSource = cancellationTokenSource;
        mainMacroPlaybackTask = RunMainMacroInfinitePlaybackAsync(macro, cancellationTokenSource);
        IsMacroPlaying = true;
        MacroStatusMessage = F(AppLanguageKeys.MainMacroStatusPlayingInfiniteFormat, macro.Name, MacroHotkeyDisplay);
        AddMacroLog(MacroStatusMessage);
    }

    private async Task RunMainMacroInfinitePlaybackAsync(RecordedMacro macro, CancellationTokenSource cancellationTokenSource)
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
            MacroStatusMessage = F(AppLanguageKeys.MainMacroStatusPlayFailedFormat, ex.Message);
            AddMacroLog(MacroStatusMessage);
        }
        finally
        {
            if (ReferenceEquals(mainMacroPlaybackCancellationTokenSource, cancellationTokenSource))
            {
                mainMacroPlaybackCancellationTokenSource = null;
            }

            mainMacroPlaybackTask = null;
            cancellationTokenSource.Dispose();
            IsMacroPlaying = false;
        }
    }

    private async Task StopMainMacroInfinitePlaybackAsync(string? message = null)
    {
        var cancellationTokenSource = mainMacroPlaybackCancellationTokenSource;
        var playbackTask = mainMacroPlaybackTask;
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

    private bool CanSaveMacro => HasRecordedMacro && !IsMacroRecording && !IsMacroPlaying;

    [RelayCommand(CanExecute = nameof(CanSaveMacro))]
    private async Task SaveMacroAsync()
    {
        try
        {
            var macro = macroService.CurrentMacro;
            if (macro is null)
            {
                MacroStatusMessage = L(AppLanguageKeys.MainMacroStatusNoRecordedToSave);
                AddMacroLog(L(AppLanguageKeys.MainMacroLogSaveRequestedNoMacro));
                return;
            }

            var suggestedName = string.IsNullOrWhiteSpace(MacroName) ? macro.Name : MacroName.Trim();
            var chosenName = macroNamePromptService.PromptForName(suggestedName);
            if (string.IsNullOrWhiteSpace(chosenName))
            {
                MacroStatusMessage = L(AppLanguageKeys.MainMacroStatusSaveCanceled);
                AddMacroLog(MacroStatusMessage);
                return;
            }

            var macroToSave = macro with { Name = chosenName.Trim() };
            var filePath = macroLibraryService.GetSavePath(macroToSave.Name);
            await macroFileStore.SaveAsync(filePath, macroToSave);
            macroService.SetCurrentMacro(macroToSave);
            ApplyLoadedMacro(macroToSave);
            RefreshSavedMacrosInternal(filePath);
            MacroStatusMessage = F(AppLanguageKeys.MainMacroStatusSavedToMacrosFormat, Path.GetFileName(filePath));
            AddMacroLog(F(AppLanguageKeys.MainMacroLogSavedToPathFormat, filePath));
        }
        catch (Exception ex)
        {
            MacroStatusMessage = F(AppLanguageKeys.MainMacroStatusSaveFailedFormat, ex.Message);
            AddMacroLog(MacroStatusMessage);
        }
    }

    private bool CanLoadMacro => !IsMacroRecording && !IsMacroPlaying;

    [RelayCommand(CanExecute = nameof(CanLoadMacro))]
    private async Task LoadMacroAsync()
    {
        try
        {
            var filePath = macroFileDialogService.PickOpenPath();
            if (string.IsNullOrWhiteSpace(filePath))
            {
                MacroStatusMessage = L(AppLanguageKeys.MainMacroStatusLoadCanceled);
                AddMacroLog(MacroStatusMessage);
                return;
            }

            var macro = await macroFileStore.LoadAsync(filePath);
            macroService.SetCurrentMacro(macro);
            ApplyLoadedMacro(macro);
            MacroStatusMessage = F(AppLanguageKeys.MainMacroStatusLoadedFromFileFormat, Path.GetFileName(filePath));
            AddMacroLog(F(AppLanguageKeys.MainMacroLogLoadedFromPathFormat, filePath));
        }
        catch (Exception ex)
        {
            MacroStatusMessage = F(AppLanguageKeys.MainMacroStatusLoadFailedFormat, ex.Message);
            AddMacroLog(MacroStatusMessage);
        }
    }

    private bool CanLoadSelectedSavedMacro => SelectedSavedMacro is not null && !IsMacroRecording && !IsMacroPlaying;

    [RelayCommand(CanExecute = nameof(CanLoadSelectedSavedMacro))]
    private async Task LoadSelectedSavedMacroAsync()
    {
        if (SelectedSavedMacro is null)
        {
            MacroStatusMessage = L(AppLanguageKeys.MainMacroStatusChooseSavedFirst);
            AddMacroLog(L(AppLanguageKeys.MainMacroLogLoadSelectedNoSaved));
            return;
        }

        try
        {
            var macro = await macroFileStore.LoadAsync(SelectedSavedMacro.FilePath);
            macroService.SetCurrentMacro(macro);
            ApplyLoadedMacro(macro);
            MacroStatusMessage = F(AppLanguageKeys.MainMacroStatusLoadedSavedFormat, SelectedSavedMacro.DisplayName);
            AddMacroLog(F(AppLanguageKeys.MainMacroLogLoadedSavedPathFormat, SelectedSavedMacro.FilePath));
        }
        catch (Exception ex)
        {
            MacroStatusMessage = F(AppLanguageKeys.MainMacroStatusLoadSavedFailedFormat, ex.Message);
            AddMacroLog(MacroStatusMessage);
        }
    }

    private bool CanEditSelectedSavedMacro => SelectedSavedMacro is not null && !IsMacroRecording && !IsMacroPlaying;

    [RelayCommand(CanExecute = nameof(CanEditSelectedSavedMacro))]
    private async Task EditSelectedSavedMacroAsync()
    {
        if (SelectedSavedMacro is null)
        {
            MacroStatusMessage = L(AppLanguageKeys.MainMacroStatusChooseSavedFirst);
            AddMacroLog(L(AppLanguageKeys.MainMacroLogEditRequestedNoSaved));
            return;
        }

        try
        {
            var originalPath = SelectedSavedMacro.FilePath;
            var macro = await macroFileStore.LoadAsync(originalPath);
            var editedMacro = macroEditorDialogService.Edit(macro);
            if (editedMacro is null)
            {
                MacroStatusMessage = L(AppLanguageKeys.MainMacroStatusEditCanceled);
                AddMacroLog(MacroStatusMessage);
                return;
            }

            var updatedPath = macroLibraryService.GetSavePath(editedMacro.Name);
            await macroFileStore.SaveAsync(updatedPath, editedMacro);

            if (!string.Equals(originalPath, updatedPath, StringComparison.OrdinalIgnoreCase) && File.Exists(originalPath))
            {
                File.Delete(originalPath);
            }

            macroService.SetCurrentMacro(editedMacro);
            ApplyLoadedMacro(editedMacro);
            UpdateAssignedMacroHotkeyPath(originalPath, updatedPath, editedMacro.Name);
            RefreshSavedMacrosInternal(updatedPath);
            MacroStatusMessage = F(AppLanguageKeys.MainMacroStatusSavedEditsFormat, editedMacro.Name);
            AddMacroLog(F(AppLanguageKeys.MainMacroLogSavedEditedToPathFormat, updatedPath));
        }
        catch (Exception ex)
        {
            MacroStatusMessage = F(AppLanguageKeys.MainMacroStatusEditSavedFailedFormat, ex.Message);
            AddMacroLog(MacroStatusMessage);
        }
    }

    [RelayCommand]
    private void RefreshSavedMacros()
    {
        RefreshSavedMacrosInternal();
        MacroStatusMessage = HasSavedMacros
            ? F(AppLanguageKeys.MainMacroStatusFoundSavedFormat, SavedMacros.Count, PluralSuffix(SavedMacros.Count))
            : L(AppLanguageKeys.MainMacroStatusNoSavedInDefaultFolder);
        AddMacroLog(MacroStatusMessage);
    }

    [RelayCommand]
    private void OpenSavedMacrosFolder()
    {
        try
        {
            Directory.CreateDirectory(macroLibraryService.DefaultDirectory);
            Process.Start(
                new ProcessStartInfo
                {
                    FileName = macroLibraryService.DefaultDirectory,
                    UseShellExecute = true,
                });

            MacroStatusMessage = L(AppLanguageKeys.MainMacroStatusOpenedSavedFolder);
            AddMacroLog(MacroStatusMessage);
        }
        catch (Exception ex)
        {
            MacroStatusMessage = F(AppLanguageKeys.MainMacroStatusOpenSavedFolderFailedFormat, ex.Message);
            AddMacroLog(MacroStatusMessage);
        }
    }

    [RelayCommand]
    private void ClearMacroLog()
    {
        MacroLogEntries.Clear();
        AddMacroLog(L(AppLanguageKeys.MainMacroStatusLogCleared));
        MacroStatusMessage = L(AppLanguageKeys.MainMacroStatusLogCleared);
    }

    public void CaptureMacroHotkey(Key key, ModifierKeys modifiers)
    {
        var capturedKey = key == Key.System ? Key.None : key;
        var binding = HotkeyDisplayNameFormatter.CreateKeyboardBinding(capturedKey, modifiers);
        if (binding.VirtualKey <= 0)
        {
            return;
        }

        MacroHotkeyVirtualKey = binding.VirtualKey;
        MacroHotkeyModifiers = binding.Modifiers;
        MacroHotkeyDisplay = binding.DisplayName;
        MacroStatusMessage = F(AppLanguageKeys.MainMacroStatusHotkeySetFormat, MacroHotkeyDisplay);
        AddMacroLog(MacroStatusMessage);
        HotkeysChanged?.Invoke(this, EventArgs.Empty);
    }

    public void CaptureMacroRecordHotkey(Key key, ModifierKeys modifiers)
    {
        var capturedKey = key == Key.System ? Key.None : key;
        var binding = HotkeyDisplayNameFormatter.CreateKeyboardBinding(capturedKey, modifiers);
        if (binding.VirtualKey <= 0)
        {
            return;
        }

        MacroRecordHotkeyVirtualKey = binding.VirtualKey;
        MacroRecordHotkeyModifiers = binding.Modifiers;
        MacroRecordHotkeyDisplay = binding.DisplayName;
        MacroStatusMessage = F(AppLanguageKeys.MainMacroStatusRecordHotkeySetFormat, MacroRecordHotkeyDisplay);
        AddMacroLog(MacroStatusMessage);
        HotkeysChanged?.Invoke(this, EventArgs.Empty);
    }


    private void RefreshMacroCommandStates()
    {
        NewMacroCommand.NotifyCanExecuteChanged();
        StartMacroRecordingCommand.NotifyCanExecuteChanged();
        StopMacroRecordingCommand.NotifyCanExecuteChanged();
        PlayMacroCommand.NotifyCanExecuteChanged();
        SaveMacroCommand.NotifyCanExecuteChanged();
        LoadMacroCommand.NotifyCanExecuteChanged();
        LoadSelectedSavedMacroCommand.NotifyCanExecuteChanged();
        EditSelectedSavedMacroCommand.NotifyCanExecuteChanged();
        ManageMacroHotkeysCommand.NotifyCanExecuteChanged();
        AssignSelectedMacroHotkeyCommand.NotifyCanExecuteChanged();
    }


    private void AddMacroLog(string message)
    {
        MacroLogEntries.Insert(0, $"{DateTime.Now:HH:mm:ss}  {message}");
        AddActivityLog(message);
    }


    private void ApplyLoadedMacro(RecordedMacro macro)
    {
        MacroName = macro.Name;
        HasRecordedMacro = macro.Events.Count > 0;
        MacroSummaryText = BuildMacroSummary(macro);
    }

    private static string BuildMacroSummary(RecordedMacro macro) =>
        macro.Events.Count > 0
            ? AppLanguageStrings.FormatForCurrentLanguage(AppLanguageKeys.MainMacroSummaryRecordedFormat, macro.Name, macro.Events.Count, macro.Duration.TotalMilliseconds)
            : AppLanguageStrings.FormatForCurrentLanguage(AppLanguageKeys.MainMacroSummaryNoInputCapturedFormat, macro.Name);

    private void RefreshSavedMacrosInternal(string? preferredPath = null)
    {
        IReadOnlyList<SavedMacroEntry> savedMacros;
        try
        {
            savedMacros = macroLibraryService.GetSavedMacros();
        }
        catch (Exception ex)
        {
            SavedMacros.Clear();
            SelectedSavedMacro = null;
            OnPropertyChanged(nameof(HasSavedMacros));
            MacroStatusMessage = F(AppLanguageKeys.MainMacroStatusSavedFolderUnavailableFormat, ex.Message);
            AddMacroLog(MacroStatusMessage);
            return;
        }

        var selectedPath = preferredPath ?? SelectedSavedMacro?.FilePath;

        SavedMacros.Clear();
        foreach (var savedMacro in savedMacros)
        {
            SavedMacros.Add(savedMacro);
        }

        SelectedSavedMacro = savedMacros.FirstOrDefault(macro => string.Equals(macro.FilePath, selectedPath, StringComparison.OrdinalIgnoreCase))
            ?? savedMacros.FirstOrDefault();

        OnPropertyChanged(nameof(HasSavedMacros));
        LoadSelectedSavedMacroCommand.NotifyCanExecuteChanged();
        ManageMacroHotkeysCommand.NotifyCanExecuteChanged();
        AssignSelectedMacroHotkeyCommand.NotifyCanExecuteChanged();
        SynchronizeAssignedMacroHotkeysWithSavedMacros();
    }

}
