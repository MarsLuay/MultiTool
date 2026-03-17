using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using AutoClicker.App.Localization;
using AutoClicker.App.Models;
using AutoClicker.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AutoClicker.App.ViewModels;

public partial class MainWindowViewModel
{
    private const string WindowsUpdateOptionalUpdatesSettingsUri = "ms-settings:windowsupdate-optionalupdates";
    private const string WindowsUpdateSettingsUri = "ms-settings:windowsupdate";
    private const string UsoClientExecutable = "UsoClient.exe";

    private static readonly IReadOnlyList<UsefulSiteItem> DefaultUsefulSites =
    [
        new(
            AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.ToolsUsefulSiteFmhyName),
            "https://fmhy.net/",
            AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.ToolsUsefulSiteFmhyDescription)),
        new(
            AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.ToolsUsefulSiteSevenSeasName),
            "https://rentry.co/megathread",
            AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.ToolsUsefulSiteSevenSeasDescription)),
        new(
            AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.ToolsUsefulSiteZLibraryName),
            "http://zlibrary24tuxziyiyfr7zd46ytefdqbqd2axkmxm4o5374ptpc52fad.onion/",
            AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.ToolsUsefulSiteZLibraryDescription)),
        new(
            AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.ToolsUsefulSiteFmhyBackupName),
            "https://rentry.co/FMHYB64",
            AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.ToolsUsefulSiteFmhyBackupDescription)),
    ];

    public ObservableCollection<EmptyDirectoryItem> EmptyDirectoryCandidates { get; } = [];

    public ObservableCollection<int> MouseSensitivityLevels { get; } = [];

    public ObservableCollection<DisplayRefreshRecommendationItem> DisplayRefreshRecommendations { get; } = [];

    public ObservableCollection<HardwareDisplayAdapterInfo> HardwareGraphicsAdapters { get; } = [];

    public ObservableCollection<HardwareStorageDriveInfo> HardwareStorageDrives { get; } = [];

    public ObservableCollection<HardwarePartitionInfo> HardwareStoragePartitions { get; } = [];

    public ObservableCollection<HardwareSensorInfo> HardwareSensors { get; } = [];

    public ObservableCollection<HardwarePciDeviceInfo> HardwarePciDevices { get; } = [];

    public ObservableCollection<HardwareRaidInfo> HardwareRaidDetails { get; } = [];

    public ObservableCollection<DriverHardwareInfo> DriverHardwareInventory { get; } = [];

    public ObservableCollection<DriverUpdateItem> DriverUpdateCandidates { get; } = [];

    public ObservableCollection<UsefulSiteItem> UsefulSites { get; } = [];

    public ObservableCollection<string> ToolLogEntries { get; } = [];

    public bool HasSelectedDriverUpdates => DriverUpdateCandidates.Any(item => item.IsSelected);

    public bool HasDisplayRefreshRecommendations => DisplayRefreshRecommendations.Count > 0;

    public bool HasHardwareStoragePartitions => HardwareStoragePartitions.Count > 0;

    public bool HasHardwareSensors => HardwareSensors.Count > 0;

    public bool HasHardwarePciDevices => HardwarePciDevices.Count > 0;

    public bool HasHardwareRaidDetails => HardwareRaidDetails.Count > 0;

    public bool HasSelectedEmptyDirectories => EmptyDirectoryCandidates.Any(item => item.IsSelected);

    public string MouseSensitivitySummary => BuildMouseSensitivitySummary();

    public string MouseSensitivitySelectedLevelLabel => BuildMouseSensitivityLevelText(SelectedMouseSensitivityLevel);

    public string MouseSensitivitySelectionGuidance => BuildMouseSensitivitySelectionGuidance();

    public string DisplayRefreshSummary => BuildDisplayRefreshSummary();

    public string HardwareGraphicsSummary => BuildHardwareGraphicsSummary();

    public string HardwareStorageSummary => BuildHardwareStorageSummary();

    public string HardwarePartitionSummary => BuildHardwarePartitionSummary();

    public string HardwareSensorSummary => BuildHardwareSensorSummary();

    public string HardwarePciSummary => BuildHardwarePciSummary();

    public string HardwareRaidSummary => BuildHardwareRaidSummary();

    public string DriverHardwareSummary => BuildDriverHardwareSummary();

    public string DriverUpdateSelectionSummary => BuildDriverUpdateSelectionSummary();

    public string EmptyDirectorySelectionSummary => BuildEmptyDirectorySelectionSummary();

    public string EmptyDirectoryScanProgressSummary => BuildEmptyDirectoryScanProgressSummary();

    public string ShortcutHotkeyScanProgressSummary => BuildShortcutHotkeyScanProgressSummary();

    public string UsefulSitesToggleText => AreUsefulSitesVisible
        ? L(AppLanguageKeys.ToolsUsefulSitesToggleHide)
        : L(AppLanguageKeys.ToolsUsefulSitesToggleShow);

    [ObservableProperty]
    private string emptyDirectoryRootPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    [ObservableProperty]
    private string emptyDirectoryStatusMessage = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.ToolsStatusEmptyDirectoryInitial);

    [ObservableProperty]
    private string shortcutHotkeyStatusMessage = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.ToolsStatusShortcutHotkeyInitial);

    [ObservableProperty]
    private bool isShortcutHotkeyScanProgressVisible;

    [ObservableProperty]
    private int shortcutHotkeyScanProgressValue;

    [ObservableProperty]
    private int shortcutHotkeyScanProgressMaximum = 1;

    [ObservableProperty]
    private int shortcutHotkeyScannedShortcutCount;

    [ObservableProperty]
    private string shortcutHotkeyScanProgressPath = string.Empty;

    [ObservableProperty]
    private bool isEmptyDirectoryScanProgressVisible;

    [ObservableProperty]
    private int emptyDirectoryScanProgressValue;

    [ObservableProperty]
    private int emptyDirectoryScanProgressMaximum = 1;

    [ObservableProperty]
    private string emptyDirectoryScanProgressPath = string.Empty;

    [ObservableProperty]
    private int currentMouseSensitivityLevel = 10;

    [ObservableProperty]
    private int selectedMouseSensitivityLevel = 10;

    [ObservableProperty]
    private string mouseSensitivityStatusMessage = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.ToolsStatusMouseSensitivityInitial);

    [ObservableProperty]
    private string displayRefreshStatusMessage = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.ToolsStatusDisplayRefreshInitial);

    [ObservableProperty]
    private string hardwareCheckSystemSummary = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.ToolsStatusHardwareSystemInitial);

    [ObservableProperty]
    private string hardwareCheckHealthSummary = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.ToolsStatusHardwareHealthInitial);

    [ObservableProperty]
    private string hardwareCheckOperatingSystemSummary = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.ToolsStatusHardwareOperatingSystemInitial);

    [ObservableProperty]
    private string hardwareCheckProcessorSummary = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.ToolsStatusHardwareProcessorInitial);

    [ObservableProperty]
    private string hardwareCheckMemorySummary = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.ToolsStatusHardwareMemoryInitial);

    [ObservableProperty]
    private string hardwareCheckMotherboardSummary = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.ToolsStatusHardwareMotherboardInitial);

    [ObservableProperty]
    private string hardwareCheckBiosSummary = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.ToolsStatusHardwareBiosInitial);

    [ObservableProperty]
    private string hardwareCheckStatusMessage = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.ToolsStatusHardwareCheckInitial);

    [ObservableProperty]
    private string driverUpdateStatusMessage = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.ToolsStatusDriverUpdateInitial);

    [ObservableProperty]
    private bool isToolBusy;

    [ObservableProperty]
    private string darkModeToolStatusMessage = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.ToolsStatusDarkModeInitial);

    [ObservableProperty]
    private string searchReplacementStatusMessage = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.ToolsStatusSearchReplacementInitial);

    [ObservableProperty]
    private string searchReindexStatusMessage = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.ToolsStatusSearchReindexInitial);

    [ObservableProperty]
    private string telemetryToolStatusMessage = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.ToolsStatusTelemetryInitial);

    [ObservableProperty]
    private string pinWindowToolStatusMessage = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.ToolsStatusPinWindowInitial);

    [ObservableProperty]
    private string oneDriveToolStatusMessage = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.ToolsStatusOneDriveInitial);

    [ObservableProperty]
    private string edgeToolStatusMessage = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.ToolsStatusEdgeInitial);

    [ObservableProperty]
    private string fnCtrlSwapStatusMessage = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.ToolsStatusFnCtrlSwapInitial);

    [ObservableProperty]
    private bool isFnCtrlSwapSupported;

    [ObservableProperty]
    private bool areUsefulSitesVisible;

    [ObservableProperty]
    private string usefulSitesStatusMessage = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.ToolsStatusUsefulSitesInitial);

    [ObservableProperty]
    private string windows11EeaInstallStatusMessage = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.ToolsStatusWindows11EeaInitial);

    private void InitializeToolsState()
    {
        if (MouseSensitivityLevels.Count == 0)
        {
            foreach (var level in mouseSensitivityService.GetSupportedLevels().OrderBy(static level => level))
            {
                MouseSensitivityLevels.Add(level);
            }
        }

        if (UsefulSites.Count == 0)
        {
            foreach (var site in DefaultUsefulSites)
            {
                UsefulSites.Add(site);
            }
        }

        RefreshMouseSensitivityStatusCore(addLogEntry: false);
        OnPropertyChanged(nameof(HasDisplayRefreshRecommendations));
        OnPropertyChanged(nameof(MouseSensitivitySummary));
        OnPropertyChanged(nameof(MouseSensitivitySelectedLevelLabel));
        OnPropertyChanged(nameof(MouseSensitivitySelectionGuidance));
        OnPropertyChanged(nameof(DisplayRefreshSummary));
        OnPropertyChanged(nameof(HardwareGraphicsSummary));
        OnPropertyChanged(nameof(HardwareStorageSummary));
        OnPropertyChanged(nameof(HardwarePartitionSummary));
        OnPropertyChanged(nameof(HardwareSensorSummary));
        OnPropertyChanged(nameof(HardwarePciSummary));
        OnPropertyChanged(nameof(HardwareRaidSummary));
        OnPropertyChanged(nameof(HasSelectedDriverUpdates));
        OnPropertyChanged(nameof(DriverHardwareSummary));
        OnPropertyChanged(nameof(DriverUpdateSelectionSummary));
        OnPropertyChanged(nameof(HasSelectedEmptyDirectories));
        OnPropertyChanged(nameof(EmptyDirectorySelectionSummary));
        OnPropertyChanged(nameof(ShortcutHotkeyScanProgressSummary));
        OnPropertyChanged(nameof(EmptyDirectoryScanProgressSummary));
        OnPropertyChanged(nameof(UsefulSitesToggleText));
        RefreshOneDriveStatusCore(addLogEntry: false);
        RefreshEdgeStatusCore(addLogEntry: false);
        RefreshFnCtrlSwapStatusCore(addLogEntry: false);
        RefreshSearchReplacementStatusCore(addLogEntry: false);
        RefreshSearchReindexStatusCore(addLogEntry: false);
        RefreshTelemetryStatusCore(addLogEntry: false);
        RefreshPinWindowStatusCore(addLogEntry: false);
    }

    partial void OnEmptyDirectoryRootPathChanged(string value)
    {
        RefreshToolCommandStates();
    }

    partial void OnCurrentMouseSensitivityLevelChanged(int value)
    {
        OnPropertyChanged(nameof(MouseSensitivitySummary));
    }

    partial void OnSelectedMouseSensitivityLevelChanged(int value)
    {
        OnPropertyChanged(nameof(MouseSensitivitySummary));
        OnPropertyChanged(nameof(MouseSensitivitySelectedLevelLabel));
        OnPropertyChanged(nameof(MouseSensitivitySelectionGuidance));
        RefreshToolCommandStates();
    }

    partial void OnIsToolBusyChanged(bool value)
    {
        RefreshToolCommandStates();
    }

    partial void OnAreUsefulSitesVisibleChanged(bool value)
    {
        OnPropertyChanged(nameof(UsefulSitesToggleText));
    }

    partial void OnIsFnCtrlSwapSupportedChanged(bool value)
    {
        RefreshToolCommandStates();
    }

    private void DriverUpdateItem_OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DriverUpdateItem.IsSelected))
        {
            OnPropertyChanged(nameof(HasSelectedDriverUpdates));
            OnPropertyChanged(nameof(DriverUpdateSelectionSummary));
            RefreshToolCommandStates();
        }
    }

    [RelayCommand(CanExecute = nameof(CanRefreshOneDriveStatus))]
    private void RefreshOneDriveStatus()
    {
        RefreshOneDriveStatusCore(addLogEntry: true);
    }

    private bool CanRefreshOneDriveStatus => !IsToolBusy;

    [RelayCommand(CanExecute = nameof(CanRefreshEdgeStatus))]
    private void RefreshEdgeStatus()
    {
        RefreshEdgeStatusCore(addLogEntry: true);
    }

    private bool CanRefreshEdgeStatus => !IsToolBusy;

    private bool CanRefreshFnCtrlSwapStatus => !IsToolBusy;

    [RelayCommand(CanExecute = nameof(CanRefreshFnCtrlSwapStatus))]
    private void RefreshFnCtrlSwapStatus()
    {
        RefreshFnCtrlSwapStatusCore(addLogEntry: true);
    }

    private bool CanRefreshMouseSensitivityStatus => !IsToolBusy;

    [RelayCommand(CanExecute = nameof(CanRefreshMouseSensitivityStatus))]
    private void RefreshMouseSensitivityStatus()
    {
        RefreshMouseSensitivityStatusCore(addLogEntry: true);
    }

    private void RefreshMouseSensitivityStatusCore(bool addLogEntry)
    {
        try
        {
            var status = mouseSensitivityService.GetStatus();
            CurrentMouseSensitivityLevel = status.CurrentLevel;
            SelectedMouseSensitivityLevel = status.CurrentLevel;
            MouseSensitivityStatusMessage = status.Message;
            if (addLogEntry)
            {
                AddToolLog(status.Message);
            }
        }
        catch (Exception ex)
        {
            MouseSensitivityStatusMessage = F(AppLanguageKeys.ToolsErrorReadMouseSensitivityFormat, ex.Message);
            if (addLogEntry)
            {
                AddToolLog(MouseSensitivityStatusMessage);
            }
        }
    }

    private void RefreshOneDriveStatusCore(bool addLogEntry)
    {
        try
        {
            var status = oneDriveRemovalService.GetStatus();
            OneDriveToolStatusMessage = status.Message;
            if (addLogEntry)
            {
                AddToolLog(status.Message);
            }
        }
        catch (Exception ex)
        {
            OneDriveToolStatusMessage = F(AppLanguageKeys.ToolsErrorCheckOneDriveFormat, ex.Message);
            if (addLogEntry)
            {
                AddToolLog(OneDriveToolStatusMessage);
            }
        }
    }

    private void RefreshEdgeStatusCore(bool addLogEntry)
    {
        try
        {
            var status = edgeRemovalService.GetStatus();
            EdgeToolStatusMessage = status.Message;
            if (addLogEntry)
            {
                AddToolLog(status.Message);
            }
        }
        catch (Exception ex)
        {
            EdgeToolStatusMessage = F(AppLanguageKeys.ToolsErrorCheckEdgeFormat, ex.Message);
            if (addLogEntry)
            {
                AddToolLog(EdgeToolStatusMessage);
            }
        }
    }

    private void RefreshSearchReplacementStatusCore(bool addLogEntry)
    {
        try
        {
            var status = windowsSearchReplacementService.GetStatus();
            SearchReplacementStatusMessage = status.Message;
            if (addLogEntry)
            {
                AddToolLog(status.Message);
            }
        }
        catch (Exception ex)
        {
            SearchReplacementStatusMessage = F(AppLanguageKeys.ToolsErrorCheckSearchReplacementFormat, ex.Message);
            if (addLogEntry)
            {
                AddToolLog(SearchReplacementStatusMessage);
            }
        }
    }

    private void RefreshFnCtrlSwapStatusCore(bool addLogEntry)
    {
        try
        {
            var status = fnCtrlSwapService.GetStatus();
            IsFnCtrlSwapSupported = status.IsSupported;
            FnCtrlSwapStatusMessage = status.Message;
            if (addLogEntry)
            {
                AddToolLog(status.Message);
            }
        }
        catch (Exception ex)
        {
            IsFnCtrlSwapSupported = false;
            FnCtrlSwapStatusMessage = F(AppLanguageKeys.ToolsErrorCheckFnCtrlSwapFormat, ex.Message);
            if (addLogEntry)
            {
                AddToolLog(FnCtrlSwapStatusMessage);
            }
        }
    }

    private void Windows11EeaMediaService_OnStatusChanged(object? sender, string message)
    {
        void ApplyStatus()
        {
            Windows11EeaInstallStatusMessage = message;
            AddToolLog(message);
        }

        if (synchronizationContext is null)
        {
            ApplyStatus();
            return;
        }

        synchronizationContext.Post(
            _ => ApplyStatus(),
            null);
    }

    private void EmptyDirectoryItem_OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(EmptyDirectoryItem.IsSelected))
        {
            OnPropertyChanged(nameof(HasSelectedEmptyDirectories));
            OnPropertyChanged(nameof(EmptyDirectorySelectionSummary));
            RefreshToolCommandStates();
        }
    }

    [RelayCommand]
    private void BrowseEmptyDirectoryRoot()
    {
        var selectedPath = folderPickerService.PickFolder(EmptyDirectoryRootPath, L(AppLanguageKeys.ToolsFolderPickerSelectEmptyDirectoryRoot));
        if (string.IsNullOrWhiteSpace(selectedPath))
        {
            EmptyDirectoryStatusMessage = L(AppLanguageKeys.ToolsStatusFolderSelectionCanceled);
            AddToolLog(EmptyDirectoryStatusMessage);
            return;
        }

        EmptyDirectoryRootPath = selectedPath;
        EmptyDirectoryStatusMessage = F(AppLanguageKeys.ToolsStatusEmptyDirectoryRootSetFormat, selectedPath);
        AddToolLog(EmptyDirectoryStatusMessage);
    }

    private bool CanShowAssignedShortcutHotkeys => !IsToolBusy;

    [RelayCommand(CanExecute = nameof(CanShowAssignedShortcutHotkeys))]
    private async Task ShowAssignedShortcutHotkeysAsync()
    {
        IsToolBusy = true;
        ShortcutHotkeyStatusMessage = L(AppLanguageKeys.ToolsStatusShortcutScanRunning);
        StartShortcutHotkeyScanProgress();
        AddToolLog(ShortcutHotkeyStatusMessage);

        try
        {
            var progress = new Progress<ShortcutHotkeyScanProgress>(UpdateShortcutHotkeyScanProgress);
            var result = await shortcutHotkeyInventoryService.ScanAsync(progress).ConfigureAwait(true);
            PersistShortcutHotkeyScanMaxFolderCount(ShortcutHotkeyScanProgressMaximum);
            shortcutHotkeyDialogService.Show(result);

            var warningSuffix = result.Warnings.Count == 0
                ? string.Empty
                : F(AppLanguageKeys.ToolsStatusShortcutScanWarningsSuffixFormat, result.Warnings.Count, PluralSuffix(result.Warnings.Count));

            var detectedCount = result.Shortcuts.Count(static shortcut => !shortcut.IsReferenceShortcut);
            var referenceCount = result.Shortcuts.Count(static shortcut => shortcut.IsReferenceShortcut);
            ShortcutHotkeyStatusMessage = detectedCount == 0 && referenceCount == 0
                ? F(AppLanguageKeys.ToolsStatusShortcutScanNoShortcutsFormat, result.ScannedShortcutCount, PluralSuffix(result.ScannedShortcutCount), warningSuffix)
                : F(
                    AppLanguageKeys.ToolsStatusShortcutScanOpenedViewerFormat,
                    detectedCount,
                    PluralSuffix(detectedCount),
                    referenceCount,
                    referenceCount == 1 ? "y" : "ies",
                    warningSuffix);
            AddToolLog(ShortcutHotkeyStatusMessage);

            foreach (var warning in result.Warnings.Take(10))
            {
                AddToolLog(warning);
            }
        }
        catch (Exception ex)
        {
            ShortcutHotkeyStatusMessage = F(AppLanguageKeys.ToolsStatusShortcutScanFailedFormat, ex.Message);
            AddToolLog(ShortcutHotkeyStatusMessage);
        }
        finally
        {
            ResetShortcutHotkeyScanProgress();
            IsToolBusy = false;
        }
    }

    [RelayCommand]
    private void ToggleUsefulSites()
    {
        AreUsefulSitesVisible = !AreUsefulSitesVisible;
        UsefulSitesStatusMessage = AreUsefulSitesVisible
            ? F(AppLanguageKeys.ToolsStatusUsefulSitesShowingFormat, UsefulSites.Count, PluralSuffix(UsefulSites.Count))
            : L(AppLanguageKeys.ToolsStatusUsefulSitesHidden);
        AddToolLog(UsefulSitesStatusMessage);
    }

    [RelayCommand]
    private void OpenUsefulSite(UsefulSiteItem? site)
    {
        if (site is null)
        {
            return;
        }

        try
        {
            var launchResult = browserLauncherService.OpenUrl(site.Url);

            UsefulSitesStatusMessage = F(AppLanguageKeys.ToolsStatusUsefulSiteOpenedFormat, site.DisplayName, launchResult.BrowserDisplayName);
            AddToolLog(UsefulSitesStatusMessage);
        }
        catch (Exception ex)
        {
            UsefulSitesStatusMessage = F(AppLanguageKeys.ToolsStatusUsefulSiteOpenFailedFormat, site.DisplayName, ex.Message);
            AddToolLog(UsefulSitesStatusMessage);
        }
    }

    private bool CanPrepareWindows11EeaMedia => !IsToolBusy;

    [RelayCommand(CanExecute = nameof(CanPrepareWindows11EeaMedia))]
    private async Task PrepareWindows11EeaMediaAsync()
    {
        IsToolBusy = true;
        Windows11EeaInstallStatusMessage = L(AppLanguageKeys.ToolsStatusWindows11EeaPreparing);
        AddToolLog(Windows11EeaInstallStatusMessage);

        try
        {
            var result = await windows11EeaMediaService.PrepareAsync().ConfigureAwait(true);
            Windows11EeaInstallStatusMessage = result.Message;
            AddToolLog(result.Message);
        }
        catch (Exception ex)
        {
            Windows11EeaInstallStatusMessage = F(AppLanguageKeys.ToolsStatusWindows11EeaFailedFormat, ex.Message);
            AddToolLog(Windows11EeaInstallStatusMessage);
        }
        finally
        {
            IsToolBusy = false;
        }
    }

    private bool CanApplySystemDarkMode => !IsToolBusy;

    private bool CanRefreshSearchReplacementStatus => !IsToolBusy;

    [RelayCommand(CanExecute = nameof(CanRefreshSearchReplacementStatus))]
    private void RefreshSearchReplacementStatus()
    {
        RefreshSearchReplacementStatusCore(addLogEntry: true);
    }

    private bool CanApplySearchReplacement => !IsToolBusy;

    private bool CanRefreshSearchReindexStatus => !IsToolBusy;

    [RelayCommand(CanExecute = nameof(CanRefreshSearchReindexStatus))]
    private void RefreshSearchReindexStatus()
    {
        RefreshSearchReindexStatusCore(addLogEntry: true);
    }

    [RelayCommand(CanExecute = nameof(CanApplySearchReplacement))]
    private async Task ApplySearchReplacementAsync()
    {
        IsToolBusy = true;
        SearchReplacementStatusMessage = L(AppLanguageKeys.ToolsStatusSearchReplacementApplying);
        AddToolLog(SearchReplacementStatusMessage);

        try
        {
            var result = await windowsSearchReplacementService.ApplyAsync().ConfigureAwait(true);
            SearchReplacementStatusMessage = result.Message;
            AddToolLog(result.Message);
        }
        catch (Exception ex)
        {
            SearchReplacementStatusMessage = F(AppLanguageKeys.ToolsStatusSearchReplacementApplyFailedFormat, ex.Message);
            AddToolLog(SearchReplacementStatusMessage);
        }
        finally
        {
            IsToolBusy = false;
        }
    }

    private bool CanRestoreSearchReplacement => !IsToolBusy;

    [RelayCommand(CanExecute = nameof(CanRestoreSearchReplacement))]
    private async Task RestoreSearchReplacementAsync()
    {
        IsToolBusy = true;
        SearchReplacementStatusMessage = L(AppLanguageKeys.ToolsStatusSearchReplacementRestoring);
        AddToolLog(SearchReplacementStatusMessage);

        try
        {
            var result = await windowsSearchReplacementService.RestoreAsync().ConfigureAwait(true);
            SearchReplacementStatusMessage = result.Message;
            AddToolLog(result.Message);
        }
        catch (Exception ex)
        {
            SearchReplacementStatusMessage = F(AppLanguageKeys.ToolsStatusSearchReplacementRestoreFailedFormat, ex.Message);
            AddToolLog(SearchReplacementStatusMessage);
        }
        finally
        {
            IsToolBusy = false;
        }
    }

    private bool CanReindexWindowsSearch => !IsToolBusy;

    private bool CanRefreshTelemetryStatus => !IsToolBusy;

    [RelayCommand(CanExecute = nameof(CanRefreshTelemetryStatus))]
    private void RefreshTelemetryStatus()
    {
        RefreshTelemetryStatusCore(addLogEntry: true);
    }

    private bool CanApplyTelemetryReduction => !IsToolBusy;

    private bool CanToggleWindowPin => !IsToolBusy;

    [RelayCommand(CanExecute = nameof(CanToggleWindowPin))]
    private void ToggleWindowPin()
    {
        ToggleWindowPinCore(L(AppLanguageKeys.ToolsPinWindowTriggerToolButton));
    }

    [RelayCommand(CanExecute = nameof(CanApplyTelemetryReduction))]
    private async Task ApplyTelemetryReductionAsync()
    {
        IsToolBusy = true;
        TelemetryToolStatusMessage = L(AppLanguageKeys.ToolsStatusTelemetryApplying);
        AddToolLog(TelemetryToolStatusMessage);

        try
        {
            var result = await windowsTelemetryService.ApplyAsync().ConfigureAwait(true);
            TelemetryToolStatusMessage = result.Message;
            AddToolLog(result.Message);
            RefreshTelemetryStatusCore(addLogEntry: false);
        }
        catch (Exception ex)
        {
            TelemetryToolStatusMessage = F(AppLanguageKeys.ToolsStatusTelemetryApplyFailedFormat, ex.Message);
            AddToolLog(TelemetryToolStatusMessage);
        }
        finally
        {
            IsToolBusy = false;
        }
    }

    private bool CanRestoreTelemetryDefaults => !IsToolBusy;

    [RelayCommand(CanExecute = nameof(CanRestoreTelemetryDefaults))]
    private async Task RestoreTelemetryDefaultsAsync()
    {
        IsToolBusy = true;
        TelemetryToolStatusMessage = L(AppLanguageKeys.ToolsStatusTelemetryRestoring);
        AddToolLog(TelemetryToolStatusMessage);

        try
        {
            var result = await windowsTelemetryService.RestoreAsync().ConfigureAwait(true);
            TelemetryToolStatusMessage = result.Message;
            AddToolLog(result.Message);
            RefreshTelemetryStatusCore(addLogEntry: false);
        }
        catch (Exception ex)
        {
            TelemetryToolStatusMessage = F(AppLanguageKeys.ToolsStatusTelemetryRestoreFailedFormat, ex.Message);
            AddToolLog(TelemetryToolStatusMessage);
        }
        finally
        {
            IsToolBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanReindexWindowsSearch))]
    private async Task ReindexWindowsSearchAsync()
    {
        IsToolBusy = true;
        SearchReindexStatusMessage = L(AppLanguageKeys.ToolsStatusSearchReindexRequesting);
        AddToolLog(SearchReindexStatusMessage);

        try
        {
            var result = await windowsSearchReindexService.ReindexAsync().ConfigureAwait(true);
            SearchReindexStatusMessage = result.Message;
            AddToolLog(result.Message);
        }
        catch (Exception ex)
        {
            SearchReindexStatusMessage = F(AppLanguageKeys.ToolsStatusSearchReindexFailedFormat, ex.Message);
            AddToolLog(SearchReindexStatusMessage);
        }
        finally
        {
            IsToolBusy = false;
        }
    }

    private void RefreshSearchReindexStatusCore(bool addLogEntry)
    {
        try
        {
            var status = windowsSearchReindexService.GetStatus();
            SearchReindexStatusMessage = status.Message;
            if (addLogEntry)
            {
                AddToolLog(status.Message);
            }
        }
        catch (Exception ex)
        {
            SearchReindexStatusMessage = F(AppLanguageKeys.ToolsErrorCheckSearchReindexFormat, ex.Message);
            if (addLogEntry)
            {
                AddToolLog(SearchReindexStatusMessage);
            }
        }
    }

    private void RefreshTelemetryStatusCore(bool addLogEntry)
    {
        try
        {
            var status = windowsTelemetryService.GetStatus();
            TelemetryToolStatusMessage = status.Message;
            if (addLogEntry)
            {
                AddToolLog(status.Message);
            }
        }
        catch (Exception ex)
        {
            TelemetryToolStatusMessage = F(AppLanguageKeys.ToolsErrorCheckTelemetryFormat, ex.Message);
            if (addLogEntry)
            {
                AddToolLog(TelemetryToolStatusMessage);
            }
        }
    }

    private void RefreshPinWindowStatusCore(bool addLogEntry)
    {
        var hotkeyLabel = PinWindowHotkeyLabel;
        PinWindowToolStatusMessage = IsTopMost
            ? F(AppLanguageKeys.ToolsStatusPinWindowPinnedFormat, hotkeyLabel)
            : F(AppLanguageKeys.ToolsStatusPinWindowUnpinnedFormat, hotkeyLabel);

        if (addLogEntry)
        {
            AddToolLog(PinWindowToolStatusMessage);
        }
    }

    private void ToggleWindowPinCore(string triggerSource)
    {
        IsTopMost = !IsTopMost;
        var stateText = IsTopMost
            ? L(AppLanguageKeys.ToolsPinWindowStatePinned)
            : L(AppLanguageKeys.ToolsPinWindowStateUnpinned);
        PinWindowToolStatusMessage = F(AppLanguageKeys.ToolsStatusPinWindowToggledFormat, stateText, triggerSource, PinWindowHotkeyLabel);
        AddToolLog(PinWindowToolStatusMessage);
    }

    private void ToggleWindowPinFromHotkey()
    {
        ToggleWindowPinCore(L(AppLanguageKeys.ToolsPinWindowTriggerHotkey));
    }

    [RelayCommand(CanExecute = nameof(CanApplySystemDarkMode))]
    private void ApplySystemDarkMode()
    {
        IsToolBusy = true;

        try
        {
            var succeeded = themeService.TryApplySystemDarkModePreference(out var message);
            if (succeeded)
            {
                suppressThemeChange = true;
                IsDarkMode = true;
                suppressThemeChange = false;
                themeService.ApplyTheme(true);
                SettingsStatusMessage = L(AppLanguageKeys.ToolsStatusSettingsDarkModeOn);
                ScheduleSettingsAutoSave();
            }

            DarkModeToolStatusMessage = message;
            AddToolLog(message);
        }
        finally
        {
            IsToolBusy = false;
        }
    }

    [RelayCommand]
    private void OpenWindowsColorSettings()
    {
        try
        {
            Process.Start(
                new ProcessStartInfo
                {
                    FileName = "ms-settings:colors",
                    UseShellExecute = true,
                });

            DarkModeToolStatusMessage = L(AppLanguageKeys.ToolsStatusOpenedColorSettings);
            AddToolLog(DarkModeToolStatusMessage);
        }
        catch (Exception ex)
        {
            DarkModeToolStatusMessage = F(AppLanguageKeys.ToolsStatusOpenColorSettingsFailedFormat, ex.Message);
            AddToolLog(DarkModeToolStatusMessage);
        }
    }

    private bool CanOpenEmptyDirectoryRoot => !IsToolBusy && Directory.Exists(EmptyDirectoryRootPath);

    [RelayCommand(CanExecute = nameof(CanOpenEmptyDirectoryRoot))]
    private void OpenEmptyDirectoryRoot()
    {
        try
        {
            Process.Start(
                new ProcessStartInfo
                {
                    FileName = EmptyDirectoryRootPath,
                    UseShellExecute = true,
                });

            EmptyDirectoryStatusMessage = L(AppLanguageKeys.ToolsStatusOpenedScanRoot);
            AddToolLog(EmptyDirectoryStatusMessage);
        }
        catch (Exception ex)
        {
            EmptyDirectoryStatusMessage = F(AppLanguageKeys.ToolsStatusOpenScanRootFailedFormat, ex.Message);
            AddToolLog(EmptyDirectoryStatusMessage);
        }
    }

    private bool CanScanEmptyDirectories => !IsToolBusy && !string.IsNullOrWhiteSpace(EmptyDirectoryRootPath);

    private bool CanApplyMouseSensitivity =>
        !IsToolBusy
        && SelectedMouseSensitivityLevel > 0
        && SelectedMouseSensitivityLevel != CurrentMouseSensitivityLevel;

    [RelayCommand(CanExecute = nameof(CanApplyMouseSensitivity))]
    private async Task ApplyMouseSensitivityAsync()
    {
        IsToolBusy = true;
        MouseSensitivityStatusMessage = F(AppLanguageKeys.ToolsStatusMouseSensitivityApplyingFormat, BuildMouseSensitivityLevelText(SelectedMouseSensitivityLevel));
        AddToolLog(MouseSensitivityStatusMessage);

        try
        {
            var result = await mouseSensitivityService.ApplyAsync(SelectedMouseSensitivityLevel).ConfigureAwait(true);
            RefreshMouseSensitivityStatusCore(addLogEntry: false);
            MouseSensitivityStatusMessage = result.Message;
            AddToolLog(result.Message);
        }
        catch (Exception ex)
        {
            MouseSensitivityStatusMessage = F(AppLanguageKeys.ToolsStatusMouseSensitivityUpdateFailedFormat, ex.Message);
            AddToolLog(MouseSensitivityStatusMessage);
        }
        finally
        {
            IsToolBusy = false;
        }
    }

    [RelayCommand]
    private void OpenMouseSettings()
    {
        try
        {
            Process.Start(
                new ProcessStartInfo
                {
                    FileName = "main.cpl",
                    UseShellExecute = true,
                });

            MouseSensitivityStatusMessage = L(AppLanguageKeys.ToolsStatusOpenedMouseSettings);
            AddToolLog(MouseSensitivityStatusMessage);
        }
        catch (Exception ex)
        {
            MouseSensitivityStatusMessage = F(AppLanguageKeys.ToolsStatusOpenMouseSettingsFailedFormat, ex.Message);
            AddToolLog(MouseSensitivityStatusMessage);
        }
    }

    private bool CanScanDisplayRefreshRecommendations => !IsToolBusy;

    [RelayCommand(CanExecute = nameof(CanScanDisplayRefreshRecommendations))]
    private async Task ScanDisplayRefreshRecommendationsAsync()
    {
        await ScanDisplayRefreshRecommendationsCoreAsync(addLogEntry: true, manageBusyState: true).ConfigureAwait(true);
    }

    private bool CanApplyDisplayRefreshRecommendations => !IsToolBusy && DisplayRefreshRecommendations.Any(item => item.NeedsChange);

    [RelayCommand(CanExecute = nameof(CanApplyDisplayRefreshRecommendations))]
    private async Task ApplyDisplayRefreshRecommendationsAsync()
    {
        IsToolBusy = true;
        DisplayRefreshStatusMessage = L(AppLanguageKeys.ToolsStatusDisplayRefreshApplying);
        AddToolLog(DisplayRefreshStatusMessage);

        try
        {
            var results = await displayRefreshRateService.ApplyRecommendedAsync().ConfigureAwait(true);
            foreach (var result in results)
            {
                AddToolLog($"{result.DisplayName}: {result.Message}");
            }

            await ScanDisplayRefreshRecommendationsCoreAsync(addLogEntry: false, manageBusyState: false).ConfigureAwait(true);

            var changedCount = results.Count(result => result.Succeeded && result.Changed);
            var failedCount = results.Count(result => !result.Succeeded);
            DisplayRefreshStatusMessage = changedCount == 0 && failedCount == 0
                ? L(AppLanguageKeys.ToolsStatusDisplayRefreshAlreadyBest)
                : F(AppLanguageKeys.ToolsStatusDisplayRefreshAppliedSummaryFormat, changedCount, PluralSuffix(changedCount), failedCount);
            AddToolLog(DisplayRefreshStatusMessage);
        }
        catch (Exception ex)
        {
            DisplayRefreshStatusMessage = F(AppLanguageKeys.ToolsStatusDisplayRefreshUpdateFailedFormat, ex.Message);
            AddToolLog(DisplayRefreshStatusMessage);
        }
        finally
        {
            IsToolBusy = false;
        }
    }

    private bool CanScanHardwareCheck => !IsToolBusy;

    [RelayCommand(CanExecute = nameof(CanScanHardwareCheck))]
    private async Task ScanHardwareCheckAsync()
    {
        await ScanHardwareCheckCoreAsync(addLogEntry: true, manageBusyState: true).ConfigureAwait(true);
    }

    private bool CanCopyHardwareCheckInfo => !IsToolBusy;

    [RelayCommand(CanExecute = nameof(CanCopyHardwareCheckInfo))]
    private void CopyHardwareCheckInfo()
    {
        try
        {
            clipboardTextService.SetText(BuildHardwareCheckClipboardText());
            HardwareCheckStatusMessage = L(AppLanguageKeys.ToolsStatusHardwareCopiedClipboard);
            AddToolLog(HardwareCheckStatusMessage);
        }
        catch (Exception ex)
        {
            HardwareCheckStatusMessage = F(AppLanguageKeys.ToolsStatusHardwareCopyFailedFormat, ex.Message);
            AddToolLog(HardwareCheckStatusMessage);
        }
    }

    private bool CanScanDriverUpdates => !IsToolBusy;

    [RelayCommand(CanExecute = nameof(CanScanDriverUpdates))]
    private async Task ScanDriverUpdatesAsync()
    {
        await ScanDriverUpdatesCoreAsync(addLogEntry: true, manageBusyState: true).ConfigureAwait(true);
    }

    private bool CanSelectRecommendedDriverUpdates => !IsToolBusy && DriverUpdateCandidates.Any(item => item.IsSelected != !item.IsOptional);

    [RelayCommand(CanExecute = nameof(CanSelectRecommendedDriverUpdates))]
    private void SelectRecommendedDriverUpdates()
    {
        foreach (var item in DriverUpdateCandidates)
        {
            item.IsSelected = !item.IsOptional;
        }

        DriverUpdateStatusMessage = L(AppLanguageKeys.ToolsStatusDriverSelectRecommended);
        AddToolLog(DriverUpdateStatusMessage);
    }

    private bool CanSelectAllDriverUpdates => !IsToolBusy && DriverUpdateCandidates.Any(item => !item.IsSelected);

    [RelayCommand(CanExecute = nameof(CanSelectAllDriverUpdates))]
    private void SelectAllDriverUpdates()
    {
        foreach (var item in DriverUpdateCandidates)
        {
            item.IsSelected = true;
        }

        DriverUpdateStatusMessage = L(AppLanguageKeys.ToolsStatusDriverSelectAll);
        AddToolLog(DriverUpdateStatusMessage);
    }

    private bool CanClearDriverUpdateSelection => !IsToolBusy && DriverUpdateCandidates.Any(item => item.IsSelected);

    [RelayCommand(CanExecute = nameof(CanClearDriverUpdateSelection))]
    private void ClearDriverUpdateSelection()
    {
        foreach (var item in DriverUpdateCandidates.Where(item => item.IsSelected))
        {
            item.IsSelected = false;
        }

        DriverUpdateStatusMessage = L(AppLanguageKeys.ToolsStatusDriverSelectionCleared);
        AddToolLog(DriverUpdateStatusMessage);
    }

    private bool CanInstallSelectedDriverUpdates => !IsToolBusy && DriverUpdateCandidates.Any(item => item.IsSelected);

    [RelayCommand(CanExecute = nameof(CanInstallSelectedDriverUpdates))]
    private async Task InstallSelectedDriverUpdatesAsync()
    {
        var selectedItems = DriverUpdateCandidates
            .Where(item => item.IsSelected)
            .ToArray();
        if (selectedItems.Length == 0)
        {
            DriverUpdateStatusMessage = L(AppLanguageKeys.ToolsStatusDriverSelectAtLeastOne);
            AddToolLog(DriverUpdateStatusMessage);
            return;
        }

        IsToolBusy = true;
        DriverUpdateStatusMessage = F(AppLanguageKeys.ToolsStatusDriverInstallingFormat, selectedItems.Length, PluralSuffix(selectedItems.Length));
        AddToolLog(DriverUpdateStatusMessage);

        try
        {
            var results = await driverUpdateService
                .InstallAsync(selectedItems.Select(static item => item.UpdateId))
                .ConfigureAwait(true);

            foreach (var result in results)
            {
                AddToolLog($"{result.Title}: {result.Message}");
            }

            await ScanDriverUpdatesCoreAsync(addLogEntry: false, manageBusyState: false).ConfigureAwait(true);

            var installedCount = results.Count(result => result.Succeeded && result.Changed);
            var manualFlowCount = results.Count(result => result.RequiresUserInput);
            var failedCount = results.Count(result => !result.Succeeded && !result.RequiresUserInput);
            var restartCount = results.Count(result => result.RequiresRestart);
            var statusParts = new List<string>();
            if (installedCount > 0)
            {
                statusParts.Add(F(AppLanguageKeys.ToolsStatusDriverResultInstalledFormat, installedCount));
            }

            if (manualFlowCount > 0)
            {
                statusParts.Add(F(AppLanguageKeys.ToolsStatusDriverResultManualFlowFormat, manualFlowCount));
            }

            if (failedCount > 0)
            {
                statusParts.Add(F(AppLanguageKeys.ToolsStatusDriverResultFailedFormat, failedCount));
            }

            if (statusParts.Count == 0)
            {
                statusParts.Add(L(AppLanguageKeys.ToolsStatusDriverResultNoChanges));
            }

            DriverUpdateStatusMessage =
                F(AppLanguageKeys.ToolsStatusDriverSummaryFormat, string.Join(", ", statusParts), DriverUpdateCandidates.Count, PluralSuffix(DriverUpdateCandidates.Count)) +
                (manualFlowCount > 0 ? L(AppLanguageKeys.ToolsStatusDriverManualFlowHint) : string.Empty) +
                (restartCount > 0 ? F(AppLanguageKeys.ToolsStatusDriverRestartHintFormat, restartCount, PluralSuffix(restartCount)) : string.Empty);
            AddToolLog(DriverUpdateStatusMessage);

            if (manualFlowCount > 0)
            {
                var handoffMessage = OpenWindowsUpdateOptionalUpdatesAndStartScan();
                AddToolLog(handoffMessage);
                DriverUpdateStatusMessage = $"{DriverUpdateStatusMessage} {handoffMessage}";
            }
        }
        catch (Exception ex)
        {
            DriverUpdateStatusMessage = F(AppLanguageKeys.ToolsStatusDriverInstallFailedFormat, ex.Message);
            AddToolLog(DriverUpdateStatusMessage);
        }
        finally
        {
            IsToolBusy = false;
        }
    }

    [RelayCommand]
    private void OpenWindowsUpdateOptionalUpdates()
    {
        try
        {
            DriverUpdateStatusMessage = OpenWindowsUpdateOptionalUpdatesAndStartScan();
            AddToolLog(DriverUpdateStatusMessage);
        }
        catch (Exception ex)
        {
            DriverUpdateStatusMessage = F(AppLanguageKeys.ToolsStatusDriverOpenOptionalUpdatesFailedFormat, ex.Message);
            AddToolLog(DriverUpdateStatusMessage);
        }
    }

    private string OpenWindowsUpdateOptionalUpdatesAndStartScan()
    {
        var openedOptionalPage = false;

        try
        {
            Process.Start(
                new ProcessStartInfo
                {
                    FileName = WindowsUpdateOptionalUpdatesSettingsUri,
                    UseShellExecute = true,
                });
            openedOptionalPage = true;
        }
        catch
        {
            Process.Start(
                new ProcessStartInfo
                {
                    FileName = WindowsUpdateSettingsUri,
                    UseShellExecute = true,
                });
        }

        var triggeredScan = false;
        foreach (var argument in new[] { "StartScan", "StartInteractiveScan" })
        {
            try
            {
                Process.Start(
                    new ProcessStartInfo
                    {
                        FileName = UsoClientExecutable,
                        Arguments = argument,
                        UseShellExecute = true,
                        CreateNoWindow = true,
                    });
                triggeredScan = true;
                break;
            }
            catch
            {
            }
        }

        return openedOptionalPage
            ? triggeredScan
                ? L(AppLanguageKeys.ToolsStatusDriverOpenOptionalUpdatesAndScan)
                : L(AppLanguageKeys.ToolsStatusDriverOpenOptionalUpdatesNoScan)
            : triggeredScan
                ? L(AppLanguageKeys.ToolsStatusDriverOpenUpdatesAndScan)
                : L(AppLanguageKeys.ToolsStatusDriverOpenUpdatesNoScan);
    }

    private bool CanRemoveOneDrive => !IsToolBusy;

    [RelayCommand(CanExecute = nameof(CanRemoveOneDrive))]
    private async Task RemoveOneDriveAsync()
    {
        IsToolBusy = true;
        OneDriveToolStatusMessage = L(AppLanguageKeys.ToolsStatusOneDriveRemoving);
        AddToolLog(OneDriveToolStatusMessage);

        try
        {
            var result = await oneDriveRemovalService.RemoveAsync().ConfigureAwait(true);
            OneDriveToolStatusMessage = result.Message;
            AddToolLog(result.Message);
        }
        catch (Exception ex)
        {
            OneDriveToolStatusMessage = F(AppLanguageKeys.ToolsStatusOneDriveRemoveFailedFormat, ex.Message);
            AddToolLog(OneDriveToolStatusMessage);
        }
        finally
        {
            IsToolBusy = false;
        }
    }

    private bool CanRemoveEdge => !IsToolBusy;

    [RelayCommand(CanExecute = nameof(CanRemoveEdge))]
    private async Task RemoveEdgeAsync()
    {
        IsToolBusy = true;
        EdgeToolStatusMessage = L(AppLanguageKeys.ToolsStatusEdgeRemoving);
        AddToolLog(EdgeToolStatusMessage);

        try
        {
            var result = await edgeRemovalService.RemoveAsync().ConfigureAwait(true);
            EdgeToolStatusMessage = result.Message;
            AddToolLog(result.Message);
        }
        catch (Exception ex)
        {
            EdgeToolStatusMessage = F(AppLanguageKeys.ToolsStatusEdgeRemoveFailedFormat, ex.Message);
            AddToolLog(EdgeToolStatusMessage);
        }
        finally
        {
            IsToolBusy = false;
        }
    }

    private bool CanToggleFnCtrlSwap => !IsToolBusy && IsFnCtrlSwapSupported;

    [RelayCommand(CanExecute = nameof(CanToggleFnCtrlSwap))]
    private async Task ToggleFnCtrlSwapAsync()
    {
        IsToolBusy = true;
        FnCtrlSwapStatusMessage = L(AppLanguageKeys.ToolsStatusFnCtrlSwapApplying);
        AddToolLog(FnCtrlSwapStatusMessage);

        try
        {
            var result = await fnCtrlSwapService.ToggleAsync().ConfigureAwait(true);
            FnCtrlSwapStatusMessage = result.Message;
            AddToolLog(result.Message);
            RefreshFnCtrlSwapStatusCore(addLogEntry: false);
        }
        catch (Exception ex)
        {
            FnCtrlSwapStatusMessage = F(AppLanguageKeys.ToolsStatusFnCtrlSwapFailedFormat, ex.Message);
            AddToolLog(FnCtrlSwapStatusMessage);
        }
        finally
        {
            IsToolBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanScanEmptyDirectories))]
    private async Task ScanEmptyDirectoriesAsync()
    {
        await ScanEmptyDirectoriesCoreAsync(addLogEntry: true, manageBusyState: true).ConfigureAwait(true);
    }

    private bool CanSelectAllEmptyDirectories => !IsToolBusy && EmptyDirectoryCandidates.Any(item => !item.IsSelected);

    [RelayCommand(CanExecute = nameof(CanSelectAllEmptyDirectories))]
    private void SelectAllEmptyDirectories()
    {
        foreach (var item in EmptyDirectoryCandidates)
        {
            item.IsSelected = true;
        }

        EmptyDirectoryStatusMessage = L(AppLanguageKeys.ToolsStatusEmptyDirectorySelectAll);
        AddToolLog(EmptyDirectoryStatusMessage);
    }

    private bool CanClearEmptyDirectorySelection => !IsToolBusy && EmptyDirectoryCandidates.Any(item => item.IsSelected);

    [RelayCommand(CanExecute = nameof(CanClearEmptyDirectorySelection))]
    private void ClearEmptyDirectorySelection()
    {
        foreach (var item in EmptyDirectoryCandidates.Where(item => item.IsSelected))
        {
            item.IsSelected = false;
        }

        EmptyDirectoryStatusMessage = L(AppLanguageKeys.ToolsStatusEmptyDirectorySelectionCleared);
        AddToolLog(EmptyDirectoryStatusMessage);
    }

    private bool CanDeleteSelectedEmptyDirectories => !IsToolBusy && EmptyDirectoryCandidates.Any(item => item.IsSelected);

    [RelayCommand(CanExecute = nameof(CanDeleteSelectedEmptyDirectories))]
    private async Task DeleteSelectedEmptyDirectoriesAsync()
    {
        var selectedPaths = EmptyDirectoryCandidates
            .Where(item => item.IsSelected)
            .Select(item => item.FullPath)
            .ToArray();
        if (selectedPaths.Length == 0)
        {
            EmptyDirectoryStatusMessage = L(AppLanguageKeys.ToolsStatusEmptyDirectorySelectAtLeastOne);
            AddToolLog(EmptyDirectoryStatusMessage);
            return;
        }

        IsToolBusy = true;
        EmptyDirectoryStatusMessage = F(AppLanguageKeys.ToolsStatusEmptyDirectoryDeletingFormat, selectedPaths.Length, selectedPaths.Length == 1 ? "y" : "ies");
        AddToolLog(EmptyDirectoryStatusMessage);

        try
        {
            var results = await emptyDirectoryService.DeleteDirectoriesAsync(selectedPaths).ConfigureAwait(true);
            foreach (var result in results)
            {
                AddToolLog($"{result.DirectoryPath}: {result.Message}");
            }

            var deletedCount = results.Count(result => result.Succeeded && result.Deleted);
            var missingCount = results.Count(result => result.Succeeded && !result.Deleted);
            var failedCount = results.Count(result => !result.Succeeded);

            await ScanEmptyDirectoriesCoreAsync(addLogEntry: false, manageBusyState: false).ConfigureAwait(true);
            EmptyDirectoryStatusMessage = F(
                AppLanguageKeys.ToolsStatusEmptyDirectoryDeleteSummaryFormat,
                deletedCount,
                missingCount,
                failedCount,
                EmptyDirectoryCandidates.Count,
                EmptyDirectoryCandidates.Count == 1 ? "y" : "ies");
            AddToolLog(EmptyDirectoryStatusMessage);
        }
        catch (Exception ex)
        {
            EmptyDirectoryStatusMessage = F(AppLanguageKeys.ToolsStatusEmptyDirectoryDeleteFailedFormat, ex.Message);
            AddToolLog(EmptyDirectoryStatusMessage);
        }
        finally
        {
            IsToolBusy = false;
        }
    }

    private async Task ScanEmptyDirectoriesCoreAsync(bool addLogEntry, bool manageBusyState)
    {
        var rootPath = EmptyDirectoryRootPath?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            EmptyDirectoryStatusMessage = L(AppLanguageKeys.ToolsStatusEmptyDirectoryChooseRootFirst);
            if (addLogEntry)
            {
                AddToolLog(EmptyDirectoryStatusMessage);
            }

            return;
        }

        if (!Directory.Exists(rootPath))
        {
            EmptyDirectoryStatusMessage = F(AppLanguageKeys.ToolsStatusEmptyDirectoryRootMissingFormat, rootPath);
            if (addLogEntry)
            {
                AddToolLog(EmptyDirectoryStatusMessage);
            }

            return;
        }

        var fullRootPath = Path.GetFullPath(rootPath);
        EmptyDirectoryRootPath = fullRootPath;
        if (manageBusyState)
        {
            IsToolBusy = true;
        }

        EmptyDirectoryStatusMessage = F(AppLanguageKeys.ToolsStatusEmptyDirectoryScanningFormat, fullRootPath);
        StartEmptyDirectoryScanProgress(fullRootPath);
        if (addLogEntry)
        {
            AddToolLog(EmptyDirectoryStatusMessage);
        }

        try
        {
            var progress = new Progress<EmptyDirectoryScanProgress>(UpdateEmptyDirectoryScanProgress);
            var scanResult = await emptyDirectoryService.FindEmptyDirectoriesAsync(fullRootPath, progress).ConfigureAwait(true);
            PersistEmptyDirectoryScanMaxFolderCount(fullRootPath, EmptyDirectoryScanProgressMaximum);
            ReplaceEmptyDirectoryCandidates(fullRootPath, scanResult.Candidates);

            var warningSuffix = scanResult.Warnings.Count == 0
                ? string.Empty
                : F(AppLanguageKeys.ToolsStatusEmptyDirectoryWarningsSuffixFormat, scanResult.Warnings.Count, PluralSuffix(scanResult.Warnings.Count));
            EmptyDirectoryStatusMessage = scanResult.Candidates.Count == 0
                ? F(AppLanguageKeys.ToolsStatusEmptyDirectoryNoneFoundFormat, warningSuffix)
                : F(AppLanguageKeys.ToolsStatusEmptyDirectoryFoundFormat, scanResult.Candidates.Count, scanResult.Candidates.Count == 1 ? "y" : "ies", warningSuffix);

            if (addLogEntry)
            {
                AddToolLog(EmptyDirectoryStatusMessage);
                foreach (var warning in scanResult.Warnings.Take(10))
                {
                    AddToolLog(warning);
                }
            }
        }
        catch (Exception ex)
        {
            EmptyDirectoryStatusMessage = F(AppLanguageKeys.ToolsStatusEmptyDirectoryScanFailedFormat, ex.Message);
            if (addLogEntry)
            {
                AddToolLog(EmptyDirectoryStatusMessage);
            }
        }
        finally
        {
            ResetEmptyDirectoryScanProgress();
            if (manageBusyState)
            {
                IsToolBusy = false;
            }
        }
    }

    private async Task ScanDriverUpdatesCoreAsync(bool addLogEntry, bool manageBusyState)
    {
        if (manageBusyState)
        {
            IsToolBusy = true;
        }

        DriverUpdateStatusMessage = L(AppLanguageKeys.ToolsStatusDriverScanStarting);
        if (addLogEntry)
        {
            AddToolLog(DriverUpdateStatusMessage);
        }

        try
        {
            var scanResult = await driverUpdateService.ScanAsync().ConfigureAwait(true);
            ReplaceDriverHardwareInventory(scanResult.Hardware);
            ReplaceDriverUpdateCandidates(scanResult.Updates);

            var recommendedCount = scanResult.Updates.Count(update => !update.IsOptional);
            var optionalCount = scanResult.Updates.Count - recommendedCount;
            var interactiveCount = scanResult.Updates.Count(update => update.RequiresUserInput);
            var warningSuffix = scanResult.Warnings.Count == 0
                ? string.Empty
                : F(AppLanguageKeys.ToolsStatusDriverScanWarningsSuffixFormat, scanResult.Warnings.Count);
            var interactiveSuffix = interactiveCount == 0
                ? string.Empty
                : F(AppLanguageKeys.ToolsStatusDriverScanInteractiveSuffixFormat, interactiveCount);

            DriverUpdateStatusMessage = scanResult.Updates.Count == 0
                ? F(AppLanguageKeys.ToolsStatusDriverScanNoneFormat, scanResult.Hardware.Count, PluralSuffix(scanResult.Hardware.Count), warningSuffix)
                : F(
                    AppLanguageKeys.ToolsStatusDriverScanFoundFormat,
                    scanResult.Hardware.Count,
                    PluralSuffix(scanResult.Hardware.Count),
                    recommendedCount,
                    optionalCount,
                    PluralSuffix(scanResult.Updates.Count),
                    interactiveSuffix,
                    warningSuffix);

            if (addLogEntry)
            {
                AddToolLog(DriverUpdateStatusMessage);
                foreach (var warning in scanResult.Warnings.Take(10))
                {
                    AddToolLog(warning);
                }
            }
        }
        catch (Exception ex)
        {
            DriverUpdateStatusMessage = F(AppLanguageKeys.ToolsStatusDriverScanFailedFormat, ex.Message);
            if (addLogEntry)
            {
                AddToolLog(DriverUpdateStatusMessage);
            }
        }
        finally
        {
            if (manageBusyState)
            {
                IsToolBusy = false;
            }
        }
    }

    private async Task ScanHardwareCheckCoreAsync(bool addLogEntry, bool manageBusyState)
    {
        if (manageBusyState)
        {
            IsToolBusy = true;
        }

        HardwareCheckStatusMessage = L(AppLanguageKeys.ToolsStatusHardwareScanStarting);
        if (addLogEntry)
        {
            AddToolLog(HardwareCheckStatusMessage);
        }

        try
        {
            var report = await hardwareInventoryService.GetReportAsync().ConfigureAwait(true);
            ApplyHardwareInventoryReport(report);

            var warningSuffix = report.Warnings.Count == 0
                ? string.Empty
                : F(AppLanguageKeys.ToolsStatusHardwareScanWarningsSuffixFormat, report.Warnings.Count);
            HardwareCheckStatusMessage =
                F(
                    AppLanguageKeys.ToolsStatusHardwareScanCompleteFormat,
                    report.HealthSummary,
                    report.GraphicsAdapters.Count,
                    PluralSuffix(report.GraphicsAdapters.Count),
                    report.StorageDrives.Count,
                    PluralSuffix(report.StorageDrives.Count),
                    report.StoragePartitions.Count,
                    PluralSuffix(report.StoragePartitions.Count),
                    report.PciDevices.Count,
                    PluralSuffix(report.PciDevices.Count),
                    report.Sensors.Count,
                    PluralSuffix(report.Sensors.Count),
                    report.RaidDetails.Count,
                    PluralSuffix(report.RaidDetails.Count),
                    warningSuffix);

            if (addLogEntry)
            {
                AddToolLog(HardwareCheckStatusMessage);
                foreach (var warning in report.Warnings.Take(10))
                {
                    AddToolLog(warning);
                }
            }
        }
        catch (Exception ex)
        {
            HardwareCheckStatusMessage = F(AppLanguageKeys.ToolsStatusHardwareScanFailedFormat, ex.Message);
            if (addLogEntry)
            {
                AddToolLog(HardwareCheckStatusMessage);
            }
        }
        finally
        {
            if (manageBusyState)
            {
                IsToolBusy = false;
            }
        }
    }

    private async Task ScanDisplayRefreshRecommendationsCoreAsync(bool addLogEntry, bool manageBusyState)
    {
        if (manageBusyState)
        {
            IsToolBusy = true;
        }

        DisplayRefreshStatusMessage = L(AppLanguageKeys.ToolsStatusDisplayRefreshScanStarting);
        if (addLogEntry)
        {
            AddToolLog(DisplayRefreshStatusMessage);
        }

        try
        {
            var recommendations = await displayRefreshRateService.GetRecommendationsAsync().ConfigureAwait(true);
            ReplaceDisplayRefreshRecommendations(recommendations);

            var needsChangeCount = recommendations.Count(item => item.NeedsChange);
            DisplayRefreshStatusMessage = recommendations.Count == 0
                ? L(AppLanguageKeys.ToolsStatusDisplayRefreshNoDisplays)
                : needsChangeCount == 0
                    ? F(AppLanguageKeys.ToolsStatusDisplayRefreshCheckedAllBestFormat, recommendations.Count, PluralSuffix(recommendations.Count))
                    : F(AppLanguageKeys.ToolsStatusDisplayRefreshCheckedCanRunFasterFormat, recommendations.Count, PluralSuffix(recommendations.Count), needsChangeCount);

            if (addLogEntry)
            {
                AddToolLog(DisplayRefreshStatusMessage);
            }
        }
        catch (Exception ex)
        {
            DisplayRefreshStatusMessage = F(AppLanguageKeys.ToolsStatusDisplayRefreshScanFailedFormat, ex.Message);
            if (addLogEntry)
            {
                AddToolLog(DisplayRefreshStatusMessage);
            }
        }
        finally
        {
            if (manageBusyState)
            {
                IsToolBusy = false;
            }
        }
    }

    private void ApplyHardwareInventoryReport(HardwareInventoryReport report)
    {
        HardwareCheckSystemSummary = report.SystemSummary;
        HardwareCheckHealthSummary = report.HealthSummary;
        HardwareCheckOperatingSystemSummary = report.OperatingSystemSummary;
        HardwareCheckProcessorSummary = report.ProcessorSummary;
        HardwareCheckMemorySummary = report.MemorySummary;
        HardwareCheckMotherboardSummary = report.MotherboardSummary;
        HardwareCheckBiosSummary = report.BiosSummary;

        ReplaceHardwareGraphicsAdapters(report.GraphicsAdapters);
        ReplaceHardwareStorageDrives(report.StorageDrives);
        ReplaceHardwareStoragePartitions(report.StoragePartitions);
        ReplaceHardwareSensors(report.Sensors);
        ReplaceHardwarePciDevices(report.PciDevices);
        ReplaceHardwareRaidDetails(report.RaidDetails);
    }

    private void ReplaceHardwareGraphicsAdapters(IReadOnlyList<HardwareDisplayAdapterInfo> adapters)
    {
        HardwareGraphicsAdapters.Clear();
        foreach (var adapter in adapters)
        {
            HardwareGraphicsAdapters.Add(adapter);
        }

        OnPropertyChanged(nameof(HardwareGraphicsSummary));
    }

    private void ReplaceHardwareStorageDrives(IReadOnlyList<HardwareStorageDriveInfo> drives)
    {
        HardwareStorageDrives.Clear();
        foreach (var drive in drives)
        {
            HardwareStorageDrives.Add(drive);
        }

        OnPropertyChanged(nameof(HardwareStorageSummary));
    }

    private void ReplaceHardwareStoragePartitions(IReadOnlyList<HardwarePartitionInfo> partitions)
    {
        HardwareStoragePartitions.Clear();
        foreach (var partition in partitions)
        {
            HardwareStoragePartitions.Add(partition);
        }

        OnPropertyChanged(nameof(HasHardwareStoragePartitions));
        OnPropertyChanged(nameof(HardwarePartitionSummary));
    }

    private void ReplaceHardwareSensors(IReadOnlyList<HardwareSensorInfo> sensors)
    {
        HardwareSensors.Clear();
        foreach (var sensor in sensors)
        {
            HardwareSensors.Add(sensor);
        }

        OnPropertyChanged(nameof(HasHardwareSensors));
        OnPropertyChanged(nameof(HardwareSensorSummary));
    }

    private void ReplaceHardwarePciDevices(IReadOnlyList<HardwarePciDeviceInfo> devices)
    {
        HardwarePciDevices.Clear();
        foreach (var device in devices)
        {
            HardwarePciDevices.Add(device);
        }

        OnPropertyChanged(nameof(HasHardwarePciDevices));
        OnPropertyChanged(nameof(HardwarePciSummary));
    }

    private void ReplaceHardwareRaidDetails(IReadOnlyList<HardwareRaidInfo> raidDetails)
    {
        HardwareRaidDetails.Clear();
        foreach (var detail in raidDetails)
        {
            HardwareRaidDetails.Add(detail);
        }

        OnPropertyChanged(nameof(HasHardwareRaidDetails));
        OnPropertyChanged(nameof(HardwareRaidSummary));
    }

    private void ReplaceDisplayRefreshRecommendations(IReadOnlyList<DisplayRefreshRecommendation> recommendations)
    {
        DisplayRefreshRecommendations.Clear();
        foreach (var recommendation in recommendations)
        {
            DisplayRefreshRecommendations.Add(new DisplayRefreshRecommendationItem(recommendation));
        }

        OnPropertyChanged(nameof(HasDisplayRefreshRecommendations));
        OnPropertyChanged(nameof(DisplayRefreshSummary));
        RefreshToolCommandStates();
    }

    private void ReplaceDriverHardwareInventory(IReadOnlyList<DriverHardwareInfo> hardware)
    {
        DriverHardwareInventory.Clear();
        foreach (var component in hardware)
        {
            DriverHardwareInventory.Add(component);
        }

        OnPropertyChanged(nameof(DriverHardwareSummary));
    }

    private void ReplaceDriverUpdateCandidates(IReadOnlyList<DriverUpdateCandidate> candidates)
    {
        foreach (var item in DriverUpdateCandidates)
        {
            item.PropertyChanged -= DriverUpdateItem_OnPropertyChanged;
        }

        DriverUpdateCandidates.Clear();
        foreach (var candidate in candidates)
        {
            var item = new DriverUpdateItem(candidate);
            item.PropertyChanged += DriverUpdateItem_OnPropertyChanged;
            DriverUpdateCandidates.Add(item);
        }

        OnPropertyChanged(nameof(HasSelectedDriverUpdates));
        OnPropertyChanged(nameof(DriverUpdateSelectionSummary));
        RefreshToolCommandStates();
    }

    private void ReplaceEmptyDirectoryCandidates(string rootPath, IReadOnlyList<AutoClicker.Core.Models.EmptyDirectoryCandidate> candidates)
    {
        foreach (var item in EmptyDirectoryCandidates)
        {
            item.PropertyChanged -= EmptyDirectoryItem_OnPropertyChanged;
        }

        EmptyDirectoryCandidates.Clear();
        foreach (var candidate in candidates)
        {
            var item = new EmptyDirectoryItem(rootPath, candidate);
            item.PropertyChanged += EmptyDirectoryItem_OnPropertyChanged;
            EmptyDirectoryCandidates.Add(item);
        }

        OnPropertyChanged(nameof(HasSelectedEmptyDirectories));
        OnPropertyChanged(nameof(EmptyDirectorySelectionSummary));
        RefreshToolCommandStates();
    }

    private void StartEmptyDirectoryScanProgress(string rootPath)
    {
        EmptyDirectoryScanProgressValue = 0;
        EmptyDirectoryScanProgressMaximum = GetEmptyDirectoryScanCachedMaxFolderCount(rootPath);
        EmptyDirectoryScanProgressPath = rootPath;
        IsEmptyDirectoryScanProgressVisible = true;
        OnPropertyChanged(nameof(EmptyDirectoryScanProgressSummary));
    }

    private void StartShortcutHotkeyScanProgress()
    {
        ShortcutHotkeyScanProgressValue = 0;
        ShortcutHotkeyScanProgressMaximum = GetShortcutHotkeyScanCachedMaxFolderCount();
        ShortcutHotkeyScannedShortcutCount = 0;
        ShortcutHotkeyScanProgressPath = L(AppLanguageKeys.ToolsShortcutHotkeyScanPreparing);
        IsShortcutHotkeyScanProgressVisible = true;
        OnPropertyChanged(nameof(ShortcutHotkeyScanProgressSummary));
    }

    private void UpdateEmptyDirectoryScanProgress(EmptyDirectoryScanProgress progress)
    {
        EmptyDirectoryScanProgressMaximum = Math.Max(progress.TotalDirectoryCount, 1);
        EmptyDirectoryScanProgressValue = Math.Min(progress.CompletedDirectoryCount, EmptyDirectoryScanProgressMaximum);
        EmptyDirectoryScanProgressPath = progress.CurrentPath;
        OnPropertyChanged(nameof(EmptyDirectoryScanProgressSummary));
    }

    private void UpdateShortcutHotkeyScanProgress(ShortcutHotkeyScanProgress progress)
    {
        ShortcutHotkeyScanProgressMaximum = Math.Max(progress.TotalFolderCount, 1);
        ShortcutHotkeyScanProgressValue = Math.Min(progress.CompletedFolderCount, ShortcutHotkeyScanProgressMaximum);
        ShortcutHotkeyScannedShortcutCount = Math.Max(progress.ScannedShortcutCount, 0);
        ShortcutHotkeyScanProgressPath = progress.CurrentPath;
        OnPropertyChanged(nameof(ShortcutHotkeyScanProgressSummary));
    }

    private void ResetEmptyDirectoryScanProgress()
    {
        IsEmptyDirectoryScanProgressVisible = false;
        EmptyDirectoryScanProgressValue = 0;
        EmptyDirectoryScanProgressMaximum = 1;
        EmptyDirectoryScanProgressPath = string.Empty;
        OnPropertyChanged(nameof(EmptyDirectoryScanProgressSummary));
    }

    private void ResetShortcutHotkeyScanProgress()
    {
        IsShortcutHotkeyScanProgressVisible = false;
        ShortcutHotkeyScanProgressValue = 0;
        ShortcutHotkeyScanProgressMaximum = 1;
        ShortcutHotkeyScannedShortcutCount = 0;
        ShortcutHotkeyScanProgressPath = string.Empty;
        OnPropertyChanged(nameof(ShortcutHotkeyScanProgressSummary));
    }

    private void RefreshToolCommandStates()
    {
        ShowAssignedShortcutHotkeysCommand.NotifyCanExecuteChanged();
        RefreshMouseSensitivityStatusCommand.NotifyCanExecuteChanged();
        ApplyMouseSensitivityCommand.NotifyCanExecuteChanged();
        ScanDisplayRefreshRecommendationsCommand.NotifyCanExecuteChanged();
        ApplyDisplayRefreshRecommendationsCommand.NotifyCanExecuteChanged();
        ScanHardwareCheckCommand.NotifyCanExecuteChanged();
        CopyHardwareCheckInfoCommand.NotifyCanExecuteChanged();
        ScanDriverUpdatesCommand.NotifyCanExecuteChanged();
        SelectRecommendedDriverUpdatesCommand.NotifyCanExecuteChanged();
        SelectAllDriverUpdatesCommand.NotifyCanExecuteChanged();
        ClearDriverUpdateSelectionCommand.NotifyCanExecuteChanged();
        InstallSelectedDriverUpdatesCommand.NotifyCanExecuteChanged();
        PrepareWindows11EeaMediaCommand.NotifyCanExecuteChanged();
        RefreshSearchReplacementStatusCommand.NotifyCanExecuteChanged();
        ApplySearchReplacementCommand.NotifyCanExecuteChanged();
        RestoreSearchReplacementCommand.NotifyCanExecuteChanged();
        RefreshSearchReindexStatusCommand.NotifyCanExecuteChanged();
        ReindexWindowsSearchCommand.NotifyCanExecuteChanged();
        RefreshTelemetryStatusCommand.NotifyCanExecuteChanged();
        ApplyTelemetryReductionCommand.NotifyCanExecuteChanged();
        RestoreTelemetryDefaultsCommand.NotifyCanExecuteChanged();
        ToggleWindowPinCommand.NotifyCanExecuteChanged();
        RefreshOneDriveStatusCommand.NotifyCanExecuteChanged();
        RemoveOneDriveCommand.NotifyCanExecuteChanged();
        RefreshEdgeStatusCommand.NotifyCanExecuteChanged();
        RemoveEdgeCommand.NotifyCanExecuteChanged();
        RefreshFnCtrlSwapStatusCommand.NotifyCanExecuteChanged();
        ToggleFnCtrlSwapCommand.NotifyCanExecuteChanged();
        ApplySystemDarkModeCommand.NotifyCanExecuteChanged();
        OpenEmptyDirectoryRootCommand.NotifyCanExecuteChanged();
        ScanEmptyDirectoriesCommand.NotifyCanExecuteChanged();
        SelectAllEmptyDirectoriesCommand.NotifyCanExecuteChanged();
        ClearEmptyDirectorySelectionCommand.NotifyCanExecuteChanged();
        DeleteSelectedEmptyDirectoriesCommand.NotifyCanExecuteChanged();
    }

    private string BuildEmptyDirectorySelectionSummary()
    {
        var selectedCount = EmptyDirectoryCandidates.Count(item => item.IsSelected);
        return F(
            AppLanguageKeys.ToolsEmptyDirectorySelectionSummaryFormat,
            selectedCount,
            EmptyDirectoryCandidates.Count,
            EmptyDirectoryCandidates.Count == 1 ? string.Empty : "s");
    }

    private string BuildEmptyDirectoryScanProgressSummary()
    {
        if (!IsEmptyDirectoryScanProgressVisible)
        {
            return string.Empty;
        }

        return F(
            AppLanguageKeys.ToolsEmptyDirectoryScanProgressSummaryFormat,
            EmptyDirectoryScanProgressValue,
            EmptyDirectoryScanProgressMaximum,
            EmptyDirectoryScanProgressPath);
    }

    private string BuildShortcutHotkeyScanProgressSummary()
    {
        if (!IsShortcutHotkeyScanProgressVisible)
        {
            return string.Empty;
        }

        return F(
            AppLanguageKeys.ToolsShortcutHotkeyScanProgressSummaryFormat,
            ShortcutHotkeyScanProgressValue,
            ShortcutHotkeyScanProgressMaximum,
            ShortcutHotkeyScannedShortcutCount);
    }

    private int GetShortcutHotkeyScanCachedMaxFolderCount() =>
        Math.Max(shortcutHotkeyScanMaxFolderCountCache, 1);

    private int GetEmptyDirectoryScanCachedMaxFolderCount(string rootPath)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            return 1;
        }

        var normalizedRootPath = Path.GetFullPath(rootPath);
        return emptyDirectoryScanMaxFolderCountCache.TryGetValue(normalizedRootPath, out var cachedMaximum)
            ? Math.Max(cachedMaximum, 1)
            : 1;
    }

    private void PersistShortcutHotkeyScanMaxFolderCount(int maximum)
    {
        var normalizedMaximum = Math.Max(maximum, 0);
        if (normalizedMaximum <= 0 || normalizedMaximum == shortcutHotkeyScanMaxFolderCountCache)
        {
            return;
        }

        shortcutHotkeyScanMaxFolderCountCache = normalizedMaximum;
        ScheduleSettingsAutoSave();
    }

    private void PersistEmptyDirectoryScanMaxFolderCount(string rootPath, int maximum)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            return;
        }

        var normalizedMaximum = Math.Max(maximum, 0);
        if (normalizedMaximum <= 0)
        {
            return;
        }

        var normalizedRootPath = Path.GetFullPath(rootPath);
        if (emptyDirectoryScanMaxFolderCountCache.TryGetValue(normalizedRootPath, out var existingMaximum)
            && existingMaximum == normalizedMaximum)
        {
            return;
        }

        emptyDirectoryScanMaxFolderCountCache[normalizedRootPath] = normalizedMaximum;
        ScheduleSettingsAutoSave();
    }

    private string BuildMouseSensitivitySummary()
    {
        var currentText = BuildMouseSensitivityLevelText(CurrentMouseSensitivityLevel);
        return SelectedMouseSensitivityLevel == CurrentMouseSensitivityLevel
            ? F(AppLanguageKeys.ToolsMouseSensitivitySummaryCurrentFormat, currentText)
            : F(AppLanguageKeys.ToolsMouseSensitivitySummaryPickedFormat, currentText, BuildMouseSensitivityLevelText(SelectedMouseSensitivityLevel));
    }

    private string BuildMouseSensitivitySelectionGuidance()
    {
        var level = Math.Clamp(SelectedMouseSensitivityLevel, 1, 20);

        var rangeGuidance = level switch
        {
            <= 4 => L(AppLanguageKeys.ToolsMouseSensitivityGuidanceVerySlow),
            <= 8 => L(AppLanguageKeys.ToolsMouseSensitivityGuidanceSlow),
            <= 12 => L(AppLanguageKeys.ToolsMouseSensitivityGuidanceBalanced),
            <= 16 => L(AppLanguageKeys.ToolsMouseSensitivityGuidanceFast),
            _ => L(AppLanguageKeys.ToolsMouseSensitivityGuidanceVeryFast),
        };

        return F(AppLanguageKeys.ToolsMouseSensitivitySelectionGuidanceFormat, BuildMouseSensitivityLevelText(level), rangeGuidance);
    }

    private string BuildMouseSensitivityLevelText(int level)
    {
        var normalizedLevel = Math.Clamp(level, 1, 20);
        var feelLabel = normalizedLevel switch
        {
            <= 4 => L(AppLanguageKeys.ToolsMouseSensitivityFeelVerySlow),
            <= 8 => L(AppLanguageKeys.ToolsMouseSensitivityFeelSlow),
            <= 12 => L(AppLanguageKeys.ToolsMouseSensitivityFeelBalanced),
            <= 16 => L(AppLanguageKeys.ToolsMouseSensitivityFeelFast),
            _ => L(AppLanguageKeys.ToolsMouseSensitivityFeelVeryFast),
        };

        return normalizedLevel == 10
            ? F(AppLanguageKeys.ToolsMouseSensitivityLevelTextMiddleFormat, feelLabel, normalizedLevel)
            : F(AppLanguageKeys.ToolsMouseSensitivityLevelTextFormat, feelLabel, normalizedLevel);
    }

    private string BuildDisplayRefreshSummary()
    {
        var total = DisplayRefreshRecommendations.Count;
        var needsChangeCount = DisplayRefreshRecommendations.Count(item => item.NeedsChange);
        if (total == 0)
        {
            return L(AppLanguageKeys.ToolsDisplayRefreshSummaryNone);
        }

        return needsChangeCount == 0
            ? F(AppLanguageKeys.ToolsDisplayRefreshSummaryAllBestFormat, total, total == 1 ? string.Empty : "s")
            : F(AppLanguageKeys.ToolsDisplayRefreshSummaryCanRunFasterFormat, total, total == 1 ? string.Empty : "s", needsChangeCount);
    }

    private string BuildHardwareGraphicsSummary() =>
        F(AppLanguageKeys.ToolsHardwareGraphicsSummaryFormat, HardwareGraphicsAdapters.Count, HardwareGraphicsAdapters.Count == 1 ? string.Empty : "s");

    private string BuildHardwareStorageSummary() =>
        F(AppLanguageKeys.ToolsHardwareStorageSummaryFormat, HardwareStorageDrives.Count, HardwareStorageDrives.Count == 1 ? string.Empty : "s");

    private string BuildHardwarePartitionSummary() =>
        HardwareStoragePartitions.Count == 0
            ? L(AppLanguageKeys.ToolsHardwarePartitionSummaryNone)
            : F(AppLanguageKeys.ToolsHardwarePartitionSummaryFormat, HardwareStoragePartitions.Count, HardwareStoragePartitions.Count == 1 ? string.Empty : "s");

    private string BuildHardwareSensorSummary() =>
        HardwareSensors.Count == 0
            ? L(AppLanguageKeys.ToolsHardwareSensorSummaryNone)
            : F(AppLanguageKeys.ToolsHardwareSensorSummaryFormat, HardwareSensors.Count, HardwareSensors.Count == 1 ? string.Empty : "s");

    private string BuildHardwarePciSummary() =>
        HardwarePciDevices.Count == 0
            ? L(AppLanguageKeys.ToolsHardwarePciSummaryNone)
            : F(AppLanguageKeys.ToolsHardwarePciSummaryFormat, HardwarePciDevices.Count, HardwarePciDevices.Count == 1 ? string.Empty : "s");

    private string BuildHardwareRaidSummary() =>
        HardwareRaidDetails.Count == 0
            ? L(AppLanguageKeys.ToolsHardwareRaidSummaryNone)
            : F(AppLanguageKeys.ToolsHardwareRaidSummaryFormat, HardwareRaidDetails.Count, HardwareRaidDetails.Count == 1 ? string.Empty : "s");

    private string BuildHardwareCheckClipboardText()
    {
        var builder = new StringBuilder();
        builder.AppendLine(L(AppLanguageKeys.ToolsHardwareClipboardTitle));
        builder.AppendLine(F(AppLanguageKeys.ToolsHardwareClipboardCapturedFormat, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
        builder.AppendLine();
        builder.AppendLine(L(AppLanguageKeys.ToolsHardwareClipboardSystemSection));
        builder.AppendLine(F(AppLanguageKeys.ToolsHardwareClipboardOverviewFormat, HardwareCheckSystemSummary));
        builder.AppendLine(F(AppLanguageKeys.ToolsHardwareClipboardHealthSummaryFormat, HardwareCheckHealthSummary));
        builder.AppendLine(F(AppLanguageKeys.ToolsHardwareClipboardOperatingSystemFormat, HardwareCheckOperatingSystemSummary));
        builder.AppendLine(F(AppLanguageKeys.ToolsHardwareClipboardProcessorFormat, HardwareCheckProcessorSummary));
        builder.AppendLine(F(AppLanguageKeys.ToolsHardwareClipboardMemoryFormat, HardwareCheckMemorySummary));
        builder.AppendLine(F(AppLanguageKeys.ToolsHardwareClipboardMotherboardFormat, HardwareCheckMotherboardSummary));
        builder.AppendLine(F(AppLanguageKeys.ToolsHardwareClipboardBiosFormat, HardwareCheckBiosSummary));

        AppendGraphicsSection(builder);
        AppendStorageSection(builder);
        AppendPartitionSection(builder);
        AppendSensorSection(builder);
        AppendPciSection(builder);
        AppendRaidSection(builder);

        return builder.ToString().TrimEnd();
    }

    private void AppendGraphicsSection(StringBuilder builder)
    {
        builder.AppendLine();
        builder.AppendLine(L(AppLanguageKeys.ToolsHardwareClipboardGraphicsSection));
        builder.AppendLine(F(AppLanguageKeys.ToolsHardwareClipboardSummaryFormat, HardwareGraphicsSummary));

        foreach (var adapter in HardwareGraphicsAdapters)
        {
            builder.AppendLine($"- {adapter.Name}");
            builder.AppendLine(F(AppLanguageKeys.ToolsHardwareClipboardDriverFormat, adapter.DriverVersion));
            builder.AppendLine(F(AppLanguageKeys.ToolsHardwareClipboardMemoryFieldFormat, adapter.AdapterMemory));
        }
    }

    private void AppendStorageSection(StringBuilder builder)
    {
        builder.AppendLine();
        builder.AppendLine(L(AppLanguageKeys.ToolsHardwareClipboardStorageSection));
        builder.AppendLine(F(AppLanguageKeys.ToolsHardwareClipboardSummaryFormat, HardwareStorageSummary));

        foreach (var drive in HardwareStorageDrives)
        {
            builder.AppendLine($"- {drive.Model}");
            builder.AppendLine(F(AppLanguageKeys.ToolsHardwareClipboardSizeFormat, drive.Size));
            builder.AppendLine(F(AppLanguageKeys.ToolsHardwareClipboardInterfaceFormat, drive.InterfaceType));
            builder.AppendLine(F(AppLanguageKeys.ToolsHardwareClipboardMediaFormat, drive.MediaType));
            builder.AppendLine(F(AppLanguageKeys.ToolsHardwareClipboardHealthFormat, drive.HealthStatus));
            builder.AppendLine(F(AppLanguageKeys.ToolsHardwareClipboardSmartFormat, drive.SmartStatus));
            builder.AppendLine(F(AppLanguageKeys.ToolsHardwareClipboardFirmwareFormat, drive.FirmwareVersion));
            builder.AppendLine(F(AppLanguageKeys.ToolsHardwareClipboardSerialFormat, drive.SerialNumber));

            if (!string.IsNullOrWhiteSpace(drive.Notes))
            {
                builder.AppendLine(F(AppLanguageKeys.ToolsHardwareClipboardNotesFormat, drive.Notes));
            }
        }
    }

    private void AppendPartitionSection(StringBuilder builder)
    {
        builder.AppendLine();
        builder.AppendLine(L(AppLanguageKeys.ToolsHardwareClipboardPartitionsSection));
        builder.AppendLine(F(AppLanguageKeys.ToolsHardwareClipboardSummaryFormat, HardwarePartitionSummary));

        foreach (var partition in HardwareStoragePartitions)
        {
            builder.AppendLine($"- {partition.PartitionName}");
            builder.AppendLine(F(AppLanguageKeys.ToolsHardwareClipboardDiskFormat, partition.DiskName));
            builder.AppendLine(F(AppLanguageKeys.ToolsHardwareClipboardSizeFormat, partition.Size));
            builder.AppendLine(F(AppLanguageKeys.ToolsHardwareClipboardTypeFormat, partition.Type));
            builder.AppendLine(F(AppLanguageKeys.ToolsHardwareClipboardVolumeFormat, partition.Volume));
            builder.AppendLine(F(AppLanguageKeys.ToolsHardwareClipboardFileSystemFormat, partition.FileSystem));
            builder.AppendLine(F(AppLanguageKeys.ToolsHardwareClipboardFreeSpaceFormat, partition.FreeSpace));
            builder.AppendLine(F(AppLanguageKeys.ToolsHardwareClipboardStatusFormat, partition.Status));
        }
    }

    private void AppendSensorSection(StringBuilder builder)
    {
        builder.AppendLine();
        builder.AppendLine(L(AppLanguageKeys.ToolsHardwareClipboardSensorsSection));
        builder.AppendLine(F(AppLanguageKeys.ToolsHardwareClipboardSummaryFormat, HardwareSensorSummary));

        foreach (var sensor in HardwareSensors)
        {
            builder.AppendLine($"- {sensor.Name}");
            builder.AppendLine(F(AppLanguageKeys.ToolsHardwareClipboardCategoryFormat, sensor.Category));
            builder.AppendLine(F(AppLanguageKeys.ToolsHardwareClipboardReadingFormat, sensor.CurrentValue));
            builder.AppendLine(F(AppLanguageKeys.ToolsHardwareClipboardSourceFormat, sensor.Source));
            builder.AppendLine(F(AppLanguageKeys.ToolsHardwareClipboardStatusFormat, sensor.Status));
        }
    }

    private void AppendPciSection(StringBuilder builder)
    {
        builder.AppendLine();
        builder.AppendLine(L(AppLanguageKeys.ToolsHardwareClipboardPciSection));
        builder.AppendLine(F(AppLanguageKeys.ToolsHardwareClipboardSummaryFormat, HardwarePciSummary));

        foreach (var device in HardwarePciDevices)
        {
            builder.AppendLine($"- {device.Name}");
            builder.AppendLine(F(AppLanguageKeys.ToolsHardwareClipboardClassFormat, device.DeviceClass));
            builder.AppendLine(F(AppLanguageKeys.ToolsHardwareClipboardManufacturerFormat, device.Manufacturer));
            builder.AppendLine(F(AppLanguageKeys.ToolsHardwareClipboardLocationFormat, device.Location));
            builder.AppendLine(F(AppLanguageKeys.ToolsHardwareClipboardStatusFormat, device.Status));
        }
    }

    private void AppendRaidSection(StringBuilder builder)
    {
        builder.AppendLine();
        builder.AppendLine(L(AppLanguageKeys.ToolsHardwareClipboardRaidSection));
        builder.AppendLine(F(AppLanguageKeys.ToolsHardwareClipboardSummaryFormat, HardwareRaidSummary));

        foreach (var detail in HardwareRaidDetails)
        {
            builder.AppendLine($"- {detail.Name}");
            builder.AppendLine(F(AppLanguageKeys.ToolsHardwareClipboardTypeFormat, detail.Type));
            builder.AppendLine(F(AppLanguageKeys.ToolsHardwareClipboardStatusFormat, detail.Status));
            builder.AppendLine(F(AppLanguageKeys.ToolsHardwareClipboardDetailsFormat, detail.Details));
            builder.AppendLine(F(AppLanguageKeys.ToolsHardwareClipboardSourceFormat, detail.Source));
        }
    }

    private string BuildDriverHardwareSummary() =>
        F(AppLanguageKeys.ToolsDriverHardwareSummaryFormat, DriverHardwareInventory.Count, DriverHardwareInventory.Count == 1 ? string.Empty : "s");

    private string BuildDriverUpdateSelectionSummary()
    {
        var selectedCount = DriverUpdateCandidates.Count(item => item.IsSelected);
        var recommendedCount = DriverUpdateCandidates.Count(item => !item.IsOptional);
        var optionalCount = DriverUpdateCandidates.Count - recommendedCount;
        return F(AppLanguageKeys.ToolsDriverUpdateSelectionSummaryFormat, selectedCount, recommendedCount, optionalCount);
    }

    private void AddToolLog(string message)
    {
        ToolLogEntries.Insert(0, $"{DateTime.Now:HH:mm:ss}  {message}");
        AddActivityLog(message);

        while (ToolLogEntries.Count > 200)
        {
            ToolLogEntries.RemoveAt(ToolLogEntries.Count - 1);
        }
    }

}
