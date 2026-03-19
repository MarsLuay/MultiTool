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
    public async Task InitializeAsync()
    {
        if (initialized)
        {
            return;
        }

        var settings = await settingsStore.LoadAsync();
        var currentRunAtStartupState = GetRunAtStartupStateOrFallback(false);
        var resolvedRunAtStartupSetting = settings.Ui.RunAtStartup ?? currentRunAtStartupState;
        ApplySettings(settings, resolvedRunAtStartupSetting);
        var shouldPersistMigratedRunAtStartupSetting = settings.Ui.RunAtStartup is null && currentRunAtStartupState;
        if (settings.Ui.RunAtStartup is not null || currentRunAtStartupState)
        {
            TryApplyRunAtStartupSetting(
                resolvedRunAtStartupSetting,
                updateStatusOnSuccess: false,
                addActivityLogOnSuccess: false,
                scheduleSaveOnSuccess: false);
        }

        RefreshSavedMacrosInternal();
        StatusMessage = L(AppLanguageKeys.MainStatusReady);
        AddActivityLog(L(AppLanguageKeys.MainActivitySettingsLoaded));
        initialized = true;

        if (shouldPersistMigratedRunAtStartupSetting)
        {
            await SaveSettingsAsync(L(AppLanguageKeys.MainStatusSettingsAutoSaved), updateStatusOnSuccess: false, addActivityLogOnSuccess: false);
        }

        StartInstallerInitialization();
    }

    public HotkeySettings CurrentHotkeys => hotkeySettings.Clone();

    public ScreenshotSettings CurrentScreenshotSettings => BuildScreenshotSettings();

    public MacroSettings CurrentMacroSettings => BuildMacroSettings();

    public Task<bool> AutoSaveAsync() => SaveSettingsAsync(L(AppLanguageKeys.MainStatusSettingsAutoSaved), updateStatusOnSuccess: false, addActivityLogOnSuccess: false);


    public async Task HandleHotkeyAsync(HotkeyAction action, string? payload = null)
    {
        switch (action)
        {
            case HotkeyAction.Toggle:
                await ToggleAsync();
                return;
            case HotkeyAction.ForceStop:
                await ForceStopAsync();
                return;
            case HotkeyAction.ScreenshotCapture:
                await HandleScreenshotCaptureHotkeyAsync();
                return;
            case HotkeyAction.ScreenshotOptions:
                await HandleScreenshotCaptureHotkeyAsync();
                return;
            case HotkeyAction.MacroPlay:
                await PlayMacroAsync();
                return;
            case HotkeyAction.MacroRecordToggle:
                await ToggleMacroRecordingAsync();
                return;
            case HotkeyAction.MacroAssigned:
                await HandleAssignedMacroHotkeyAsync(payload);
                return;
            case HotkeyAction.WindowPinToggle:
                ToggleWindowPinFromHotkey();
                return;
            default:
                throw new NotSupportedException($"Hotkey action {action} is not supported.");
        }
    }

    private void ApplySettings(AppSettings settings, bool resolvedRunAtStartupSetting = false)
    {
        Hours = settings.Clicker.Hours;
        Minutes = settings.Clicker.Minutes;
        Seconds = settings.Clicker.Seconds;
        Milliseconds = settings.Clicker.Milliseconds;
        IsRandomTimingEnabled = settings.Clicker.IsRandomTimingEnabled;
        RandomTimingVarianceMilliseconds = settings.Clicker.RandomTimingVarianceMilliseconds;
        SelectedMouseButton = settings.Clicker.MouseButton;
        CustomInputKind = settings.Clicker.CustomInputKind;
        CustomKeyVirtualKey = settings.Clicker.CustomKeyVirtualKey;
        CustomMouseButton = settings.Clicker.CustomMouseButton;
        CustomKeyDisplayText = GetCustomKeyDisplayText(settings.Clicker);
        SelectedClickKind = settings.Clicker.ClickType;
        SelectedRepeatMode = settings.Clicker.RepeatMode;
        SelectedLocationMode = settings.Clicker.LocationMode;
        FixedX = settings.Clicker.FixedX;
        FixedY = settings.Clicker.FixedY;
        RepeatCount = settings.Clicker.RepeatCount;
        IsTopMost = settings.Clicker.AlwaysOnTop;
        hotkeySettings = settings.Hotkeys.Clone();
        ScreenshotFolderPath = settings.Screenshot.SaveFolderPath;
        ScreenshotFilePrefix = settings.Screenshot.FilePrefix;
        ScreenshotHotkeyVirtualKey = settings.Screenshot.CaptureHotkey.VirtualKey;
        ScreenshotHotkeyDisplay = settings.Screenshot.CaptureHotkey.DisplayName;
        MacroHotkeyVirtualKey = settings.Macro.PlayHotkey.VirtualKey;
        MacroHotkeyDisplay = settings.Macro.PlayHotkey.DisplayName;
        MacroRecordHotkeyVirtualKey = settings.Macro.RecordHotkey.VirtualKey;
        MacroRecordHotkeyDisplay = settings.Macro.RecordHotkey.DisplayName;
        RecordMacroMouseMovement = settings.Macro.RecordMouseMovement;
        SetMacroHotkeyAssignments(settings.Macro.AssignedHotkeys);
        ApplyInstallerSettings(settings.Installer);
        shortcutHotkeyScanMaxFolderCountCache = Math.Max(settings.Tools.ShortcutHotkeyScanMaxFolderCount, 0);
        emptyDirectoryScanMaxFolderCountCache = new Dictionary<string, int>(
            settings.Tools.EmptyDirectoryScanMaxFolderCounts,
            StringComparer.OrdinalIgnoreCase);
        suppressThemeChange = true;
        IsDarkMode = settings.Ui.IsDarkMode ?? themeService.GetSystemPrefersDarkMode();
        IsCtrlWheelResizeEnabled = settings.Ui.EnableCtrlWheelResize;
        IsRunAtStartupEnabled = resolvedRunAtStartupSetting;
        IsAutoHideOnStartupEnabled = settings.Ui.AutoHideOnStartup;
        IsSillyModeEnabled = settings.Ui.SillyMode;
        OnPropertyChanged(nameof(AppearanceHelperText));
        OnPropertyChanged(nameof(ClickerTabHeaderText));
        OnPropertyChanged(nameof(ClickerHotkeyLabelText));
        OnPropertyChanged(nameof(ClickerForceStopHelperText));
        OnPropertyChanged(nameof(ScreenshotTabHeaderText));
        OnPropertyChanged(nameof(MacroTabHeaderText));
        OnPropertyChanged(nameof(MacroSetShortcutButtonText));
        OnPropertyChanged(nameof(MacroEditShortcutsButtonText));
        OnPropertyChanged(nameof(ToolsTabHeaderText));
        OnPropertyChanged(nameof(NicheToolsHeaderText));
        OnPropertyChanged(nameof(ShortcutKeyExplorerHeaderText));
        OnPropertyChanged(nameof(ShortcutExplorerButtonText));
        OnPropertyChanged(nameof(InstallerTabHeaderText));
        OnPropertyChanged(nameof(SettingsTabHeaderText));
        OnPropertyChanged(nameof(AppearanceHeaderText));
        OnPropertyChanged(nameof(DarkModeLabelText));
        OnPropertyChanged(nameof(CtrlWheelResizeLabelText));
        OnPropertyChanged(nameof(AlwaysOnTopLabelText));
        OnPropertyChanged(nameof(CatTranslatorLabelText));
        OnPropertyChanged(nameof(RunAtStartupLabelText));
        OnPropertyChanged(nameof(AutoHideOnStartupLabelText));
        OnPropertyChanged(nameof(ResetAllSettingsButtonText));
        OnPropertyChanged(nameof(BugCheckingHeaderText));
        OnPropertyChanged(nameof(BugCheckingHelperText));
        OnPropertyChanged(nameof(CopyLogButtonText));
        suppressThemeChange = false;
        themeService.ApplyTheme(IsDarkMode);
        SettingsStatusMessage = IsDarkMode
            ? L(AppLanguageKeys.MainSettingsStatusDarkModeOn)
            : L(AppLanguageKeys.MainSettingsStatusDarkModeOff);
        RefreshHotkeyLabels();
    }

    private ClickSettings BuildClickSettings() =>
        new()
        {
            Hours = Hours,
            Minutes = Minutes,
            Seconds = Seconds,
            Milliseconds = Milliseconds,
            IsRandomTimingEnabled = IsRandomTimingEnabled,
            RandomTimingVarianceMilliseconds = RandomTimingVarianceMilliseconds,
            MouseButton = SelectedMouseButton,
            CustomInputKind = CustomInputKind,
            CustomKeyVirtualKey = CustomKeyVirtualKey,
            CustomKeyDisplayName = GetCustomKeyDisplayName(),
            CustomMouseButton = CustomMouseButton,
            ClickType = SelectedClickKind,
            RepeatMode = SelectedRepeatMode,
            LocationMode = SelectedLocationMode,
            FixedX = FixedX,
            FixedY = FixedY,
            RepeatCount = RepeatCount,
            AlwaysOnTop = IsTopMost,
        };

    private AppSettings BuildAppSettings() =>
        new()
        {
            Clicker = BuildClickSettings(),
            Hotkeys = hotkeySettings.Clone(),
            Screenshot = BuildScreenshotSettings(),
            Macro = BuildMacroSettings(),
            Installer = BuildInstallerSettings(),
            Tools = BuildToolSettings(),
            Ui = new UiSettings
            {
                IsDarkMode = IsDarkMode,
                EnableCtrlWheelResize = IsCtrlWheelResizeEnabled,
                RunAtStartup = IsRunAtStartupEnabled,
                AutoHideOnStartup = IsAutoHideOnStartupEnabled,
                SillyMode = IsSillyModeEnabled,
            },
        };

    private ScreenshotSettings BuildScreenshotSettings() =>
        new()
        {
            CaptureHotkey = new HotkeyBinding(
                virtualKey: ScreenshotHotkeyVirtualKey <= 0 ? ScreenshotSettings.DefaultCaptureVirtualKey : ScreenshotHotkeyVirtualKey,
                displayName: string.IsNullOrWhiteSpace(ScreenshotHotkeyDisplay) ? ScreenshotSettings.DefaultCaptureDisplayName : ScreenshotHotkeyDisplay),
            SaveFolderPath = ScreenshotFolderPath,
            FilePrefix = ScreenshotFilePrefix,
        };

    private MacroSettings BuildMacroSettings() =>
        new()
        {
            PlayHotkey = new HotkeyBinding(
                virtualKey: MacroHotkeyVirtualKey <= 0 ? MacroSettings.DefaultPlayVirtualKey : MacroHotkeyVirtualKey,
                displayName: string.IsNullOrWhiteSpace(MacroHotkeyDisplay) ? MacroSettings.DefaultPlayDisplayName : MacroHotkeyDisplay),
            RecordHotkey = new HotkeyBinding(
                virtualKey: MacroRecordHotkeyVirtualKey <= 0 ? MacroSettings.DefaultRecordVirtualKey : MacroRecordHotkeyVirtualKey,
                displayName: string.IsNullOrWhiteSpace(MacroRecordHotkeyDisplay) ? MacroSettings.DefaultRecordDisplayName : MacroRecordHotkeyDisplay),
            RecordMouseMovement = RecordMacroMouseMovement,
            AssignedHotkeys = macroHotkeyAssignments.Select(assignment => assignment.Clone()).ToList(),
        };

    private ToolSettings BuildToolSettings() =>
        new()
        {
            ShortcutHotkeyScanMaxFolderCount = Math.Max(shortcutHotkeyScanMaxFolderCountCache, 0),
            EmptyDirectoryScanMaxFolderCounts = new Dictionary<string, int>(
                emptyDirectoryScanMaxFolderCountCache,
                StringComparer.OrdinalIgnoreCase),
        };

    private bool GetRunAtStartupStateOrFallback(bool fallback)
    {
        try
        {
            return runAtStartupService.IsEnabled();
        }
        catch
        {
            return fallback;
        }
    }

    private bool TryApplyRunAtStartupSetting(
        bool enabled,
        bool updateStatusOnSuccess,
        bool addActivityLogOnSuccess,
        bool scheduleSaveOnSuccess)
    {
        try
        {
            runAtStartupService.SetEnabled(enabled);
        }
        catch (Exception ex)
        {
            SetRunAtStartupEnabledWithoutSideEffects(GetRunAtStartupStateOrFallback(enabled));

            var failureMessage = F(AppLanguageKeys.MainSettingsStatusRunAtStartupFailedFormat, ex.Message);
            SettingsStatusMessage = failureMessage;

            if (initialized)
            {
                AddActivityLog(failureMessage);
            }

            return false;
        }

        if (updateStatusOnSuccess)
        {
            SettingsStatusMessage = enabled
                ? L(AppLanguageKeys.MainSettingsStatusRunAtStartupOn)
                : L(AppLanguageKeys.MainSettingsStatusRunAtStartupOff);
        }

        if (scheduleSaveOnSuccess)
        {
            ScheduleSettingsAutoSave();
        }

        if (addActivityLogOnSuccess)
        {
            AddActivityLog(enabled
                ? L(AppLanguageKeys.MainActivityRunAtStartupEnabled)
                : L(AppLanguageKeys.MainActivityRunAtStartupDisabled));
        }

        return true;
    }

    private void SetRunAtStartupEnabledWithoutSideEffects(bool value)
    {
        suppressRunAtStartupChangeHandling = true;
        try
        {
            IsRunAtStartupEnabled = value;
        }
        finally
        {
            suppressRunAtStartupChangeHandling = false;
        }
    }

    private void RefreshHotkeyLabels()
    {
        OnPropertyChanged(nameof(ClickerHotkeyDisplay));
        OnPropertyChanged(nameof(PinWindowHotkeyLabel));
    }

    private string GetPinWindowHotkeyDisplay() =>
        hotkeySettings.PinWindow.VirtualKey > 0 && !string.IsNullOrWhiteSpace(hotkeySettings.PinWindow.DisplayName)
            ? hotkeySettings.PinWindow.DisplayName
            : HotkeySettings.UnassignedDisplayName;

    partial void OnSelectedMouseButtonChanged(ClickMouseButton value)
    {
        OnPropertyChanged(nameof(IsCustomKeySelected));
        OnPropertyChanged(nameof(UsesMousePositionSettings));
        ScheduleSettingsAutoSave();
    }

    partial void OnCustomInputKindChanged(CustomInputKind value)
    {
        OnPropertyChanged(nameof(UsesMousePositionSettings));
        ScheduleSettingsAutoSave();
    }

    partial void OnHoursChanged(int value)
    {
        ScheduleSettingsAutoSave();
    }

    partial void OnMinutesChanged(int value)
    {
        ScheduleSettingsAutoSave();
    }

    partial void OnSecondsChanged(int value)
    {
        ScheduleSettingsAutoSave();
    }

    partial void OnMillisecondsChanged(int value)
    {
        ScheduleSettingsAutoSave();
    }

    partial void OnIsRandomTimingEnabledChanged(bool value)
    {
        OnPropertyChanged(nameof(IsRandomTimingVarianceEnabled));
        ScheduleSettingsAutoSave();
    }

    partial void OnRandomTimingVarianceMillisecondsChanged(int value)
    {
        ScheduleSettingsAutoSave();
    }

    partial void OnCustomKeyVirtualKeyChanged(int value)
    {
        ScheduleSettingsAutoSave();
    }

    partial void OnCustomKeyDisplayTextChanged(string value)
    {
        ScheduleSettingsAutoSave();
    }

    partial void OnCustomMouseButtonChanged(ClickMouseButton value)
    {
        ScheduleSettingsAutoSave();
    }

    partial void OnSelectedClickKindChanged(ClickKind value)
    {
        ScheduleSettingsAutoSave();
    }

    partial void OnSelectedRepeatModeChanged(RepeatMode value)
    {
        OnPropertyChanged(nameof(IsRepeatCountEnabled));
        ScheduleSettingsAutoSave();
    }

    partial void OnSelectedLocationModeChanged(ClickLocationMode value)
    {
        ScheduleSettingsAutoSave();
    }

    partial void OnFixedXChanged(int value)
    {
        ScheduleSettingsAutoSave();
    }

    partial void OnFixedYChanged(int value)
    {
        ScheduleSettingsAutoSave();
    }

    partial void OnRepeatCountChanged(int value)
    {
        ScheduleSettingsAutoSave();
    }

    partial void OnIsTopMostChanged(bool value)
    {
        OnPropertyChanged(nameof(IsWindowPinned));
        ScheduleSettingsAutoSave();
    }

    partial void OnScreenshotFolderPathChanged(string value)
    {
        ScheduleSettingsAutoSave();
    }

    partial void OnScreenshotFilePrefixChanged(string value)
    {
        ScheduleSettingsAutoSave();
    }

    partial void OnScreenshotHotkeyDisplayChanged(string value)
    {
        ScheduleSettingsAutoSave();
    }

    partial void OnScreenshotHotkeyVirtualKeyChanged(int value)
    {
        ScheduleSettingsAutoSave();
    }

    partial void OnMacroHotkeyDisplayChanged(string value)
    {
        OnPropertyChanged(nameof(MacroInfiniteHelperText));
        ScheduleSettingsAutoSave();
    }

    partial void OnMacroHotkeyVirtualKeyChanged(int value)
    {
        ScheduleSettingsAutoSave();
    }

    partial void OnMacroRecordHotkeyDisplayChanged(string value)
    {
        ScheduleSettingsAutoSave();
    }

    partial void OnMacroRecordHotkeyVirtualKeyChanged(int value)
    {
        ScheduleSettingsAutoSave();
    }

    partial void OnRecordMacroMouseMovementChanged(bool value)
    {
        ScheduleSettingsAutoSave();
    }

    partial void OnSelectedSavedMacroChanged(SavedMacroEntry? value)
    {
        LoadSelectedSavedMacroCommand.NotifyCanExecuteChanged();
        EditSelectedSavedMacroCommand.NotifyCanExecuteChanged();
        AssignSelectedMacroHotkeyCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(HasSelectedSavedMacroHotkey));
        OnPropertyChanged(nameof(SelectedSavedMacroHotkeySummary));
    }

    partial void OnLatestScreenshotPreviewChanged(ImageSource? value)
    {
        OnPropertyChanged(nameof(HasLatestScreenshot));
        OnPropertyChanged(nameof(IsLatestMediaVideo));
    }

    partial void OnLatestVideoPathChanged(string? value)
    {
        OnPropertyChanged(nameof(HasLatestVideo));
        OnPropertyChanged(nameof(IsLatestMediaVideo));
        OnPropertyChanged(nameof(LatestVideoSource));
    }

    partial void OnIsMacroRecordingChanged(bool value)
    {
        RefreshMacroCommandStates();
    }

    partial void OnHasRecordedMacroChanged(bool value)
    {
        RefreshMacroCommandStates();
    }

    partial void OnIsMacroPlayingChanged(bool value)
    {
        RefreshMacroCommandStates();
        OnPropertyChanged(nameof(PlayButtonText));
    }

    partial void OnIsMacroPlaybackInfiniteChanged(bool value)
    {
        OnPropertyChanged(nameof(IsMacroPlaybackCountEnabled));
    }

    partial void OnIsDarkModeChanged(bool value)
    {
        if (suppressThemeChange)
        {
            return;
        }

        themeService.ApplyTheme(value);
        SettingsStatusMessage = value
            ? L(AppLanguageKeys.MainSettingsStatusDarkModeOn)
            : L(AppLanguageKeys.MainSettingsStatusDarkModeOff);
        ScheduleSettingsAutoSave();

        if (initialized)
        {
            AddActivityLog(value
                ? L(AppLanguageKeys.MainActivityDarkModeEnabled)
                : L(AppLanguageKeys.MainActivityDarkModeDisabled));
        }
    }

    partial void OnIsCtrlWheelResizeEnabledChanged(bool value)
    {
        if (suppressThemeChange)
        {
            return;
        }

        SettingsStatusMessage = value
            ? L(AppLanguageKeys.MainSettingsStatusCtrlWheelZoomOn)
            : L(AppLanguageKeys.MainSettingsStatusCtrlWheelZoomOff);
        ScheduleSettingsAutoSave();

        if (initialized)
        {
            AddActivityLog(value
                ? L(AppLanguageKeys.MainActivityCtrlWheelZoomEnabled)
                : L(AppLanguageKeys.MainActivityCtrlWheelZoomDisabled));
        }
    }

    partial void OnIsRunAtStartupEnabledChanged(bool value)
    {
        if (suppressThemeChange || suppressRunAtStartupChangeHandling)
        {
            return;
        }

        TryApplyRunAtStartupSetting(
            value,
            updateStatusOnSuccess: true,
            addActivityLogOnSuccess: initialized,
            scheduleSaveOnSuccess: true);
    }

    partial void OnIsAutoHideOnStartupEnabledChanged(bool value)
    {
        if (suppressThemeChange)
        {
            return;
        }

        SettingsStatusMessage = value
            ? L(AppLanguageKeys.MainSettingsStatusAutoHideOn)
            : L(AppLanguageKeys.MainSettingsStatusAutoHideOff);
        ScheduleSettingsAutoSave();

        if (initialized)
        {
            AddActivityLog(value
                ? L(AppLanguageKeys.MainActivityAutoHideEnabled)
                : L(AppLanguageKeys.MainActivityAutoHideDisabled));
        }
    }

    partial void OnIsSillyModeEnabledChanged(bool value)
    {
        if (suppressThemeChange)
        {
            return;
        }

        OnPropertyChanged(nameof(AppearanceHelperText));
        OnPropertyChanged(nameof(ClickerTabHeaderText));
        OnPropertyChanged(nameof(ClickerHotkeyLabelText));
        OnPropertyChanged(nameof(ClickerForceStopHelperText));
        OnPropertyChanged(nameof(ScreenshotTabHeaderText));
        OnPropertyChanged(nameof(MacroTabHeaderText));
        OnPropertyChanged(nameof(MacroSetShortcutButtonText));
        OnPropertyChanged(nameof(MacroEditShortcutsButtonText));
        OnPropertyChanged(nameof(ToolsTabHeaderText));
        OnPropertyChanged(nameof(NicheToolsHeaderText));
        OnPropertyChanged(nameof(ShortcutKeyExplorerHeaderText));
        OnPropertyChanged(nameof(ShortcutExplorerButtonText));
        OnPropertyChanged(nameof(InstallerTabHeaderText));
        OnPropertyChanged(nameof(SettingsTabHeaderText));
        OnPropertyChanged(nameof(AppearanceHeaderText));
        OnPropertyChanged(nameof(DarkModeLabelText));
        OnPropertyChanged(nameof(CtrlWheelResizeLabelText));
        OnPropertyChanged(nameof(AlwaysOnTopLabelText));
        OnPropertyChanged(nameof(CatTranslatorLabelText));
        OnPropertyChanged(nameof(RunAtStartupLabelText));
        OnPropertyChanged(nameof(AutoHideOnStartupLabelText));
        OnPropertyChanged(nameof(ResetAllSettingsButtonText));
        OnPropertyChanged(nameof(BugCheckingHeaderText));
        OnPropertyChanged(nameof(BugCheckingHelperText));
        OnPropertyChanged(nameof(CopyLogButtonText));

        SettingsStatusMessage = value
            ? L(AppLanguageKeys.MainSettingsStatusCatTranslatorOn)
            : (IsDarkMode
                ? L(AppLanguageKeys.MainSettingsStatusDarkModeOn)
                : L(AppLanguageKeys.MainSettingsStatusDarkModeOff));
        ScheduleSettingsAutoSave();

        if (initialized)
        {
            AddActivityLog(value
                ? L(AppLanguageKeys.MainActivityCatTranslatorEnabled)
                : L(AppLanguageKeys.MainActivityCatTranslatorDisabled));
        }
    }

    partial void OnIsRunningAsAdministratorChanged(bool value)
    {
        OnPropertyChanged(nameof(ShouldShowAdminModeBanner));
        OnPropertyChanged(nameof(AdminModeBannerText));
        RefreshWindowTitle();
    }


    private void AddActivityLog(string message)
    {
        ActivityLogEntries.Insert(0, $"{DateTime.Now:HH:mm:ss}  {message}");

        while (ActivityLogEntries.Count > 200)
        {
            ActivityLogEntries.RemoveAt(ActivityLogEntries.Count - 1);
        }
    }

    private void RefreshAdminModeState()
    {
        IsRunningAsAdministrator = GetIsCurrentProcessElevated();
        RefreshWindowTitle();
        if (!IsRunningAsAdministrator)
        {
            AddActivityLog(L(AppLanguageKeys.MainAdminActivityNotAdmin));
        }
    }

    private void RefreshWindowTitle()
    {
        WindowTitle = IsRunning
            ? L(AppLanguageKeys.MainWindowTitleRunning)
            : L(AppLanguageKeys.MainWindowTitleDefault);

        if (!IsRunningAsAdministrator)
        {
            WindowTitle += L(AppLanguageKeys.MainWindowTitleNotAdminSuffix);
        }
    }

    private static bool GetIsCurrentProcessElevated()
    {
        using var identity = WindowsIdentity.GetCurrent();
        return identity is not null && new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
    }


    private void CopyActivityLog()
    {
        var text = ActivityLogEntries.Count > 0
            ? string.Join(Environment.NewLine, ActivityLogEntries)
            : L(AppLanguageKeys.MainActivityLogEmpty);

        clipboardTextService.SetText(text);
        SettingsStatusMessage = L(AppLanguageKeys.MainSettingsStatusCopiedActivityLog);
        AddActivityLog(SettingsStatusMessage);
    }

    [RelayCommand]
    private async Task ResetAllSettingsAsync()
    {
        var defaults = DefaultSettingsFactory.Create();
        defaults.Ui.RunAtStartup = false;
        ApplySettings(defaults, resolvedRunAtStartupSetting: false);
        TryApplyRunAtStartupSetting(
            enabled: false,
            updateStatusOnSuccess: false,
            addActivityLogOnSuccess: false,
            scheduleSaveOnSuccess: false);
        HotkeysChanged?.Invoke(this, EventArgs.Empty);

        var saved = await SaveSettingsAsync(L(AppLanguageKeys.MainSettingsStatusResetRequested), updateStatusOnSuccess: true);
        SettingsStatusMessage = saved
            ? L(AppLanguageKeys.MainSettingsStatusResetCompleted)
            : L(AppLanguageKeys.MainSettingsStatusResetSaveFailed);
    }


    private async Task<bool> SaveSettingsAsync(string successMessage, bool updateStatusOnSuccess, bool addActivityLogOnSuccess = true)
    {
        if (!initialized)
        {
            return false;
        }

        var settings = BuildAppSettings();
        var validation = settingsValidator.Validate(settings);
        if (!validation.IsValid)
        {
            StatusMessage = validation.Summary;
            return false;
        }

        await saveLock.WaitAsync();

        try
        {
            await settingsStore.SaveAsync(settings);
        }
        catch (Exception ex)
        {
            StatusMessage = F(AppLanguageKeys.MainStatusUnableSaveSettingsFormat, ex.Message);
            return false;
        }
        finally
        {
            saveLock.Release();
        }

        if (updateStatusOnSuccess)
        {
            StatusMessage = successMessage;
        }

        if (addActivityLogOnSuccess)
        {
            AddActivityLog(successMessage);
        }

        return true;
    }

    private void ScheduleSettingsAutoSave()
    {
        if (!initialized)
        {
            return;
        }

        pendingAutoSaveCancellationTokenSource?.Cancel();
        pendingAutoSaveCancellationTokenSource?.Dispose();
        pendingAutoSaveCancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = pendingAutoSaveCancellationTokenSource.Token;

        _ = RunDebouncedSettingsAutoSaveAsync(cancellationToken);
    }

    private async Task RunDebouncedSettingsAutoSaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(450, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            if (synchronizationContext is null)
            {
                await AutoSaveAsync();
                return;
            }

            var completionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            synchronizationContext.Post(
                async _ =>
                {
                    try
                    {
                        await AutoSaveAsync();
                        completionSource.SetResult();
                    }
                    catch (Exception ex)
                    {
                        completionSource.SetException(ex);
                    }
                },
                null);

            await completionSource.Task;
        }
        catch (OperationCanceledException)
        {
        }
    }

}
