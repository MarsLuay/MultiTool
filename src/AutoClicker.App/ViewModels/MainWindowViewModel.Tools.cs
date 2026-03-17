using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using AutoClicker.App.Models;
using AutoClicker.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AutoClicker.App.ViewModels;

public partial class MainWindowViewModel
{
    private const string WindowsUpdateOptionalUpdatesSettingsUri = "ms-settings:windowsupdate-optionalupdates";
    private const string WindowsUpdateSettingsUri = "ms-settings:windowsupdate";

    private static readonly IReadOnlyList<UsefulSiteItem> DefaultUsefulSites =
    [
        new(
            "FMHY.net",
            "https://fmhy.net/",
            "The largest collection of free stuff on the internet."),
        new(
            "Guide to sailing the Seven Seas",
            "https://rentry.co/megathread",
            "A comprehensive guide to learn how to get free things!"),
        new(
            "Z-Library (the true edition)",
            "http://zlibrary24tuxziyiyfr7zd46ytefdqbqd2axkmxm4o5374ptpc52fad.onion/",
            "The largest free ebook library on the internet. Tor browser required (and recommended)."),
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

    public string UsefulSitesToggleText => AreUsefulSitesVisible ? "Hide Useful Sites" : "Useful Sites";

    [ObservableProperty]
    private string emptyDirectoryRootPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    [ObservableProperty]
    private string emptyDirectoryStatusMessage = "Choose a folder tree to scan for empty directories.";

    [ObservableProperty]
    private string shortcutHotkeyStatusMessage = "Scan Windows .lnk hotkeys and supported app keymap files on this PC, then include built-in Windows and common shortcuts in one viewer.";

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
    private string mouseSensitivityStatusMessage = "Pick a slower or faster mouse speed. 10/20 is the normal middle setting in Windows, so it is a good place to start if you are unsure.";

    [ObservableProperty]
    private string displayRefreshStatusMessage = "Click Check Displays to see if your monitors can run at a faster refresh rate.";

    [ObservableProperty]
    private string hardwareCheckSystemSummary = "No hardware scan yet.";

    [ObservableProperty]
    private string hardwareCheckOperatingSystemSummary = "Windows details will appear after scanning.";

    [ObservableProperty]
    private string hardwareCheckProcessorSummary = "Processor details will appear after scanning.";

    [ObservableProperty]
    private string hardwareCheckMemorySummary = "Memory details will appear after scanning.";

    [ObservableProperty]
    private string hardwareCheckMotherboardSummary = "Motherboard details will appear after scanning.";

    [ObservableProperty]
    private string hardwareCheckBiosSummary = "BIOS details will appear after scanning.";

    [ObservableProperty]
    private string hardwareCheckStatusMessage = "Scan the PC to review core hardware details, live sensor telemetry, PCIe devices, storage health, partitions, and RAID details.";

    [ObservableProperty]
    private string driverUpdateStatusMessage = "Scan this PC's hardware and check Windows Update for recommended and optional driver updates. Some driver offers can only be finished through Windows Update's own Optional Updates page.";

    [ObservableProperty]
    private bool isToolBusy;

    [ObservableProperty]
    private string darkModeToolStatusMessage = "Apply Windows dark mode preferences for the shell and supported apps.";

    [ObservableProperty]
    private string searchReplacementStatusMessage = "Replace the built-in Windows Search with Flow Launcher for a faster search experience. Use Restore to switch back to Windows Search at any time.";

    [ObservableProperty]
    private string oneDriveToolStatusMessage = "Check whether OneDrive is present, apply the system disable policy, and remove it with Windows' built-in uninstaller when available.";

    [ObservableProperty]
    private string edgeToolStatusMessage = "Detect whether Microsoft Edge is installed and remove it using the developer-override method. An administrator prompt will appear during removal.";

    [ObservableProperty]
    private string fnCtrlSwapStatusMessage = "Detect Lenovo BIOS Fn/Ctrl key swap support and switch the key positions when available.";

    [ObservableProperty]
    private bool isFnCtrlSwapSupported;

    [ObservableProperty]
    private bool areUsefulSitesVisible;

    [ObservableProperty]
    private string usefulSitesStatusMessage = "Open a curated list of useful sites from inside MultiTool.";

    [ObservableProperty]
    private string windows11EeaInstallStatusMessage = "Build the official Windows 11 media prep files with Ireland as the EEA regional default, then let MultiTool watch for the finished USB and copy the answer file automatically.";

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
            MouseSensitivityStatusMessage = $"Unable to read the Windows mouse sensitivity: {ex.Message}";
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
            OneDriveToolStatusMessage = $"Unable to check OneDrive status: {ex.Message}";
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
            EdgeToolStatusMessage = $"Unable to check Edge status: {ex.Message}";
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
            SearchReplacementStatusMessage = $"Unable to check the Flow Launcher search replacement: {ex.Message}";
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
            FnCtrlSwapStatusMessage = $"Unable to check Fn/Ctrl swap status: {ex.Message}";
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
        var selectedPath = folderPickerService.PickFolder(EmptyDirectoryRootPath, "Select the folder tree to scan for empty directories");
        if (string.IsNullOrWhiteSpace(selectedPath))
        {
            EmptyDirectoryStatusMessage = "Folder selection canceled.";
            AddToolLog(EmptyDirectoryStatusMessage);
            return;
        }

        EmptyDirectoryRootPath = selectedPath;
        EmptyDirectoryStatusMessage = $"Empty directory scan root set to {selectedPath}.";
        AddToolLog(EmptyDirectoryStatusMessage);
    }

    private bool CanShowAssignedShortcutHotkeys => !IsToolBusy;

    [RelayCommand(CanExecute = nameof(CanShowAssignedShortcutHotkeys))]
    private async Task ShowAssignedShortcutHotkeysAsync()
    {
        IsToolBusy = true;
        ShortcutHotkeyStatusMessage = "Scanning fixed drives for assigned Windows shortcut keys, loading supported app keymaps, and adding built-in shortcut references...";
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
                : $" Skipped {result.Warnings.Count} folder or shortcut read{(result.Warnings.Count == 1 ? string.Empty : "s")}.";

            var detectedCount = result.Shortcuts.Count(static shortcut => !shortcut.IsReferenceShortcut);
            var referenceCount = result.Shortcuts.Count(static shortcut => shortcut.IsReferenceShortcut);
            ShortcutHotkeyStatusMessage = detectedCount == 0 && referenceCount == 0
                ? $"Scanned {result.ScannedShortcutCount} .lnk shortcut file{(result.ScannedShortcutCount == 1 ? string.Empty : "s")}. No shortcut keys were found.{warningSuffix}"
                : $"Opened the shortcut viewer with {detectedCount} detected .lnk hotkey{(detectedCount == 1 ? string.Empty : "s")} and {referenceCount} built-in/common shortcut reference entr{(referenceCount == 1 ? "y" : "ies")}.{warningSuffix}";
            AddToolLog(ShortcutHotkeyStatusMessage);

            foreach (var warning in result.Warnings.Take(10))
            {
                AddToolLog(warning);
            }
        }
        catch (Exception ex)
        {
            ShortcutHotkeyStatusMessage = $"Shortcut hotkey scan failed: {ex.Message}";
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
            ? $"Showing {UsefulSites.Count} useful site{(UsefulSites.Count == 1 ? string.Empty : "s")}."
            : "Useful site list hidden.";
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

            UsefulSitesStatusMessage = $"Opened {site.DisplayName} in {launchResult.BrowserDisplayName}.";
            AddToolLog(UsefulSitesStatusMessage);
        }
        catch (Exception ex)
        {
            UsefulSitesStatusMessage = $"Unable to open {site.DisplayName}: {ex.Message}";
            AddToolLog(UsefulSitesStatusMessage);
        }
    }

    private bool CanPrepareWindows11EeaMedia => !IsToolBusy;

    [RelayCommand(CanExecute = nameof(CanPrepareWindows11EeaMedia))]
    private async Task PrepareWindows11EeaMediaAsync()
    {
        IsToolBusy = true;
        Windows11EeaInstallStatusMessage = "Downloading Microsoft's Windows 11 Media Creation Tool and preparing the EEA setup files...";
        AddToolLog(Windows11EeaInstallStatusMessage);

        try
        {
            var result = await windows11EeaMediaService.PrepareAsync().ConfigureAwait(true);
            Windows11EeaInstallStatusMessage = result.Message;
            AddToolLog(result.Message);
        }
        catch (Exception ex)
        {
            Windows11EeaInstallStatusMessage = $"Windows 11 EEA media prep failed: {ex.Message}";
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

    [RelayCommand(CanExecute = nameof(CanApplySearchReplacement))]
    private async Task ApplySearchReplacementAsync()
    {
        IsToolBusy = true;
        SearchReplacementStatusMessage = "Setting up Flow Launcher + Everything as the Win + S search replacement...";
        AddToolLog(SearchReplacementStatusMessage);

        try
        {
            var result = await windowsSearchReplacementService.ApplyAsync().ConfigureAwait(true);
            SearchReplacementStatusMessage = result.Message;
            AddToolLog(result.Message);
        }
        catch (Exception ex)
        {
            SearchReplacementStatusMessage = $"Search replacement setup failed: {ex.Message}";
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
        SearchReplacementStatusMessage = "Restoring Windows Search and removing the Flow Launcher Win + S replacement...";
        AddToolLog(SearchReplacementStatusMessage);

        try
        {
            var result = await windowsSearchReplacementService.RestoreAsync().ConfigureAwait(true);
            SearchReplacementStatusMessage = result.Message;
            AddToolLog(result.Message);
        }
        catch (Exception ex)
        {
            SearchReplacementStatusMessage = $"Search restoration failed: {ex.Message}";
            AddToolLog(SearchReplacementStatusMessage);
        }
        finally
        {
            IsToolBusy = false;
        }
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
                SettingsStatusMessage = "Dark mode is on.";
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

            DarkModeToolStatusMessage = "Opened Windows color settings for anything that still needs a manual dark mode change.";
            AddToolLog(DarkModeToolStatusMessage);
        }
        catch (Exception ex)
        {
            DarkModeToolStatusMessage = $"Unable to open Windows color settings: {ex.Message}";
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

            EmptyDirectoryStatusMessage = "Opened the scan root folder.";
            AddToolLog(EmptyDirectoryStatusMessage);
        }
        catch (Exception ex)
        {
            EmptyDirectoryStatusMessage = $"Unable to open the scan root folder: {ex.Message}";
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
        MouseSensitivityStatusMessage = $"Applying {BuildMouseSensitivityLevelText(SelectedMouseSensitivityLevel)}...";
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
            MouseSensitivityStatusMessage = $"Mouse sensitivity update failed: {ex.Message}";
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

            MouseSensitivityStatusMessage = "Opened Windows mouse settings.";
            AddToolLog(MouseSensitivityStatusMessage);
        }
        catch (Exception ex)
        {
            MouseSensitivityStatusMessage = $"Unable to open Windows mouse settings: {ex.Message}";
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
        DisplayRefreshStatusMessage = "Applying top refresh rates to connected displays...";
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
                ? "All displays were already at their top refresh rate for the current resolution."
                : $"{changedCount} display{(changedCount == 1 ? string.Empty : "s")} updated, {failedCount} failed.";
            AddToolLog(DisplayRefreshStatusMessage);
        }
        catch (Exception ex)
        {
            DisplayRefreshStatusMessage = $"Display refresh update failed: {ex.Message}";
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
            HardwareCheckStatusMessage = "Copied hardware check details to the clipboard.";
            AddToolLog(HardwareCheckStatusMessage);
        }
        catch (Exception ex)
        {
            HardwareCheckStatusMessage = $"Unable to copy the hardware check details: {ex.Message}";
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

        DriverUpdateStatusMessage = "Selected recommended driver updates and left optional ones unchecked.";
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

        DriverUpdateStatusMessage = "Selected all discovered driver updates.";
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

        DriverUpdateStatusMessage = "Cleared the driver update selection.";
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
            DriverUpdateStatusMessage = "Select at least one driver update first.";
            AddToolLog(DriverUpdateStatusMessage);
            return;
        }

        IsToolBusy = true;
        DriverUpdateStatusMessage = $"Installing {selectedItems.Length} driver update{(selectedItems.Length == 1 ? string.Empty : "s")} through Windows Update...";
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
                statusParts.Add($"{installedCount} installed");
            }

            if (manualFlowCount > 0)
            {
                statusParts.Add($"{manualFlowCount} need Windows Update's own interactive flow");
            }

            if (failedCount > 0)
            {
                statusParts.Add($"{failedCount} failed");
            }

            if (statusParts.Count == 0)
            {
                statusParts.Add("No driver changes were applied");
            }

            DriverUpdateStatusMessage =
                $"{string.Join(", ", statusParts)}. {DriverUpdateCandidates.Count} update{(DriverUpdateCandidates.Count == 1 ? string.Empty : "s")} remain." +
                (manualFlowCount > 0 ? " Use Open Optional Updates for the ones Windows will not install silently." : string.Empty) +
                (restartCount > 0 ? $" Restart required for {restartCount} item{(restartCount == 1 ? string.Empty : "s")}." : string.Empty);
            AddToolLog(DriverUpdateStatusMessage);
        }
        catch (Exception ex)
        {
            DriverUpdateStatusMessage = $"Driver install failed: {ex.Message}";
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
            try
            {
                Process.Start(
                    new ProcessStartInfo
                    {
                        FileName = WindowsUpdateOptionalUpdatesSettingsUri,
                        UseShellExecute = true,
                    });
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

            DriverUpdateStatusMessage = "Opened Windows Update Optional Updates so you can finish any driver installs that need Windows' own interactive flow.";
            AddToolLog(DriverUpdateStatusMessage);
        }
        catch (Exception ex)
        {
            DriverUpdateStatusMessage = $"Unable to open Windows Update Optional Updates: {ex.Message}";
            AddToolLog(DriverUpdateStatusMessage);
        }
    }

    private bool CanRemoveOneDrive => !IsToolBusy;

    [RelayCommand(CanExecute = nameof(CanRemoveOneDrive))]
    private async Task RemoveOneDriveAsync()
    {
        IsToolBusy = true;
        OneDriveToolStatusMessage = "Removing OneDrive...";
        AddToolLog(OneDriveToolStatusMessage);

        try
        {
            var result = await oneDriveRemovalService.RemoveAsync().ConfigureAwait(true);
            OneDriveToolStatusMessage = result.Message;
            AddToolLog(result.Message);
        }
        catch (Exception ex)
        {
            OneDriveToolStatusMessage = $"OneDrive removal failed: {ex.Message}";
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
        EdgeToolStatusMessage = "Removing Microsoft Edge...";
        AddToolLog(EdgeToolStatusMessage);

        try
        {
            var result = await edgeRemovalService.RemoveAsync().ConfigureAwait(true);
            EdgeToolStatusMessage = result.Message;
            AddToolLog(result.Message);
        }
        catch (Exception ex)
        {
            EdgeToolStatusMessage = $"Edge removal failed: {ex.Message}";
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
        FnCtrlSwapStatusMessage = "Applying Fn/Ctrl key swap setting...";
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
            FnCtrlSwapStatusMessage = $"Fn/Ctrl swap failed: {ex.Message}";
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

        EmptyDirectoryStatusMessage = "Selected all empty-directory results.";
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

        EmptyDirectoryStatusMessage = "Cleared the empty-directory selection.";
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
            EmptyDirectoryStatusMessage = "Select at least one empty directory first.";
            AddToolLog(EmptyDirectoryStatusMessage);
            return;
        }

        IsToolBusy = true;
        EmptyDirectoryStatusMessage = $"Deleting {selectedPaths.Length} empty director{(selectedPaths.Length == 1 ? "y" : "ies")}...";
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
            EmptyDirectoryStatusMessage = $"{deletedCount} deleted, {missingCount} already gone, {failedCount} failed. {EmptyDirectoryCandidates.Count} deletable director{(EmptyDirectoryCandidates.Count == 1 ? "y" : "ies")} remain.";
            AddToolLog(EmptyDirectoryStatusMessage);
        }
        catch (Exception ex)
        {
            EmptyDirectoryStatusMessage = $"Delete failed: {ex.Message}";
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
            EmptyDirectoryStatusMessage = "Choose a folder tree first.";
            if (addLogEntry)
            {
                AddToolLog(EmptyDirectoryStatusMessage);
            }

            return;
        }

        if (!Directory.Exists(rootPath))
        {
            EmptyDirectoryStatusMessage = $"The folder '{rootPath}' does not exist.";
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

        EmptyDirectoryStatusMessage = $"Scanning {fullRootPath} for empty directories...";
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
                : $" Skipped {scanResult.Warnings.Count} folder{(scanResult.Warnings.Count == 1 ? string.Empty : "s")} due to access or IO errors.";
            EmptyDirectoryStatusMessage = scanResult.Candidates.Count == 0
                ? $"No deletable empty directories found.{warningSuffix}"
                : $"Found {scanResult.Candidates.Count} deletable empty director{(scanResult.Candidates.Count == 1 ? "y" : "ies")}.{warningSuffix}";

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
            EmptyDirectoryStatusMessage = $"Scan failed: {ex.Message}";
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

        DriverUpdateStatusMessage = "Detecting hardware and checking Windows Update for driver updates...";
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
                : $" Warnings: {scanResult.Warnings.Count}.";
            var interactiveSuffix = interactiveCount == 0
                ? string.Empty
                : $" {interactiveCount} need Windows Update's own interactive flow instead of MultiTool's silent install path.";

            DriverUpdateStatusMessage = scanResult.Updates.Count == 0
                ? $"Detected {scanResult.Hardware.Count} hardware component{(scanResult.Hardware.Count == 1 ? string.Empty : "s")}. No driver updates are currently available from Windows Update.{warningSuffix}"
                : $"Detected {scanResult.Hardware.Count} hardware component{(scanResult.Hardware.Count == 1 ? string.Empty : "s")}. Found {recommendedCount} recommended and {optionalCount} optional driver update{(scanResult.Updates.Count == 1 ? string.Empty : "s")}.{interactiveSuffix}{warningSuffix}";

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
            DriverUpdateStatusMessage = $"Driver scan failed: {ex.Message}";
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

        HardwareCheckStatusMessage = "Scanning this PC's hardware details...";
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
                : $" Warnings: {report.Warnings.Count}.";
            HardwareCheckStatusMessage =
                $"Hardware scan complete. Found {report.GraphicsAdapters.Count} graphics adapter{(report.GraphicsAdapters.Count == 1 ? string.Empty : "s")}, {report.StorageDrives.Count} storage drive{(report.StorageDrives.Count == 1 ? string.Empty : "s")}, {report.StoragePartitions.Count} partition{(report.StoragePartitions.Count == 1 ? string.Empty : "s")}, {report.PciDevices.Count} PCI/PCIe device{(report.PciDevices.Count == 1 ? string.Empty : "s")}, {report.Sensors.Count} sensor reading{(report.Sensors.Count == 1 ? string.Empty : "s")}, and {report.RaidDetails.Count} RAID/storage detail{(report.RaidDetails.Count == 1 ? string.Empty : "s")}.{warningSuffix}";

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
            HardwareCheckStatusMessage = $"Hardware scan failed: {ex.Message}";
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

        DisplayRefreshStatusMessage = "Checking display refresh rate recommendations...";
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
                ? "No desktop-attached displays were detected."
                : needsChangeCount == 0
                    ? $"Checked {recommendations.Count} display{(recommendations.Count == 1 ? string.Empty : "s")}. All are already at their top refresh rate for the current resolution."
                    : $"Checked {recommendations.Count} display{(recommendations.Count == 1 ? string.Empty : "s")}. {needsChangeCount} can be switched to a higher refresh rate.";

            if (addLogEntry)
            {
                AddToolLog(DisplayRefreshStatusMessage);
            }
        }
        catch (Exception ex)
        {
            DisplayRefreshStatusMessage = $"Display refresh scan failed: {ex.Message}";
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
        ShortcutHotkeyScanProgressPath = "Preparing shortcut scan...";
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
        return $"{selectedCount} selected  |  {EmptyDirectoryCandidates.Count} deletable folders found";
    }

    private string BuildEmptyDirectoryScanProgressSummary()
    {
        if (!IsEmptyDirectoryScanProgressVisible)
        {
            return string.Empty;
        }

        return $"Scanning {EmptyDirectoryScanProgressValue}/{EmptyDirectoryScanProgressMaximum} folders...  |  Current: {EmptyDirectoryScanProgressPath}";
    }

    private string BuildShortcutHotkeyScanProgressSummary()
    {
        if (!IsShortcutHotkeyScanProgressVisible)
        {
            return string.Empty;
        }

        return $"Scanning {ShortcutHotkeyScanProgressValue}/{ShortcutHotkeyScanProgressMaximum} folders...  |  .lnk files checked: {ShortcutHotkeyScannedShortcutCount}";
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
            ? $"Current pointer feel: {currentText}"
            : $"Current pointer feel: {currentText}  |  Picked: {BuildMouseSensitivityLevelText(SelectedMouseSensitivityLevel)}";
    }

    private string BuildMouseSensitivitySelectionGuidance()
    {
        var level = Math.Clamp(SelectedMouseSensitivityLevel, 1, 20);

        var rangeGuidance = level switch
        {
            <= 4 => "Very slow. Best if the pointer feels jumpy and you want maximum control.",
            <= 8 => "Slow. Good if you want steadier cursor movement.",
            <= 12 => "Balanced. This is the easiest starting range for most people.",
            <= 16 => "Fast. Good if you want to move across the screen with less hand movement.",
            _ => "Very fast. Best only if you like an extremely quick cursor.",
        };

        return $"Selected feel: {BuildMouseSensitivityLevelText(level)}. Tip: move 1-2 steps at a time. {rangeGuidance}";
    }

    private static string BuildMouseSensitivityLevelText(int level)
    {
        var normalizedLevel = Math.Clamp(level, 1, 20);
        var feelLabel = normalizedLevel switch
        {
            <= 4 => "Very Slow",
            <= 8 => "Slow",
            <= 12 => "Balanced",
            <= 16 => "Fast",
            _ => "Very Fast",
        };

        return normalizedLevel == 10
            ? $"{feelLabel} ({normalizedLevel}/20, Windows middle)"
            : $"{feelLabel} ({normalizedLevel}/20)";
    }

    private string BuildDisplayRefreshSummary()
    {
        var total = DisplayRefreshRecommendations.Count;
        var needsChangeCount = DisplayRefreshRecommendations.Count(item => item.NeedsChange);
        if (total == 0)
        {
            return "No displays checked yet.";
        }

        return needsChangeCount == 0
            ? $"{total} display{(total == 1 ? string.Empty : "s")} found — all running at their best rate"
            : $"{total} display{(total == 1 ? string.Empty : "s")} found — {needsChangeCount} can run faster";
    }

    private string BuildHardwareGraphicsSummary() =>
        $"{HardwareGraphicsAdapters.Count} graphics adapter{(HardwareGraphicsAdapters.Count == 1 ? string.Empty : "s")}";

    private string BuildHardwareStorageSummary() =>
        $"{HardwareStorageDrives.Count} storage drive{(HardwareStorageDrives.Count == 1 ? string.Empty : "s")}";

    private string BuildHardwarePartitionSummary() =>
        HardwareStoragePartitions.Count == 0
            ? "No partitions detected"
            : $"{HardwareStoragePartitions.Count} partition{(HardwareStoragePartitions.Count == 1 ? string.Empty : "s")}";

    private string BuildHardwareSensorSummary() =>
        HardwareSensors.Count == 0
            ? "Sensors / Temps / Fans: Windows did not expose live telemetry"
            : $"{HardwareSensors.Count} sensor reading{(HardwareSensors.Count == 1 ? string.Empty : "s")}";

    private string BuildHardwarePciSummary() =>
        HardwarePciDevices.Count == 0
            ? "No PCI/PCIe devices detected"
            : $"{HardwarePciDevices.Count} PCI/PCIe device{(HardwarePciDevices.Count == 1 ? string.Empty : "s")}";

    private string BuildHardwareRaidSummary() =>
        HardwareRaidDetails.Count == 0
            ? "RAID / Storage Spaces: none detected"
            : $"{HardwareRaidDetails.Count} RAID/storage detail{(HardwareRaidDetails.Count == 1 ? string.Empty : "s")}";

    private string BuildHardwareCheckClipboardText()
    {
        var builder = new StringBuilder();
        builder.AppendLine("MultiTool Hardware Check");
        builder.AppendLine($"Captured: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        builder.AppendLine();
        builder.AppendLine("System");
        builder.AppendLine($"- Overview: {HardwareCheckSystemSummary}");
        builder.AppendLine($"- Operating System: {HardwareCheckOperatingSystemSummary}");
        builder.AppendLine($"- Processor: {HardwareCheckProcessorSummary}");
        builder.AppendLine($"- Memory: {HardwareCheckMemorySummary}");
        builder.AppendLine($"- Motherboard: {HardwareCheckMotherboardSummary}");
        builder.AppendLine($"- BIOS: {HardwareCheckBiosSummary}");

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
        builder.AppendLine("Graphics");
        builder.AppendLine($"- Summary: {HardwareGraphicsSummary}");

        foreach (var adapter in HardwareGraphicsAdapters)
        {
            builder.AppendLine($"- {adapter.Name}");
            builder.AppendLine($"  Driver: {adapter.DriverVersion}");
            builder.AppendLine($"  Memory: {adapter.AdapterMemory}");
        }
    }

    private void AppendStorageSection(StringBuilder builder)
    {
        builder.AppendLine();
        builder.AppendLine("Storage Drives");
        builder.AppendLine($"- Summary: {HardwareStorageSummary}");

        foreach (var drive in HardwareStorageDrives)
        {
            builder.AppendLine($"- {drive.Model}");
            builder.AppendLine($"  Size: {drive.Size}");
            builder.AppendLine($"  Interface: {drive.InterfaceType}");
            builder.AppendLine($"  Media: {drive.MediaType}");
            builder.AppendLine($"  Health: {drive.HealthStatus}");
            builder.AppendLine($"  SMART: {drive.SmartStatus}");
            builder.AppendLine($"  Firmware: {drive.FirmwareVersion}");
            builder.AppendLine($"  Serial: {drive.SerialNumber}");

            if (!string.IsNullOrWhiteSpace(drive.Notes))
            {
                builder.AppendLine($"  Notes: {drive.Notes}");
            }
        }
    }

    private void AppendPartitionSection(StringBuilder builder)
    {
        builder.AppendLine();
        builder.AppendLine("Partitions");
        builder.AppendLine($"- Summary: {HardwarePartitionSummary}");

        foreach (var partition in HardwareStoragePartitions)
        {
            builder.AppendLine($"- {partition.PartitionName}");
            builder.AppendLine($"  Disk: {partition.DiskName}");
            builder.AppendLine($"  Size: {partition.Size}");
            builder.AppendLine($"  Type: {partition.Type}");
            builder.AppendLine($"  Volume: {partition.Volume}");
            builder.AppendLine($"  File System: {partition.FileSystem}");
            builder.AppendLine($"  Free Space: {partition.FreeSpace}");
            builder.AppendLine($"  Status: {partition.Status}");
        }
    }

    private void AppendSensorSection(StringBuilder builder)
    {
        builder.AppendLine();
        builder.AppendLine("Sensors");
        builder.AppendLine($"- Summary: {HardwareSensorSummary}");

        foreach (var sensor in HardwareSensors)
        {
            builder.AppendLine($"- {sensor.Name}");
            builder.AppendLine($"  Category: {sensor.Category}");
            builder.AppendLine($"  Reading: {sensor.CurrentValue}");
            builder.AppendLine($"  Source: {sensor.Source}");
            builder.AppendLine($"  Status: {sensor.Status}");
        }
    }

    private void AppendPciSection(StringBuilder builder)
    {
        builder.AppendLine();
        builder.AppendLine("PCI / PCIe Devices");
        builder.AppendLine($"- Summary: {HardwarePciSummary}");

        foreach (var device in HardwarePciDevices)
        {
            builder.AppendLine($"- {device.Name}");
            builder.AppendLine($"  Class: {device.DeviceClass}");
            builder.AppendLine($"  Manufacturer: {device.Manufacturer}");
            builder.AppendLine($"  Location: {device.Location}");
            builder.AppendLine($"  Status: {device.Status}");
        }
    }

    private void AppendRaidSection(StringBuilder builder)
    {
        builder.AppendLine();
        builder.AppendLine("RAID / Storage Spaces");
        builder.AppendLine($"- Summary: {HardwareRaidSummary}");

        foreach (var detail in HardwareRaidDetails)
        {
            builder.AppendLine($"- {detail.Name}");
            builder.AppendLine($"  Type: {detail.Type}");
            builder.AppendLine($"  Status: {detail.Status}");
            builder.AppendLine($"  Details: {detail.Details}");
            builder.AppendLine($"  Source: {detail.Source}");
        }
    }

    private string BuildDriverHardwareSummary() =>
        $"{DriverHardwareInventory.Count} detected component{(DriverHardwareInventory.Count == 1 ? string.Empty : "s")}";

    private string BuildDriverUpdateSelectionSummary()
    {
        var selectedCount = DriverUpdateCandidates.Count(item => item.IsSelected);
        var recommendedCount = DriverUpdateCandidates.Count(item => !item.IsOptional);
        var optionalCount = DriverUpdateCandidates.Count - recommendedCount;
        return $"{selectedCount} selected  |  {recommendedCount} recommended, {optionalCount} optional";
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
