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
    private const int MaxInstallerOperationHistory = 24;

    private int nextInstallerOperationSequenceNumber = 1;
    private bool isInstallerQueueProcessing;

    public ObservableCollection<InstallerPackageItem> InstallerPackages { get; } = [];

    public ObservableCollection<InstallerPackageItem> CleanupPackages { get; } = [];

    public ObservableCollection<InstallerOperationItem> InstallerOperations { get; } = [];

    public ObservableCollection<string> InstallerLogEntries { get; } = [];

    public ICollectionView InstallerPackagesView { get; private set; } = null!;

    public bool HasSelectedInstallerPackages => InstallerPackages.Any(item => item.IsSelected);

    public bool HasSelectedCleanupPackages => CleanupPackages.Any(item => item.IsSelected);

    public bool HasInstallerOperations => InstallerOperations.Count > 0;

    public string InstallerOperationQueueSummary => BuildInstallerOperationQueueSummary();

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
    private bool isInstallerProgressVisible;

    [ObservableProperty]
    private int installerProgressValue;

    [ObservableProperty]
    private int installerProgressMaximum = 1;

    [ObservableProperty]
    private string installerProgressText = string.Empty;

    [ObservableProperty]
    private bool isWingetAvailable;

    [ObservableProperty]
    private string cleanupStatusMessage = "Cleanup options are loading...";

    private bool hasCompletedInstallerStatusCheck;

    private void InitializeInstallerState()
    {
        InstallerOperations.CollectionChanged += InstallerOperations_OnCollectionChanged;

        foreach (var package in installerService.GetCatalog().OrderBy(item => item.Category).ThenBy(item => item.DisplayName))
        {
            var packageItem = new InstallerPackageItem(package, installerService.GetPackageCapabilities(package.PackageId))
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
            var packageItem = new InstallerPackageItem(package, installerService.GetPackageCapabilities(package.PackageId))
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

    private void InstallerOperations_OnCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(HasInstallerOperations));
        OnPropertyChanged(nameof(InstallerOperationQueueSummary));
        ClearCompletedInstallerOperationsCommand.NotifyCanExecuteChanged();
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
        && InstallerPackages.Any(item => item.IsSelected && (IsWingetAvailable || item.CanInstallWithoutWinget));

    [RelayCommand(CanExecute = nameof(CanInstallSelectedPackages))]
    private async Task InstallSelectedPackagesAsync()
    {
        await QueueInstallerBatchAsync(
            actionLabel: "install",
            action: InstallerPackageAction.Install,
            requireInstalledSelection: false,
            syncFirefoxExtensions: true,
            canRunWithoutWinget: item => item.CanInstallWithoutWinget);
    }

    private bool CanUpgradeSelectedPackages =>
        !IsInstallerBusy
        && InstallerPackages.Any(
            item => item.IsSelected
                && ((IsWingetAvailable && item.IsInstalled) || (item.IsInstalled && item.CanUpdateWithoutWinget)));

    [RelayCommand(CanExecute = nameof(CanUpgradeSelectedPackages))]
    private async Task UpgradeSelectedPackagesAsync()
    {
        await QueueInstallerBatchAsync(
            actionLabel: "update",
            action: InstallerPackageAction.Update,
            requireInstalledSelection: true,
            syncFirefoxExtensions: false,
            canRunWithoutWinget: item => item.IsInstalled && item.CanUpdateWithoutWinget);
    }

    private bool CanUpgradeAllInstalledPackages =>
        !IsInstallerBusy
        && InstallerPackages.Any(item => item.IsInstalled && item.HasUpdateAvailable && (IsWingetAvailable || item.CanUpdateWithoutWinget));

    [RelayCommand(CanExecute = nameof(CanUpgradeAllInstalledPackages))]
    private async Task UpgradeAllInstalledPackagesAsync()
    {
        await RefreshInstallerStatusCoreAsync(addLogEntry: false);
        await QueueInstallerBatchAsync(
            actionLabel: "update",
            action: InstallerPackageAction.Update,
            requireInstalledSelection: true,
            syncFirefoxExtensions: false,
            canRunWithoutWinget: item => item.IsInstalled && item.CanUpdateWithoutWinget,
            packageFilter: item => item.IsInstalled && item.HasUpdateAvailable && (IsWingetAvailable || item.CanUpdateWithoutWinget),
            emptySelectionMessage: "There are no apps with updates ready.");
    }

    private bool CanQueuePrimaryInstallerPackageAction(InstallerPackageItem? package) =>
        !IsInstallerBusy && package?.CanQueuePrimaryAction == true;

    [RelayCommand(CanExecute = nameof(CanQueuePrimaryInstallerPackageAction))]
    private async Task QueuePrimaryInstallerPackageActionAsync(InstallerPackageItem? package)
    {
        if (package is null)
        {
            return;
        }

        var action = package.IsInstalled ? InstallerPackageAction.Update : InstallerPackageAction.Install;
        await QueueInstallerOperationsAsync(
            [package],
            action,
            syncFirefoxExtensions: !package.IsInstalled && string.Equals(package.PackageId, FirefoxPackageId, StringComparison.OrdinalIgnoreCase),
            sourceLabel: package.DisplayName,
            addedLabel: action == InstallerPackageAction.Install ? "Queued install." : "Queued update.").ConfigureAwait(true);
    }

    private bool CanQueueInteractiveInstallerPackageAction(InstallerPackageItem? package) =>
        !IsInstallerBusy && package?.CanQueueInteractiveAction == true;

    [RelayCommand(CanExecute = nameof(CanQueueInteractiveInstallerPackageAction))]
    private async Task QueueInteractiveInstallerPackageActionAsync(InstallerPackageItem? package)
    {
        if (package is null)
        {
            return;
        }

        var action = package.IsInstalled ? InstallerPackageAction.UpdateInteractive : InstallerPackageAction.InstallInteractive;
        await QueueInstallerOperationsAsync(
            [package],
            action,
            syncFirefoxExtensions: false,
            sourceLabel: package.DisplayName,
            addedLabel: action == InstallerPackageAction.InstallInteractive
                ? "Queued interactive install."
                : "Queued interactive update.").ConfigureAwait(true);
    }

    private bool CanQueueReinstallInstallerPackageAction(InstallerPackageItem? package) =>
        !IsInstallerBusy && package?.CanQueueReinstallAction == true;

    [RelayCommand(CanExecute = nameof(CanQueueReinstallInstallerPackageAction))]
    private async Task QueueReinstallInstallerPackageActionAsync(InstallerPackageItem? package)
    {
        if (package is null)
        {
            return;
        }

        await QueueInstallerOperationsAsync(
            [package],
            InstallerPackageAction.Reinstall,
            syncFirefoxExtensions: false,
            sourceLabel: package.DisplayName,
            addedLabel: "Queued reinstall.").ConfigureAwait(true);
    }

    [RelayCommand]
    private void OpenInstallerPackagePage(InstallerPackageItem? package)
    {
        if (package is null)
        {
            return;
        }

        var targetUrl = package.IsInstalled && !string.IsNullOrWhiteSpace(package.Package.UpdateUrl)
            ? package.Package.UpdateUrl
            : package.Package.InstallUrl ?? package.Package.UpdateUrl;
        if (string.IsNullOrWhiteSpace(targetUrl))
        {
            InstallerStatusMessage = $"{package.DisplayName} does not have an official page linked yet.";
            AddInstallerLog(InstallerStatusMessage);
            return;
        }

        try
        {
            var launchResult = browserLauncherService.OpenUrl(targetUrl);
            InstallerStatusMessage = $"Opened {package.DisplayName}'s official page in {launchResult.BrowserDisplayName}.";
            AddInstallerLog($"{package.DisplayName}: opened {targetUrl} in {launchResult.BrowserDisplayName}.");
        }
        catch (Exception ex)
        {
            InstallerStatusMessage = $"Unable to open {package.DisplayName}'s official page: {ex.Message}";
            AddInstallerLog(InstallerStatusMessage);
        }
    }

    private bool CanClearCompletedInstallerOperations => InstallerOperations.Any(item => item.IsCompleted);

    [RelayCommand(CanExecute = nameof(CanClearCompletedInstallerOperations))]
    private void ClearCompletedInstallerOperations()
    {
        for (var index = InstallerOperations.Count - 1; index >= 0; index--)
        {
            if (InstallerOperations[index].IsCompleted)
            {
                InstallerOperations.RemoveAt(index);
            }
        }
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

    private async Task QueueInstallerBatchAsync(
        string actionLabel,
        InstallerPackageAction action,
        bool requireInstalledSelection,
        bool syncFirefoxExtensions,
        Func<InstallerPackageItem, bool>? canRunWithoutWinget = null,
        Func<InstallerPackageItem, bool>? packageFilter = null,
        string? emptySelectionMessage = null)
    {
        canRunWithoutWinget ??= item => item.CanInstallWithoutWinget || item.CanUpdateWithoutWinget;
        packageFilter ??= item => item.IsSelected;
        var selectedPackages = InstallerPackages
            .Where(
                item => packageFilter(item)
                    && (IsWingetAvailable || canRunWithoutWinget(item))
                    && (!requireInstalledSelection || item.IsInstalled))
            .ToArray();

        if (selectedPackages.Length == 0)
        {
            InstallerStatusMessage = emptySelectionMessage
                ?? (requireInstalledSelection
                    ? "There are no installed apps ready to update."
                    : "Select at least one app first.");
            AddInstallerLog(InstallerStatusMessage);
            return;
        }

        await QueueInstallerOperationsAsync(
            selectedPackages,
            action,
            syncFirefoxExtensions,
            "Installer queue",
            BuildInstallerQueuedBatchMessage(actionLabel, selectedPackages.Length)).ConfigureAwait(true);
    }

    private async Task QueueInstallerOperationsAsync(
        IReadOnlyList<InstallerPackageItem> packages,
        InstallerPackageAction action,
        bool syncFirefoxExtensions,
        string sourceLabel,
        string addedLabel)
    {
        if (packages.Count == 0)
        {
            return;
        }

        var addedCount = 0;
        var duplicateCount = 0;

        foreach (var package in packages)
        {
            if (InstallerOperations.Any(item => !item.IsCompleted && string.Equals(item.DeduplicationKey, BuildInstallerOperationDeduplicationKey(package.PackageId, action), StringComparison.OrdinalIgnoreCase)))
            {
                duplicateCount++;
                continue;
            }

            var operation = new InstallerOperationItem(
                nextInstallerOperationSequenceNumber++,
                package,
                action,
                syncFirefoxExtensions && string.Equals(package.PackageId, FirefoxPackageId, StringComparison.OrdinalIgnoreCase));
            InstallerOperations.Add(operation);
            package.StatusText = $"#{operation.SequenceNumber} queued";
            addedCount++;
        }

        TrimInstallerOperationHistory();

        if (addedCount == 0)
        {
            InstallerStatusMessage = duplicateCount > 0
                ? $"{sourceLabel}: that action is already queued or running."
                : $"{sourceLabel}: nothing new was added to the installer queue.";
            AddInstallerLog(InstallerStatusMessage);
            return;
        }

        InstallerStatusMessage = duplicateCount > 0
            ? $"{addedLabel} Skipped {duplicateCount} duplicate request{(duplicateCount == 1 ? string.Empty : "s")}."
            : addedLabel;
        AddInstallerLog(InstallerStatusMessage);

        await ProcessInstallerQueueAsync().ConfigureAwait(true);
    }

    private async Task ProcessInstallerQueueAsync()
    {
        if (isInstallerQueueProcessing)
        {
            return;
        }

        var pendingOperation = InstallerOperations.FirstOrDefault(item => item.State == InstallerOperationQueueState.Queued);
        if (pendingOperation is null)
        {
            return;
        }

        isInstallerQueueProcessing = true;
        IsInstallerBusy = true;
        var refreshedStatusAtEnd = false;
        var currentRunOperations = InstallerOperations
            .Where(item => item.State == InstallerOperationQueueState.Queued)
            .ToArray();

        try
        {
            while ((pendingOperation = InstallerOperations.FirstOrDefault(item => item.State == InstallerOperationQueueState.Queued)) is not null)
            {
                RefreshInstallerProgress(pendingOperation);
                pendingOperation.State = InstallerOperationQueueState.Running;
                pendingOperation.StatusText = BuildInstallerOperationActiveStatusText(pendingOperation.Action);
                pendingOperation.Package.StatusText = pendingOperation.StatusText;

                var packageResults = await installerService
                    .RunPackageOperationAsync(pendingOperation.PackageId, pendingOperation.Action)
                    .ConfigureAwait(true);
                var supplementalResults = pendingOperation.SyncFirefoxExtensions
                    ? (await SyncFirefoxExtensionsAsync([pendingOperation.Package], packageResults).ConfigureAwait(true)).ToList()
                    : [];

                LogInstallerOperationResults(pendingOperation, packageResults);
                if (supplementalResults.Count > 0)
                {
                    LogInstallerOperationResults(pendingOperation, supplementalResults);
                }

                var primaryResult = GetPrimaryPackageResult(pendingOperation.PackageId, packageResults);
                if (primaryResult is null)
                {
                    pendingOperation.State = InstallerOperationQueueState.Failed;
                    pendingOperation.StatusText = "The installer did not return a result.";
                    pendingOperation.GuidanceText = "Check the activity log, then try the action again.";
                    pendingOperation.RequiresManualStep = true;
                    pendingOperation.Package.StatusText = pendingOperation.StatusText;
                    continue;
                }

                pendingOperation.State = GetInstallerOperationQueueState(primaryResult);
                pendingOperation.StatusText = primaryResult.Message;
                pendingOperation.GuidanceText = BuildInstallerOperationGuidance(primaryResult, supplementalResults);
                pendingOperation.RequiresManualStep = primaryResult.RequiresManualStep;
                pendingOperation.Package.StatusText = pendingOperation.StatusText;
            }

            if (currentRunOperations.Any(item => item.IsCompleted))
            {
                await RefreshInstallerStatusCoreAsync(addLogEntry: false, preserveBusyState: true).ConfigureAwait(true);
                refreshedStatusAtEnd = true;
            }
        }
        catch (Exception ex)
        {
            InstallerStatusMessage = $"Installer queue failed: {ex.Message}";
            AddInstallerLog(InstallerStatusMessage);
        }
        finally
        {
            ResetInstallerProgress();
            isInstallerQueueProcessing = false;
            IsInstallerBusy = false;

            if (refreshedStatusAtEnd)
            {
                InstallerStatusMessage = BuildInstallerQueueCompletionSummary(currentRunOperations);
                AddInstallerLog(InstallerStatusMessage);
            }

            RefreshInstallerCommandStates();
        }
    }

    private async Task RefreshInstallerStatusCoreAsync(bool addLogEntry, bool addDetailedUpdateLog = false, bool preserveBusyState = false)
    {
        if (!preserveBusyState)
        {
            IsInstallerBusy = true;
        }

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
            if (!preserveBusyState)
            {
                IsInstallerBusy = false;
            }
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
        UpgradeAllInstalledPackagesCommand.NotifyCanExecuteChanged();
        QueuePrimaryInstallerPackageActionCommand.NotifyCanExecuteChanged();
        QueueInteractiveInstallerPackageActionCommand.NotifyCanExecuteChanged();
        QueueReinstallInstallerPackageActionCommand.NotifyCanExecuteChanged();
        ClearCompletedInstallerOperationsCommand.NotifyCanExecuteChanged();
        UninstallSelectedCleanupPackagesCommand.NotifyCanExecuteChanged();
    }

    private void RefreshInstallerProgress(InstallerOperationItem operation)
    {
        var trackedOperations = InstallerOperations.ToArray();
        var totalCount = trackedOperations.Length;
        var completedCount = trackedOperations.Count(item => item.IsCompleted);

        InstallerProgressMaximum = Math.Max(totalCount, 1);
        InstallerProgressValue = completedCount;
        InstallerProgressText = $"{BuildInstallerOperationActionLabel(operation.Action)} {operation.DisplayName} [{Math.Min(completedCount + 1, Math.Max(totalCount, 1))}/{Math.Max(totalCount, 1)}]...";
        IsInstallerProgressVisible = true;
    }

    private void ResetInstallerProgress()
    {
        IsInstallerProgressVisible = false;
        InstallerProgressValue = 0;
        InstallerProgressMaximum = 1;
        InstallerProgressText = string.Empty;
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
        var customInstalledCount = InstallerPackages.Count(item => item.UsesCustomInstallFlow && item.IsInstalled);
        var customSuffix = customInstalledCount > 0
            ? $" Update All Ready also refreshes {customInstalledCount} custom app{(customInstalledCount == 1 ? string.Empty : "s")}."
            : string.Empty;

        return updates.Length switch
        {
            0 => $"No winget-tracked updates found.{customSuffix}".Trim(),
            <= 5 => $"Updates ready: {string.Join(", ", updates)}.{customSuffix}",
            _ => $"Updates ready: {string.Join(", ", updates[..5])}, +{updates.Length - 5} more.{customSuffix}",
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
        var customInstalledCount = InstallerPackages.Count(item => item.UsesCustomInstallFlow && item.IsInstalled);
        var customSuffix = customInstalledCount > 0
            ? $" Update All Ready also refreshes {customInstalledCount} custom app{(customInstalledCount == 1 ? string.Empty : "s")}."
            : string.Empty;

        return updates.Length switch
        {
            0 => $"No winget-tracked updates found.{customSuffix}".Trim(),
            1 => $"Update ready for {updates[0]}.{customSuffix}",
            _ => $"Updates ready for {updates.Length} apps: {string.Join(", ", updates)}.{customSuffix}",
        };
    }

    private string BuildInstallerOperationQueueSummary()
    {
        var queuedCount = InstallerOperations.Count(item => item.State == InstallerOperationQueueState.Queued);
        var runningCount = InstallerOperations.Count(item => item.State == InstallerOperationQueueState.Running);
        var completedCount = InstallerOperations.Count(item => item.IsCompleted);
        var attentionCount = InstallerOperations.Count(item => item.RequiresAttention);

        return $"Queue: {queuedCount} queued  |  {runningCount} running  |  {completedCount} finished  |  {attentionCount} attention";
    }

    private static string BuildInstallerQueueCompletionSummary(IReadOnlyList<InstallerOperationItem> completedOperations)
    {
        var changedCount = completedOperations.Count(item => item.State == InstallerOperationQueueState.Succeeded);
        var unchangedCount = completedOperations.Count(item => item.State == InstallerOperationQueueState.Skipped);
        var failedCount = completedOperations.Count(item => item.State == InstallerOperationQueueState.Failed);
        return $"{changedCount} applied, {unchangedCount} already current, {failedCount} need attention.";
    }

    private static string BuildInstallerOperationActionLabel(InstallerPackageAction action) =>
        action switch
        {
            InstallerPackageAction.Install => "Installing",
            InstallerPackageAction.Update => "Updating",
            InstallerPackageAction.Uninstall => "Removing",
            InstallerPackageAction.InstallInteractive => "Running interactive install for",
            InstallerPackageAction.UpdateInteractive => "Running interactive update for",
            InstallerPackageAction.Reinstall => "Reinstalling",
            _ => "Working on",
        };

    private static string BuildInstallerOperationActiveStatusText(InstallerPackageAction action) =>
        action switch
        {
            InstallerPackageAction.Install => "Installing...",
            InstallerPackageAction.Update => "Updating...",
            InstallerPackageAction.Uninstall => "Removing...",
            InstallerPackageAction.InstallInteractive => "Interactive install running...",
            InstallerPackageAction.UpdateInteractive => "Interactive update running...",
            InstallerPackageAction.Reinstall => "Reinstalling...",
            _ => "Working...",
        };

    private static string BuildInstallerOperationDeduplicationKey(string packageId, InstallerPackageAction action) =>
        action switch
        {
            InstallerPackageAction.Install or InstallerPackageAction.InstallInteractive or InstallerPackageAction.Reinstall => $"{packageId}|install",
            InstallerPackageAction.Update or InstallerPackageAction.UpdateInteractive => $"{packageId}|update",
            InstallerPackageAction.Uninstall => $"{packageId}|remove",
            _ => $"{packageId}|{action}",
        };

    private static InstallerOperationQueueState GetInstallerOperationQueueState(InstallerOperationResult result)
    {
        if (!result.Succeeded)
        {
            return InstallerOperationQueueState.Failed;
        }

        return result.Changed
            ? InstallerOperationQueueState.Succeeded
            : InstallerOperationQueueState.Skipped;
    }

    private static string BuildInstallerOperationGuidance(
        InstallerOperationResult result,
        IReadOnlyList<InstallerOperationResult> supplementalResults)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(result.Guidance))
        {
            parts.Add(result.Guidance.Trim());
        }

        var supplementalSummary = BuildSupplementalResultSummary(supplementalResults, "Firefox add-ons").Trim();
        if (!string.IsNullOrWhiteSpace(supplementalSummary))
        {
            parts.Add(supplementalSummary);
        }

        return string.Join(" ", parts);
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

    private void LogInstallerOperationResults(InstallerOperationItem operation, IReadOnlyList<InstallerOperationResult> results)
    {
        foreach (var result in results)
        {
            var guidanceSuffix = string.IsNullOrWhiteSpace(result.Guidance)
                ? string.Empty
                : $" Next: {result.Guidance}";
            AddInstallerLog($"#{operation.SequenceNumber} {result.DisplayName}: {result.Message}{guidanceSuffix}");
        }
    }

    private void TrimInstallerOperationHistory()
    {
        while (InstallerOperations.Count > MaxInstallerOperationHistory)
        {
            var completedEntry = InstallerOperations
                .Select((item, index) => new { item, index })
                .FirstOrDefault(pair => pair.item.IsCompleted);

            if (completedEntry is null)
            {
                break;
            }

            InstallerOperations.RemoveAt(completedEntry.index);
        }
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

    private static InstallerOperationResult? GetPrimaryPackageResult(
        string packageId,
        IReadOnlyList<InstallerOperationResult> results) =>
        results.FirstOrDefault(result => string.Equals(result.PackageId, packageId, StringComparison.OrdinalIgnoreCase))
        ?? results.LastOrDefault();

    private static string BuildInstallerQueuedBatchMessage(string actionLabel, int count) =>
        $"Queued {count} {BuildInstallerActionNoun(actionLabel, count)}.";

    private static string BuildInstallerActionNoun(string actionLabel, int count) =>
        actionLabel.ToUpperInvariant() switch
        {
            "INSTALL" => count == 1 ? "install" : "installs",
            "UPDATE" => count == 1 ? "update" : "updates",
            "REMOVE" => count == 1 ? "removal" : "removals",
            _ => count == 1 ? "task" : "tasks",
        };
}
