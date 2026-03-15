using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using AutoClicker.App.Models;
using AutoClicker.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AutoClicker.App.ViewModels;

public partial class MainWindowViewModel
{
    private const string Windows11DownloadUrl = "https://www.microsoft.com/software-download/windows11";
    private const string Windows11EeaGuidanceUrl = "https://blogs.windows.com/windows-insider/2023/11/16/previewing-changes-in-windows-to-comply-with-the-digital-markets-act-in-the-european-economic-area/";
    private const string Windows11EeaInstallChecklist = """
        Use Microsoft's official Windows 11 download page.
        Create the USB installer or ISO from that page.
        During Windows setup, choose an EEA country or region.
        Finish setup with that region if you want EEA Windows behavior.
        Microsoft says changing the DMA region later requires resetting the PC.
        """;

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

    public ObservableCollection<DisplayRefreshRecommendationItem> DisplayRefreshRecommendations { get; } = [];

    public ObservableCollection<HardwareDisplayAdapterInfo> HardwareGraphicsAdapters { get; } = [];

    public ObservableCollection<HardwareStorageDriveInfo> HardwareStorageDrives { get; } = [];

    public ObservableCollection<DriverHardwareInfo> DriverHardwareInventory { get; } = [];

    public ObservableCollection<DriverUpdateItem> DriverUpdateCandidates { get; } = [];

    public ObservableCollection<UsefulSiteItem> UsefulSites { get; } = [];

    public ObservableCollection<string> ToolLogEntries { get; } = [];

    public bool HasSelectedDriverUpdates => DriverUpdateCandidates.Any(item => item.IsSelected);

    public bool HasDisplayRefreshRecommendations => DisplayRefreshRecommendations.Count > 0;

    public bool HasSelectedEmptyDirectories => EmptyDirectoryCandidates.Any(item => item.IsSelected);

    public string DisplayRefreshSummary => BuildDisplayRefreshSummary();

    public string HardwareGraphicsSummary => BuildHardwareGraphicsSummary();

    public string HardwareStorageSummary => BuildHardwareStorageSummary();

    public string DriverHardwareSummary => BuildDriverHardwareSummary();

    public string DriverUpdateSelectionSummary => BuildDriverUpdateSelectionSummary();

    public string EmptyDirectorySelectionSummary => BuildEmptyDirectorySelectionSummary();

    public string UsefulSitesToggleText => AreUsefulSitesVisible ? "Hide Useful Sites" : "Useful Sites";

    [ObservableProperty]
    private string emptyDirectoryRootPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    [ObservableProperty]
    private string emptyDirectoryStatusMessage = "Choose a folder tree to scan for empty directories.";

    [ObservableProperty]
    private string displayRefreshStatusMessage = "Check connected displays and set each one to the top refresh rate available at its current resolution.";

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
    private string hardwareCheckStatusMessage = "Scan the PC to review core hardware details.";

    [ObservableProperty]
    private string driverUpdateStatusMessage = "Scan this PC's hardware and check Windows Update for recommended and optional driver updates.";

    [ObservableProperty]
    private bool isToolBusy;

    [ObservableProperty]
    private string darkModeToolStatusMessage = "Apply Windows dark mode preferences for the shell and supported apps.";

    [ObservableProperty]
    private string oneDriveToolStatusMessage = "Check whether OneDrive is present, then remove it with Windows' built-in uninstaller.";

    [ObservableProperty]
    private bool areUsefulSitesVisible;

    [ObservableProperty]
    private string usefulSitesStatusMessage = "Open a curated list of useful sites from inside MultiTool.";

    [ObservableProperty]
    private string windows11EeaInstallStatusMessage = "Open Microsoft's Windows 11 download page, then use an EEA region during setup if you want the EEA Windows behavior.";

    private void InitializeToolsState()
    {
        if (UsefulSites.Count == 0)
        {
            foreach (var site in DefaultUsefulSites)
            {
                UsefulSites.Add(site);
            }
        }

        OnPropertyChanged(nameof(HasDisplayRefreshRecommendations));
        OnPropertyChanged(nameof(DisplayRefreshSummary));
        OnPropertyChanged(nameof(HardwareGraphicsSummary));
        OnPropertyChanged(nameof(HardwareStorageSummary));
        OnPropertyChanged(nameof(HasSelectedDriverUpdates));
        OnPropertyChanged(nameof(DriverHardwareSummary));
        OnPropertyChanged(nameof(DriverUpdateSelectionSummary));
        OnPropertyChanged(nameof(HasSelectedEmptyDirectories));
        OnPropertyChanged(nameof(EmptyDirectorySelectionSummary));
        OnPropertyChanged(nameof(UsefulSitesToggleText));
        RefreshOneDriveStatusCore(addLogEntry: false);
    }

    partial void OnEmptyDirectoryRootPathChanged(string value)
    {
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

    [RelayCommand]
    private void OpenWindows11DownloadPage()
    {
        try
        {
            Process.Start(
                new ProcessStartInfo
                {
                    FileName = Windows11DownloadUrl,
                    UseShellExecute = true,
                });

            Windows11EeaInstallStatusMessage = "Opened Microsoft's official Windows 11 download page.";
            AddToolLog(Windows11EeaInstallStatusMessage);
        }
        catch (Exception ex)
        {
            Windows11EeaInstallStatusMessage = $"Unable to open the Windows 11 download page: {ex.Message}";
            AddToolLog(Windows11EeaInstallStatusMessage);
        }
    }

    [RelayCommand]
    private void OpenWindows11EeaNotes()
    {
        try
        {
            Process.Start(
                new ProcessStartInfo
                {
                    FileName = Windows11EeaGuidanceUrl,
                    UseShellExecute = true,
                });

            Windows11EeaInstallStatusMessage = "Opened Microsoft's EEA Windows setup notes.";
            AddToolLog(Windows11EeaInstallStatusMessage);
        }
        catch (Exception ex)
        {
            Windows11EeaInstallStatusMessage = $"Unable to open the EEA Windows notes: {ex.Message}";
            AddToolLog(Windows11EeaInstallStatusMessage);
        }
    }

    [RelayCommand]
    private void CopyWindows11EeaInstallChecklist()
    {
        try
        {
            clipboardTextService.SetText(Windows11EeaInstallChecklist);
            Windows11EeaInstallStatusMessage = "Copied the Windows 11 EEA install checklist to the clipboard.";
            AddToolLog(Windows11EeaInstallStatusMessage);
        }
        catch (Exception ex)
        {
            Windows11EeaInstallStatusMessage = $"Unable to copy the Windows 11 EEA install checklist: {ex.Message}";
            AddToolLog(Windows11EeaInstallStatusMessage);
        }
    }

    private bool CanApplySystemDarkMode => !IsToolBusy;

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
            var failedCount = results.Count(result => !result.Succeeded);
            var restartCount = results.Count(result => result.RequiresRestart);
            DriverUpdateStatusMessage =
                $"{installedCount} installed, {failedCount} failed. {DriverUpdateCandidates.Count} update{(DriverUpdateCandidates.Count == 1 ? string.Empty : "s")} remain." +
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
        if (addLogEntry)
        {
            AddToolLog(EmptyDirectoryStatusMessage);
        }

        try
        {
            var scanResult = await emptyDirectoryService.FindEmptyDirectoriesAsync(fullRootPath).ConfigureAwait(true);
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
            var warningSuffix = scanResult.Warnings.Count == 0
                ? string.Empty
                : $" Warnings: {scanResult.Warnings.Count}.";

            DriverUpdateStatusMessage = scanResult.Updates.Count == 0
                ? $"Detected {scanResult.Hardware.Count} hardware component{(scanResult.Hardware.Count == 1 ? string.Empty : "s")}. No driver updates are currently available from Windows Update.{warningSuffix}"
                : $"Detected {scanResult.Hardware.Count} hardware component{(scanResult.Hardware.Count == 1 ? string.Empty : "s")}. Found {recommendedCount} recommended and {optionalCount} optional driver update{(scanResult.Updates.Count == 1 ? string.Empty : "s")}.{warningSuffix}";

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
                $"Hardware scan complete. Found {report.GraphicsAdapters.Count} graphics adapter{(report.GraphicsAdapters.Count == 1 ? string.Empty : "s")} and {report.StorageDrives.Count} storage drive{(report.StorageDrives.Count == 1 ? string.Empty : "s")}.{warningSuffix}";

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

    private void RefreshToolCommandStates()
    {
        ScanDisplayRefreshRecommendationsCommand.NotifyCanExecuteChanged();
        ApplyDisplayRefreshRecommendationsCommand.NotifyCanExecuteChanged();
        ScanHardwareCheckCommand.NotifyCanExecuteChanged();
        ScanDriverUpdatesCommand.NotifyCanExecuteChanged();
        SelectRecommendedDriverUpdatesCommand.NotifyCanExecuteChanged();
        SelectAllDriverUpdatesCommand.NotifyCanExecuteChanged();
        ClearDriverUpdateSelectionCommand.NotifyCanExecuteChanged();
        InstallSelectedDriverUpdatesCommand.NotifyCanExecuteChanged();
        RefreshOneDriveStatusCommand.NotifyCanExecuteChanged();
        RemoveOneDriveCommand.NotifyCanExecuteChanged();
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

    private string BuildDisplayRefreshSummary()
    {
        var needsChangeCount = DisplayRefreshRecommendations.Count(item => item.NeedsChange);
        return $"{DisplayRefreshRecommendations.Count} display{(DisplayRefreshRecommendations.Count == 1 ? string.Empty : "s")} found  |  {needsChangeCount} can be improved";
    }

    private string BuildHardwareGraphicsSummary() =>
        $"{HardwareGraphicsAdapters.Count} graphics adapter{(HardwareGraphicsAdapters.Count == 1 ? string.Empty : "s")}";

    private string BuildHardwareStorageSummary() =>
        $"{HardwareStorageDrives.Count} storage drive{(HardwareStorageDrives.Count == 1 ? string.Empty : "s")}";

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
