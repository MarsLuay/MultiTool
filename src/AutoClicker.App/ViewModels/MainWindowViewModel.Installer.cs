using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using AutoClicker.App.Models;
using AutoClicker.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AutoClicker.App.ViewModels;

public partial class MainWindowViewModel
{
    private const string FirefoxPackageId = "Mozilla.Firefox";

    public ObservableCollection<InstallerPackageItem> InstallerPackages { get; } = [];

    public ObservableCollection<InstallerPackageItem> CleanupPackages { get; } = [];

    public ObservableCollection<string> InstallerLogEntries { get; } = [];

    public ICollectionView InstallerPackagesView { get; private set; } = null!;

    public bool HasSelectedInstallerPackages => InstallerPackages.Any(item => item.IsSelected);

    public bool HasSelectedCleanupPackages => CleanupPackages.Any(item => item.IsSelected);

    public string InstallerSelectionSummary => BuildInstallerSelectionSummary();

    public string InstallerUpdateSummary => BuildInstallerUpdateSummary();

    public string CleanupSelectionSummary => BuildCleanupSelectionSummary();

    [ObservableProperty]
    private string installerSearchText = string.Empty;

    [ObservableProperty]
    private string installerStatusMessage = "Preparing the installer catalog...";

    [ObservableProperty]
    private string installerEnvironmentMessage = "The installer tab uses winget for silent installs and updates.";

    [ObservableProperty]
    private string installerAppUpdateSummary = "MultiTool release checks run with Check All Updates.";

    [ObservableProperty]
    private bool isInstallerBusy;

    [ObservableProperty]
    private bool isWingetAvailable;

    [ObservableProperty]
    private string cleanupStatusMessage = "Cleanup options are loading...";

    private bool hasCompletedInstallerStatusCheck;

    private void InitializeInstallerState()
    {
        foreach (var package in installerService.GetCatalog().OrderBy(item => item.Category).ThenBy(item => item.DisplayName))
        {
            var packageItem = new InstallerPackageItem(package)
            {
                StatusText = "Checking status...",
            };

            if (string.Equals(package.PackageId, FirefoxPackageId, StringComparison.OrdinalIgnoreCase))
            {
                foreach (var option in firefoxExtensionService.GetCatalog())
                {
                    var optionItem = new InstallerPackageOptionItem(option);
                    optionItem.PropertyChanged += InstallerPackageOptionItem_OnPropertyChanged;
                    packageItem.Options.Add(optionItem);
                }
            }

            packageItem.PropertyChanged += InstallerPackageItem_OnPropertyChanged;
            InstallerPackages.Add(packageItem);
        }

        foreach (var package in installerService.GetCleanupCatalog().OrderBy(item => item.DisplayName))
        {
            var packageItem = new InstallerPackageItem(package)
            {
                StatusText = "Checking status...",
            };

            packageItem.PropertyChanged += InstallerPackageItem_OnPropertyChanged;
            CleanupPackages.Add(packageItem);
        }

        InstallerPackagesView = CollectionViewSource.GetDefaultView(InstallerPackages);
        InstallerPackagesView.Filter = FilterInstallerPackage;
        OnPropertyChanged(nameof(InstallerSelectionSummary));
        OnPropertyChanged(nameof(InstallerUpdateSummary));
        OnPropertyChanged(nameof(CleanupSelectionSummary));
    }

    private void ApplyInstallerSettings(InstallerSettings? settings)
    {
        var selectedIds = settings?.SelectedPackageIds is { Count: > 0 }
            ? new HashSet<string>(settings.SelectedPackageIds, StringComparer.OrdinalIgnoreCase)
            : [];
        var selectedCleanupIds = settings?.SelectedCleanupPackageIds is { Count: > 0 }
            ? new HashSet<string>(settings.SelectedCleanupPackageIds, StringComparer.OrdinalIgnoreCase)
            : [];
        var selectedOptionIdsByPackage = settings?.PackageOptions is { Count: > 0 }
            ? settings.PackageOptions
                .Where(package => !string.IsNullOrWhiteSpace(package.PackageId))
                .GroupBy(package => package.PackageId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                group => group.Key,
                group => group
                    .SelectMany(package => package.SelectedOptionIds)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase),
                StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var package in InstallerPackages)
        {
            package.IsSelected = selectedIds.Contains(package.PackageId);

            var selectedOptionIds = selectedOptionIdsByPackage.TryGetValue(package.PackageId, out var packageOptions)
                ? packageOptions
                : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var option in package.Options)
            {
                option.IsSelected = selectedOptionIds.Contains(option.OptionId);
            }
        }

        foreach (var package in CleanupPackages)
        {
            package.IsSelected = selectedCleanupIds.Contains(package.PackageId);
        }

        OnPropertyChanged(nameof(HasSelectedInstallerPackages));
        OnPropertyChanged(nameof(InstallerSelectionSummary));
        OnPropertyChanged(nameof(HasSelectedCleanupPackages));
        OnPropertyChanged(nameof(CleanupSelectionSummary));
    }

    private InstallerSettings BuildInstallerSettings() =>
        new()
        {
            SelectedPackageIds =
            [
                .. InstallerPackages
                    .Where(item => item.IsSelected)
                    .Select(item => item.PackageId),
            ],
            SelectedCleanupPackageIds =
            [
                .. CleanupPackages
                    .Where(item => item.IsSelected)
                    .Select(item => item.PackageId),
            ],
            PackageOptions =
            [
                .. InstallerPackages
                    .Where(item => item.Options.Any(option => option.IsSelected))
                    .Select(
                        item => new InstallerPackageOptionSelection
                        {
                            PackageId = item.PackageId,
                            SelectedOptionIds =
                            [
                                .. item.Options
                                    .Where(option => option.IsSelected)
                                    .Select(option => option.OptionId),
                            ],
                        }),
            ],
        };

    private void StartInstallerInitialization()
    {
        _ = InitializeInstallerAsync();
    }

    private async Task InitializeInstallerAsync()
    {
        try
        {
            await RefreshInstallerStatusCoreAsync(addLogEntry: false).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            InstallerStatusMessage = $"Installer setup failed: {ex.Message}";
            AddInstallerLog(InstallerStatusMessage);
        }
    }

    private void InstallerPackageItem_OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(InstallerPackageItem.IsSelected)
            or nameof(InstallerPackageItem.IsInstalled)
            or nameof(InstallerPackageItem.HasUpdateAvailable))
        {
            OnPropertyChanged(nameof(HasSelectedInstallerPackages));
            OnPropertyChanged(nameof(InstallerSelectionSummary));
            OnPropertyChanged(nameof(InstallerUpdateSummary));
            OnPropertyChanged(nameof(HasSelectedCleanupPackages));
            OnPropertyChanged(nameof(CleanupSelectionSummary));
            RefreshInstallerCommandStates();
        }

        if (e.PropertyName == nameof(InstallerPackageItem.IsSelected))
        {
            ScheduleSettingsAutoSave();
        }
    }

    private void InstallerPackageOptionItem_OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(InstallerPackageOptionItem.IsSelected))
        {
            ScheduleSettingsAutoSave();
        }
    }

    partial void OnInstallerSearchTextChanged(string value)
    {
        InstallerPackagesView?.Refresh();
    }

    partial void OnIsInstallerBusyChanged(bool value)
    {
        RefreshInstallerCommandStates();
    }

    partial void OnIsWingetAvailableChanged(bool value)
    {
        RefreshInstallerCommandStates();
    }

    [RelayCommand(CanExecute = nameof(CanRefreshInstallerStatus))]
    private async Task RefreshInstallerStatusAsync()
    {
        await RefreshInstallerStatusCoreAsync(addLogEntry: true);
    }

    private bool CanRefreshInstallerStatus => !IsInstallerBusy;

    [RelayCommand(CanExecute = nameof(CanRefreshInstallerStatus))]
    private async Task CheckAllInstallerUpdatesAsync()
    {
        InstallerStatusMessage = $"Checking {InstallerPackages.Count} tracked app{(InstallerPackages.Count == 1 ? string.Empty : "s")} for updates...";
        await RefreshInstallerStatusCoreAsync(addLogEntry: true, addDetailedUpdateLog: true);
        await RefreshMultiToolUpdateStatusAsync(addLogEntry: true).ConfigureAwait(true);
    }

    [RelayCommand]
    private void SelectRecommendedInstallerPackages()
    {
        foreach (var package in InstallerPackages)
        {
            package.IsSelected = package.IsRecommended;
        }

        InstallerStatusMessage = "Selected the recommended starter apps.";
        AddInstallerLog(InstallerStatusMessage);
    }

    [RelayCommand]
    private void SelectDeveloperInstallerPackages()
    {
        foreach (var package in InstallerPackages)
        {
            package.IsSelected = package.IsDeveloperTool;
        }

        InstallerStatusMessage = "Selected the developer stack.";
        AddInstallerLog(InstallerStatusMessage);
    }

    private bool CanClearInstallerSelection => InstallerPackages.Any(item => item.IsSelected);

    [RelayCommand(CanExecute = nameof(CanClearInstallerSelection))]
    private void ClearInstallerSelection()
    {
        foreach (var package in InstallerPackages.Where(item => item.IsSelected))
        {
            package.IsSelected = false;
        }

        InstallerStatusMessage = "Cleared the installer selection.";
        AddInstallerLog(InstallerStatusMessage);
    }

    [RelayCommand]
    private void SelectRecommendedCleanupPackages()
    {
        foreach (var package in CleanupPackages)
        {
            package.IsSelected = package.IsRecommended;
        }

        CleanupStatusMessage = "Selected the recommended cleanup apps.";
        AddInstallerLog(CleanupStatusMessage);
    }

    private bool CanClearCleanupSelection => CleanupPackages.Any(item => item.IsSelected);

    [RelayCommand(CanExecute = nameof(CanClearCleanupSelection))]
    private void ClearCleanupSelection()
    {
        foreach (var package in CleanupPackages.Where(item => item.IsSelected))
        {
            package.IsSelected = false;
        }

        CleanupStatusMessage = "Cleared the cleanup selection.";
        AddInstallerLog(CleanupStatusMessage);
    }

    private bool CanInstallSelectedPackages =>
        !IsInstallerBusy
        && InstallerPackages.Any(item => item.IsSelected && (IsWingetAvailable || item.UsesGuidedInstall));

    [RelayCommand(CanExecute = nameof(CanInstallSelectedPackages))]
    private async Task InstallSelectedPackagesAsync()
    {
        await RunInstallerBatchAsync(
            actionLabel: "install",
            changedLabel: "installed",
            packageIds => installerService.InstallPackagesAsync(packageIds),
            requireInstalledSelection: false,
            syncFirefoxExtensions: true);
    }

    private bool CanUpgradeSelectedPackages =>
        !IsInstallerBusy
        && InstallerPackages.Any(
            item => item.IsSelected
                && ((IsWingetAvailable && item.IsInstalled) || item.UsesGuidedUpdate));

    [RelayCommand(CanExecute = nameof(CanUpgradeSelectedPackages))]
    private async Task UpgradeSelectedPackagesAsync()
    {
        await RunInstallerBatchAsync(
            actionLabel: "update",
            changedLabel: "updated",
            packageIds => installerService.UpgradePackagesAsync(packageIds),
            requireInstalledSelection: true,
            syncFirefoxExtensions: false);
    }

    private bool CanUninstallSelectedCleanupPackages => IsWingetAvailable && !IsInstallerBusy && CleanupPackages.Any(item => item.IsSelected && item.IsInstalled);

    [RelayCommand(CanExecute = nameof(CanUninstallSelectedCleanupPackages))]
    private async Task UninstallSelectedCleanupPackagesAsync()
    {
        var selectedPackages = CleanupPackages
            .Where(item => item.IsSelected && item.IsInstalled)
            .ToArray();

        if (selectedPackages.Length == 0)
        {
            CleanupStatusMessage = "Select at least one installed cleanup app first.";
            AddInstallerLog(CleanupStatusMessage);
            return;
        }

        IsInstallerBusy = true;
        CleanupStatusMessage = $"Removing {selectedPackages.Length} app{(selectedPackages.Length == 1 ? string.Empty : "s")}...";
        AddInstallerLog(CleanupStatusMessage);

        try
        {
            var results = await installerService.UninstallPackagesAsync(selectedPackages.Select(item => item.PackageId).ToArray());
            foreach (var result in results)
            {
                AddInstallerLog($"{result.DisplayName}: {result.Message}");
            }

            var removedCount = results.Count(result => result.Succeeded && result.Changed);
            var missingCount = results.Count(result => result.Succeeded && !result.Changed);
            var failedCount = results.Count(result => !result.Succeeded);

            CleanupStatusMessage = $"{removedCount} removed, {missingCount} already gone, {failedCount} failed.";
            await RefreshInstallerStatusCoreAsync(addLogEntry: false);
        }
        catch (Exception ex)
        {
            CleanupStatusMessage = $"Cleanup failed: {ex.Message}";
            AddInstallerLog(CleanupStatusMessage);
        }
        finally
        {
            IsInstallerBusy = false;
        }
    }

    private async Task RunInstallerBatchAsync(
        string actionLabel,
        string changedLabel,
        Func<IReadOnlyList<string>, Task<IReadOnlyList<InstallerOperationResult>>> runBatchAsync,
        bool requireInstalledSelection,
        bool syncFirefoxExtensions)
    {
        var selectedPackages = InstallerPackages
            .Where(
                item => item.IsSelected
                    && (IsWingetAvailable || item.UsesGuidedInstall || item.UsesGuidedUpdate)
                    && (!requireInstalledSelection || item.IsInstalled || item.UsesGuidedUpdate))
            .ToArray();

        if (selectedPackages.Length == 0)
        {
            InstallerStatusMessage = requireInstalledSelection
                ? "Select at least one installed or guided-update app first."
                : "Select at least one app first.";
            AddInstallerLog(InstallerStatusMessage);
            return;
        }

        IsInstallerBusy = true;
        InstallerStatusMessage = $"Running {actionLabel} for {selectedPackages.Length} app{(selectedPackages.Length == 1 ? string.Empty : "s")}...";
        AddInstallerLog(InstallerStatusMessage);

        try
        {
            var results = await runBatchAsync(selectedPackages.Select(item => item.PackageId).ToArray());
            foreach (var result in results)
            {
                AddInstallerLog($"{result.DisplayName}: {result.Message}");
            }

            var firefoxExtensionResults = syncFirefoxExtensions
                ? await SyncFirefoxExtensionsAsync(selectedPackages, results)
                : [];
            foreach (var result in firefoxExtensionResults)
            {
                AddInstallerLog($"{result.DisplayName}: {result.Message}");
            }

            var changedCount = results.Count(result => result.Succeeded && result.Changed);
            var unchangedCount = results.Count(result => result.Succeeded && !result.Changed);
            var failedCount = results.Count(result => !result.Succeeded);
            var firefoxSummarySuffix = BuildSupplementalResultSummary(firefoxExtensionResults, "Firefox add-ons");

            InstallerStatusMessage = $"{changedCount} {changedLabel}, {unchangedCount} already current, {failedCount} failed.{firefoxSummarySuffix}";
            await RefreshInstallerStatusCoreAsync(addLogEntry: false);
        }
        catch (Exception ex)
        {
            InstallerStatusMessage = $"Installer {actionLabel} failed: {ex.Message}";
            AddInstallerLog(InstallerStatusMessage);
        }
        finally
        {
            IsInstallerBusy = false;
        }
    }

    private async Task RefreshInstallerStatusCoreAsync(bool addLogEntry, bool addDetailedUpdateLog = false)
    {
        IsInstallerBusy = true;

        try
        {
            var environment = await installerService.GetEnvironmentInfoAsync();
            IsWingetAvailable = environment.IsAvailable;
            InstallerEnvironmentMessage = environment.Message;
            hasCompletedInstallerStatusCheck = true;

            if (!environment.IsAvailable)
            {
                foreach (var package in InstallerPackages)
                {
                    package.IsInstalled = false;
                    package.HasUpdateAvailable = false;
                    package.StatusText = package.UsesGuidedInstall
                        ? "Guided install"
                        : "winget unavailable";
                }

                foreach (var package in CleanupPackages)
                {
                    package.IsInstalled = false;
                    package.HasUpdateAvailable = false;
                    package.StatusText = "winget unavailable";
                }

                var guidedCount = InstallerPackages.Count(item => item.UsesGuidedInstall);
                InstallerStatusMessage = guidedCount > 0
                    ? $"{environment.Message} {guidedCount} guided app{(guidedCount == 1 ? string.Empty : "s")} can still open official setup pages."
                    : environment.Message;
                CleanupStatusMessage = environment.Message;
                if (addLogEntry)
                {
                    AddInstallerLog(InstallerStatusMessage);
                }

                return;
            }

            var statuses = await installerService.GetPackageStatusesAsync(
                InstallerPackages
                    .Select(item => item.PackageId)
                    .Concat(CleanupPackages.Select(item => item.PackageId))
                    .Distinct(StringComparer.OrdinalIgnoreCase));
            var statusLookup = statuses.ToDictionary(item => item.PackageId, StringComparer.OrdinalIgnoreCase);

            ApplyStatuses(InstallerPackages, statusLookup);
            ApplyStatuses(CleanupPackages, statusLookup);

            var installedCount = InstallerPackages.Count(item => item.IsInstalled);
            var updateCount = InstallerPackages.Count(item => item.HasUpdateAvailable);
            InstallerStatusMessage = $"{installedCount} installed, {updateCount} update{(updateCount == 1 ? string.Empty : "s")} available.";
            CleanupStatusMessage = $"{CleanupPackages.Count(item => item.IsInstalled)} removable app{(CleanupPackages.Count(item => item.IsInstalled) == 1 ? string.Empty : "s")} currently installed.";

            if (addLogEntry)
            {
                AddInstallerLog(InstallerStatusMessage);
                if (addDetailedUpdateLog)
                {
                    AddInstallerLog(BuildDetailedUpdateLogMessage());
                }
            }
        }
        catch (Exception ex)
        {
            InstallerStatusMessage = $"Unable to refresh installer status: {ex.Message}";
            hasCompletedInstallerStatusCheck = true;

            if (addLogEntry)
            {
                AddInstallerLog(InstallerStatusMessage);
            }
        }
        finally
        {
            OnPropertyChanged(nameof(InstallerSelectionSummary));
            OnPropertyChanged(nameof(InstallerUpdateSummary));
            OnPropertyChanged(nameof(CleanupSelectionSummary));
            IsInstallerBusy = false;
        }
    }

    private void RefreshInstallerCommandStates()
    {
        RefreshInstallerStatusCommand.NotifyCanExecuteChanged();
        CheckAllInstallerUpdatesCommand.NotifyCanExecuteChanged();
        ClearInstallerSelectionCommand.NotifyCanExecuteChanged();
        ClearCleanupSelectionCommand.NotifyCanExecuteChanged();
        InstallSelectedPackagesCommand.NotifyCanExecuteChanged();
        UpgradeSelectedPackagesCommand.NotifyCanExecuteChanged();
        UninstallSelectedCleanupPackagesCommand.NotifyCanExecuteChanged();
    }

    private bool FilterInstallerPackage(object item)
    {
        if (item is not InstallerPackageItem package)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(InstallerSearchText))
        {
            return true;
        }

        return package.SearchText.Contains(InstallerSearchText.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private string BuildInstallerSelectionSummary()
    {
        var selectedCount = InstallerPackages.Count(item => item.IsSelected);
        var installedCount = InstallerPackages.Count(item => item.IsInstalled);
        var updateCount = InstallerPackages.Count(item => item.HasUpdateAvailable);
        return $"{selectedCount} selected  |  {installedCount} installed  |  {updateCount} updates ready";
    }

    private string BuildInstallerUpdateSummary()
    {
        if (!hasCompletedInstallerStatusCheck)
        {
            return "Use Check All Updates to scan every tracked app.";
        }

        if (!IsWingetAvailable)
        {
            return "Update checks are unavailable until winget is available.";
        }

        var updates = InstallerPackages
            .Where(item => item.HasUpdateAvailable)
            .OrderBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase)
            .Select(item => item.DisplayName)
            .ToArray();
        var guidedCount = InstallerPackages.Count(item => item.UsesGuidedUpdate);

        return updates.Length switch
        {
            0 when guidedCount > 0 => $"No winget-tracked updates found. {guidedCount} guided app{(guidedCount == 1 ? string.Empty : "s")} use manual update pages.",
            0 => "No updates found across the tracked app list.",
            <= 5 when guidedCount > 0 => $"Updates ready: {string.Join(", ", updates)}. {guidedCount} guided app{(guidedCount == 1 ? string.Empty : "s")} use manual update pages.",
            <= 5 => $"Updates ready: {string.Join(", ", updates)}.",
            _ when guidedCount > 0 => $"Updates ready: {string.Join(", ", updates[..5])}, +{updates.Length - 5} more. {guidedCount} guided app{(guidedCount == 1 ? string.Empty : "s")} use manual update pages.",
            _ => $"Updates ready: {string.Join(", ", updates[..5])}, +{updates.Length - 5} more.",
        };
    }

    private string BuildCleanupSelectionSummary()
    {
        var selectedCount = CleanupPackages.Count(item => item.IsSelected);
        var installedCount = CleanupPackages.Count(item => item.IsInstalled);
        return $"{selectedCount} selected  |  {installedCount} currently installed";
    }

    private string BuildDetailedUpdateLogMessage()
    {
        var updates = InstallerPackages
            .Where(item => item.HasUpdateAvailable)
            .OrderBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase)
            .Select(item => item.DisplayName)
            .ToArray();
        var guidedCount = InstallerPackages.Count(item => item.UsesGuidedUpdate);

        return updates.Length switch
        {
            0 when guidedCount > 0 => $"No winget-tracked updates found. {guidedCount} guided app{(guidedCount == 1 ? string.Empty : "s")} use manual update pages.",
            0 => "No updates found across the tracked app list.",
            1 when guidedCount > 0 => $"Update ready for {updates[0]}. {guidedCount} guided app{(guidedCount == 1 ? string.Empty : "s")} use manual update pages.",
            1 => $"Update ready for {updates[0]}.",
            _ when guidedCount > 0 => $"Updates ready for {updates.Length} apps: {string.Join(", ", updates)}. {guidedCount} guided app{(guidedCount == 1 ? string.Empty : "s")} use manual update pages.",
            _ => $"Updates ready for {updates.Length} apps: {string.Join(", ", updates)}.",
        };
    }

    private async Task<IReadOnlyList<InstallerOperationResult>> SyncFirefoxExtensionsAsync(
        IReadOnlyList<InstallerPackageItem> selectedPackages,
        IReadOnlyList<InstallerOperationResult> packageResults)
    {
        var firefoxPackage = selectedPackages.FirstOrDefault(
            static item => string.Equals(item.PackageId, FirefoxPackageId, StringComparison.OrdinalIgnoreCase));
        if (firefoxPackage is null)
        {
            return [];
        }

        var firefoxInstallResult = packageResults.FirstOrDefault(
            static result => string.Equals(result.PackageId, FirefoxPackageId, StringComparison.OrdinalIgnoreCase));
        if (firefoxInstallResult is not null && !firefoxInstallResult.Succeeded)
        {
            AddInstallerLog("Firefox add-ons skipped because Firefox did not install cleanly.");
            return [];
        }

        return await firefoxExtensionService.SyncExtensionSelectionsAsync(
            firefoxPackage.Options
                .Where(static option => option.IsSelected)
                .Select(static option => option.OptionId));
    }

    private async Task RefreshMultiToolUpdateStatusAsync(bool addLogEntry)
    {
        try
        {
            var updateInfo = await appUpdateService.CheckForUpdatesAsync().ConfigureAwait(true);
            InstallerAppUpdateSummary = updateInfo.Message;

            if (addLogEntry)
            {
                var logMessage = updateInfo.IsUpdateAvailable && !string.IsNullOrWhiteSpace(updateInfo.ReleaseUrl)
                    ? $"{updateInfo.Message} {updateInfo.ReleaseUrl}"
                    : updateInfo.Message;
                AddInstallerLog(logMessage);
            }
        }
        catch (Exception ex)
        {
            InstallerAppUpdateSummary = $"Unable to check for MultiTool updates: {ex.Message}";
            if (addLogEntry)
            {
                AddInstallerLog(InstallerAppUpdateSummary);
            }
        }
    }

    private static string BuildSupplementalResultSummary(IReadOnlyList<InstallerOperationResult> results, string label)
    {
        if (results.Count == 0)
        {
            return string.Empty;
        }

        var changedCount = results.Count(result => result.Succeeded && result.Changed);
        var unchangedCount = results.Count(result => result.Succeeded && !result.Changed);
        var failedCount = results.Count(result => !result.Succeeded);
        return $" {label}: {changedCount} applied, {unchangedCount} already ready, {failedCount} failed.";
    }

    private static void ApplyStatuses(IEnumerable<InstallerPackageItem> packages, IReadOnlyDictionary<string, InstallerPackageStatus> statusLookup)
    {
        foreach (var package in packages)
        {
            if (!statusLookup.TryGetValue(package.PackageId, out var status))
            {
                package.IsInstalled = false;
                package.HasUpdateAvailable = false;
                package.StatusText = "Status unavailable";
                continue;
            }

            package.IsInstalled = status.IsInstalled;
            package.HasUpdateAvailable = status.HasUpdateAvailable;
            package.StatusText = status.StatusText;
        }
    }

    private void AddInstallerLog(string message)
    {
        InstallerLogEntries.Insert(0, $"{DateTime.Now:HH:mm:ss}  {message}");
        AddActivityLog(message);

        while (InstallerLogEntries.Count > 200)
        {
            InstallerLogEntries.RemoveAt(InstallerLogEntries.Count - 1);
        }
    }
}
