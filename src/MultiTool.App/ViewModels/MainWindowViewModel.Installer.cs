using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows.Data;
using MultiTool.App.Localization;
using MultiTool.App.Models;
using MultiTool.App.Services;
using MultiTool.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MultiTool.App.ViewModels;

public partial class MainWindowViewModel
{
    private const string FirefoxPackageId = "Mozilla.Firefox";
    private const string EdgePackageId = "Microsoft.Edge";
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
    private string installerStatusMessage = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.InstallerStatusPreparingCatalog);

    [ObservableProperty]
    private string installerEnvironmentMessage = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.InstallerEnvironmentDefault);

    [ObservableProperty]
    private string installerAppUpdateSummary = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.InstallerAppUpdateSummaryDefault);

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
    private string cleanupStatusMessage = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.InstallerStatusCleanupLoading);

    private bool hasCompletedInstallerStatusCheck;

    private void EnsureInstallerInitialized()
    {
        InitializeInstallerState();
        StartInstallerInitialization();
    }

    private void InitializeInstallerState()
    {
        if (isInstallerStateInitialized)
        {
            return;
        }

        isInstallerStateInitialized = true;
        InstallerOperations.CollectionChanged += InstallerOperations_OnCollectionChanged;

        foreach (var package in installerService.GetCatalog().OrderBy(item => item.Category).ThenBy(item => item.DisplayName))
        {
            var packageItem = new InstallerPackageItem(package, installerService.GetPackageCapabilities(package.PackageId))
            {
                StatusText = L(AppLanguageKeys.InstallerStatusChecking),
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
                StatusText = L(AppLanguageKeys.InstallerStatusChecking),
            };

            packageItem.PropertyChanged += InstallerPackageItem_OnPropertyChanged;
            CleanupPackages.Add(packageItem);
        }

        InstallerPackagesView = CollectionViewSource.GetDefaultView(InstallerPackages);
        InstallerPackagesView.Filter = FilterInstallerPackage;
        if (InstallerPackagesView is ListCollectionView installerPackagesCollectionView)
        {
            installerPackagesCollectionView.SortDescriptions.Clear();
            installerPackagesCollectionView.SortDescriptions.Add(new SortDescription(nameof(InstallerPackageItem.Category), ListSortDirection.Ascending));
            installerPackagesCollectionView.SortDescriptions.Add(new SortDescription(nameof(InstallerPackageItem.DisplayName), ListSortDirection.Ascending));
            installerPackagesCollectionView.GroupDescriptions.Clear();
            installerPackagesCollectionView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(InstallerPackageItem.Category)));
        }

        ApplyInstallerSettings(deferredInstallerSettings);
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
        deferredInstallerSettings = settings?.Clone() ?? new InstallerSettings();

        if (!isInstallerStateInitialized)
        {
            return;
        }

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

    private InstallerSettings BuildInstallerSettings()
    {
        if (!isInstallerStateInitialized)
        {
            return deferredInstallerSettings.Clone();
        }

        return new InstallerSettings
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
    }

    private void StartInstallerInitialization()
    {
        if (isInstallerInitializationStarted)
        {
            return;
        }

        isInstallerInitializationStarted = true;
        _ = InitializeInstallerAsync();
    }

    private async Task InitializeInstallerAsync()
    {
        try
        {
            InstallerStatusMessage = L(AppLanguageKeys.InstallerStatusPreparingCatalog);
            InstallerEnvironmentMessage = L(AppLanguageKeys.InstallerEnvironmentDefault);
            InstallerAppUpdateSummary = L(AppLanguageKeys.InstallerAppUpdateSummaryDefault);
            CleanupStatusMessage = L(AppLanguageKeys.InstallerStatusCleanupLoading);
            await RefreshInstallerStatusCoreAsync(addLogEntry: false).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            InstallerStatusMessage = F(AppLanguageKeys.InstallerSetupFailedFormat, ex.Message);
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
        InstallerStatusMessage = F(AppLanguageKeys.InstallerCheckingTrackedAppsFormat, InstallerPackages.Count, PluralSuffix(InstallerPackages.Count));
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

        InstallerStatusMessage = L(AppLanguageKeys.InstallerSelectedRecommended);
        AddInstallerLog(InstallerStatusMessage);
    }

    [RelayCommand]
    private void SelectDeveloperInstallerPackages()
    {
        foreach (var package in InstallerPackages)
        {
            package.IsSelected = package.IsDeveloperTool;
        }

        InstallerStatusMessage = L(AppLanguageKeys.InstallerSelectedDeveloper);
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

        InstallerStatusMessage = L(AppLanguageKeys.InstallerSelectionCleared);
        AddInstallerLog(InstallerStatusMessage);
    }

    [RelayCommand]
    private void SelectRecommendedCleanupPackages()
    {
        foreach (var package in CleanupPackages)
        {
            package.IsSelected = package.IsRecommended;
        }

        CleanupStatusMessage = L(AppLanguageKeys.CleanupSelectedRecommended);
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

        CleanupStatusMessage = L(AppLanguageKeys.CleanupSelectionCleared);
        AddInstallerLog(CleanupStatusMessage);
    }

    private bool CanInstallSelectedPackages =>
        !IsInstallerBusy
        && InstallerPackages.Any(item => item.IsSelected && !item.IsInstalled && (IsWingetAvailable || item.CanInstallWithoutWinget));

    [RelayCommand(CanExecute = nameof(CanInstallSelectedPackages))]
    private async Task InstallSelectedPackagesAsync()
    {
        await QueueInstallerBatchAsync(
            actionLabel: "install",
            action: InstallerPackageAction.Install,
            requireInstalledSelection: false,
            syncFirefoxExtensions: true,
            canRunWithoutWinget: item => item.CanInstallWithoutWinget,
            packageFilter: item => item.IsSelected && !item.IsInstalled,
            emptySelectionMessage: L(AppLanguageKeys.InstallerSelectionAllSelectedAlreadyInstalled));
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
            emptySelectionMessage: L(AppLanguageKeys.InstallerSelectionNoUpdatesReady));
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
            addedLabel: action == InstallerPackageAction.Install
                ? L(AppLanguageKeys.InstallerAddedQueuedInstall)
                : L(AppLanguageKeys.InstallerAddedQueuedUpdate)).ConfigureAwait(true);
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
                ? L(AppLanguageKeys.InstallerAddedQueuedInteractiveInstall)
                : L(AppLanguageKeys.InstallerAddedQueuedInteractiveUpdate)).ConfigureAwait(true);
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
            addedLabel: L(AppLanguageKeys.InstallerAddedQueuedReinstall)).ConfigureAwait(true);
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
            InstallerStatusMessage = F(AppLanguageKeys.InstallerNoOfficialPageFormat, package.DisplayName);
            AddInstallerLog(InstallerStatusMessage);
            return;
        }

        try
        {
            var launchResult = browserLauncherService.OpenUrl(targetUrl);
            InstallerStatusMessage = F(AppLanguageKeys.InstallerOpenedOfficialPageFormat, package.DisplayName, launchResult.BrowserDisplayName);
            AddInstallerLog(F(AppLanguageKeys.InstallerLogOpenedOfficialPageInBrowserFormat, package.DisplayName, targetUrl, launchResult.BrowserDisplayName));
        }
        catch (Exception ex)
        {
            InstallerStatusMessage = F(AppLanguageKeys.InstallerOpenOfficialPageFailedFormat, package.DisplayName, ex.Message);
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
            CleanupStatusMessage = L(AppLanguageKeys.CleanupSelectInstalledFirst);
            AddInstallerLog(CleanupStatusMessage);
            return;
        }

        IsInstallerBusy = true;
        CleanupStatusMessage = F(AppLanguageKeys.CleanupRemovingAppsFormat, selectedPackages.Length, PluralSuffix(selectedPackages.Length));
        AddInstallerLog(CleanupStatusMessage);

        try
        {
            var results = await installerService.UninstallPackagesAsync(selectedPackages.Select(item => item.PackageId).ToArray());
            var adjustedResults = results.ToList();

            var edgeResultIndex = adjustedResults.FindIndex(result =>
                string.Equals(result.PackageId, EdgePackageId, StringComparison.OrdinalIgnoreCase)
                && !result.Succeeded);

            if (edgeResultIndex >= 0)
            {
                AddInstallerLog(L(AppLanguageKeys.InstallerLogEdgeWingetFailedTryingFallback));
                var edgeFallbackResult = await edgeRemovalService.RemoveAsync().ConfigureAwait(true);
                adjustedResults[edgeResultIndex] = new InstallerOperationResult(
                    EdgePackageId,
                    L(AppLanguageKeys.InstallerEdgeDisplayName),
                    edgeFallbackResult.Succeeded,
                    edgeFallbackResult.Changed,
                    F(AppLanguageKeys.InstallerEdgeFallbackMessageFormat, edgeFallbackResult.Message),
                    string.Empty,
                    edgeFallbackResult.Succeeded ? string.Empty : L(AppLanguageKeys.InstallerEdgeFallbackGuidanceRunAsAdminRetry));
            }

            foreach (var result in adjustedResults)
            {
                AddInstallerLog(F(AppLanguageKeys.InstallerLogResultDisplayMessageFormat, result.DisplayName, result.Message));
            }

            var removedCount = adjustedResults.Count(result => result.Succeeded && result.Changed);
            var missingCount = adjustedResults.Count(result => result.Succeeded && !result.Changed);
            var failedCount = adjustedResults.Count(result => !result.Succeeded);
            var manualStepCount = adjustedResults.Count(result => result.RequiresManualStep);

            var summary = F(AppLanguageKeys.CleanupSummaryCountsFormat, removedCount, missingCount, failedCount);
            if (manualStepCount > 0)
            {
                summary += F(AppLanguageKeys.CleanupSummaryManualStepsFormat, manualStepCount);
            }

            if (failedCount > 0)
            {
                var firstFailure = adjustedResults.First(result => !result.Succeeded);
                summary += F(AppLanguageKeys.CleanupSummaryFirstFailureFormat, firstFailure.DisplayName, firstFailure.Message);
                if (!string.IsNullOrWhiteSpace(firstFailure.Guidance))
                {
                    summary += F(AppLanguageKeys.CleanupSummaryNextStepFormat, firstFailure.Guidance);
                }

                var failureLogPath = WriteCleanupFailureLog(selectedPackages, adjustedResults);
                summary += F(AppLanguageKeys.CleanupSummaryFailureLogPathFormat, failureLogPath);
            }

            CleanupStatusMessage = summary;
            AddInstallerLog(CleanupStatusMessage);
            await RefreshInstallerStatusCoreAsync(addLogEntry: false);
        }
        catch (Exception ex)
        {
            CleanupStatusMessage = F(AppLanguageKeys.CleanupFailedFormat, ex.Message);
            AddInstallerLog(CleanupStatusMessage);

            var failureLogPath = WriteCleanupExceptionLog(selectedPackages, ex);
            AddInstallerLog(F(AppLanguageKeys.CleanupLogFailureLogPathFormat, failureLogPath));
        }
        finally
        {
            IsInstallerBusy = false;
        }
    }

    private static string WriteCleanupFailureLog(
        IReadOnlyList<InstallerPackageItem> selectedPackages,
        IReadOnlyList<InstallerOperationResult> results)
    {
        Directory.CreateDirectory(AppLog.LogsDirectoryPath);

        var filePath = Path.Combine(
            AppLog.LogsDirectoryPath,
            $"cleanup-uninstall-failure-{DateTime.Now:yyyyMMdd-HHmmss}.log");

        var builder = new StringBuilder();
        builder.AppendLine(AppLanguageStrings.Get(AppLanguageKeys.CleanupUninstallFailureTitle, AppLanguage.English));
        builder.AppendLine(AppLanguageStrings.Format(AppLanguageKeys.CleanupUninstallTimestampFormat, AppLanguage.English, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
        builder.AppendLine(AppLanguageStrings.Format(AppLanguageKeys.CleanupUninstallSelectedCountFormat, AppLanguage.English, selectedPackages.Count));
        builder.AppendLine();
        builder.AppendLine(AppLanguageStrings.Get(AppLanguageKeys.CleanupUninstallSelectedPackages, AppLanguage.English));
        foreach (var package in selectedPackages)
        {
            builder.AppendLine($"- {package.DisplayName} ({package.PackageId})");
        }

        builder.AppendLine();
        builder.AppendLine(AppLanguageStrings.Get(AppLanguageKeys.CleanupUninstallOperationResults, AppLanguage.English));
        foreach (var result in results)
        {
            builder.AppendLine($"- {result.DisplayName} ({result.PackageId})");
            builder.AppendLine(AppLanguageStrings.Format(AppLanguageKeys.CleanupResultSucceededFormat, AppLanguage.English, result.Succeeded));
            builder.AppendLine(AppLanguageStrings.Format(AppLanguageKeys.CleanupResultChangedFormat, AppLanguage.English, result.Changed));
            builder.AppendLine(AppLanguageStrings.Format(AppLanguageKeys.CleanupResultRequiresManualStepFormat, AppLanguage.English, result.RequiresManualStep));
            builder.AppendLine(AppLanguageStrings.Format(AppLanguageKeys.CleanupResultMessageFormat, AppLanguage.English, result.Message));

            if (!string.IsNullOrWhiteSpace(result.Guidance))
            {
                builder.AppendLine(AppLanguageStrings.Format(AppLanguageKeys.CleanupResultGuidanceFormat, AppLanguage.English, result.Guidance));
            }

            if (!string.IsNullOrWhiteSpace(result.Output))
            {
                builder.AppendLine(AppLanguageStrings.Get(AppLanguageKeys.CleanupResultOutputLabel, AppLanguage.English));
                foreach (var line in result.Output.Split(Environment.NewLine))
                {
                    builder.AppendLine($"    {line}");
                }
            }
        }

        File.WriteAllText(filePath, builder.ToString());
        return filePath;
    }

    private static string WriteCleanupExceptionLog(IReadOnlyList<InstallerPackageItem> selectedPackages, Exception ex)
    {
        Directory.CreateDirectory(AppLog.LogsDirectoryPath);

        var filePath = Path.Combine(
            AppLog.LogsDirectoryPath,
            $"cleanup-uninstall-exception-{DateTime.Now:yyyyMMdd-HHmmss}.log");

        var builder = new StringBuilder();
        builder.AppendLine(AppLanguageStrings.Get(AppLanguageKeys.CleanupUninstallExceptionTitle, AppLanguage.English));
        builder.AppendLine(AppLanguageStrings.Format(AppLanguageKeys.CleanupUninstallTimestampFormat, AppLanguage.English, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
        builder.AppendLine();
        builder.AppendLine(AppLanguageStrings.Get(AppLanguageKeys.CleanupUninstallSelectedPackages, AppLanguage.English));
        foreach (var package in selectedPackages)
        {
            builder.AppendLine($"- {package.DisplayName} ({package.PackageId})");
        }

        builder.AppendLine();
        builder.AppendLine(AppLanguageStrings.Get(AppLanguageKeys.CleanupUninstallException, AppLanguage.English));
        builder.AppendLine(ex.ToString());

        File.WriteAllText(filePath, builder.ToString());
        return filePath;
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
                    ? L(AppLanguageKeys.InstallerSelectionNoInstalledReadyToUpdate)
                    : L(AppLanguageKeys.InstallerSelectionSelectAtLeastOneFirst));
            AddInstallerLog(InstallerStatusMessage);
            return;
        }

        await QueueInstallerOperationsAsync(
            selectedPackages,
            action,
            syncFirefoxExtensions,
            L(AppLanguageKeys.InstallerQueueSourceLabel),
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
        var installedSkipCount = 0;

        foreach (var package in packages)
        {
            if ((action is InstallerPackageAction.Install or InstallerPackageAction.InstallInteractive) && package.IsInstalled)
            {
                installedSkipCount++;
                continue;
            }

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
            package.StatusText = F(AppLanguageKeys.InstallerPackageStatusQueuedSequenceFormat, operation.SequenceNumber);
            addedCount++;
        }

        TrimInstallerOperationHistory();

        if (addedCount == 0)
        {
            InstallerStatusMessage = duplicateCount > 0
                ? F(AppLanguageKeys.InstallerStatusSourceAlreadyQueuedOrRunningFormat, sourceLabel)
                : installedSkipCount > 0
                    ? F(AppLanguageKeys.InstallerStatusSourceSelectedAppsAlreadyInstalledFormat, sourceLabel, installedSkipCount == 1 ? L(AppLanguageKeys.InstallerPluralIs) : L(AppLanguageKeys.InstallerPluralAre))
                : F(AppLanguageKeys.InstallerStatusSourceNothingNewAddedFormat, sourceLabel);
            AddInstallerLog(InstallerStatusMessage);
            return;
        }

        var suffixParts = new List<string>();
        if (duplicateCount > 0)
        {
            suffixParts.Add(F(AppLanguageKeys.InstallerSuffixSkippedDuplicateRequestsFormat, duplicateCount, duplicateCount == 1 ? string.Empty : L(AppLanguageKeys.InstallerPluralS)));
        }

        if (installedSkipCount > 0)
        {
            suffixParts.Add(F(AppLanguageKeys.InstallerSuffixSkippedAlreadyInstalledAppsFormat, installedSkipCount, installedSkipCount == 1 ? string.Empty : L(AppLanguageKeys.InstallerPluralS)));
        }

        InstallerStatusMessage = suffixParts.Count > 0
            ? $"{addedLabel} {string.Join(". ", suffixParts)}."
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
                    pendingOperation.StatusText = L(AppLanguageKeys.InstallerOperationNoResult);
                    pendingOperation.GuidanceText = L(AppLanguageKeys.InstallerOperationNoResultGuidance);
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
            InstallerStatusMessage = F(AppLanguageKeys.InstallerQueueFailedFormat, ex.Message);
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
                        ? L(AppLanguageKeys.InstallerPackageStatusGuidedInstall)
                        : L(AppLanguageKeys.InstallerPackageStatusWingetUnavailable);
                }

                foreach (var package in CleanupPackages)
                {
                    package.IsInstalled = false;
                    package.HasUpdateAvailable = false;
                    package.StatusText = L(AppLanguageKeys.InstallerPackageStatusWingetUnavailable);
                }

                var guidedCount = InstallerPackages.Count(item => item.UsesGuidedInstall);
                InstallerStatusMessage = guidedCount > 0
                    ? F(AppLanguageKeys.InstallerStatusGuidedAppsCanOpenOfficialPagesFormat, environment.Message, guidedCount, guidedCount == 1 ? string.Empty : L(AppLanguageKeys.InstallerPluralS))
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
            InstallerStatusMessage = F(AppLanguageKeys.InstallerStatusInstalledUpdatesFormat, installedCount, updateCount, PluralSuffix(updateCount));
            var installedCleanupCount = CleanupPackages.Count(item => item.IsInstalled);
            CleanupStatusMessage = F(AppLanguageKeys.CleanupStatusInstalledRemovableFormat, installedCleanupCount, PluralSuffix(installedCleanupCount));

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
            InstallerStatusMessage = F(AppLanguageKeys.InstallerRefreshFailedFormat, ex.Message);
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
        InstallerProgressText = F(
            AppLanguageKeys.InstallerProgressTextFormat,
            BuildInstallerOperationActionLabel(operation.Action),
            operation.DisplayName,
            Math.Min(completedCount + 1, Math.Max(totalCount, 1)),
            Math.Max(totalCount, 1));
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
        return F(AppLanguageKeys.InstallerSelectionSummaryFormat, selectedCount, installedCount, updateCount);
    }

    private string BuildInstallerUpdateSummary()
    {
        if (!hasCompletedInstallerStatusCheck)
        {
            return L(AppLanguageKeys.InstallerUpdateSummaryInitial);
        }

        if (!IsWingetAvailable)
        {
            return L(AppLanguageKeys.InstallerUpdateSummaryUnavailable);
        }

        var updates = InstallerPackages
            .Where(item => item.HasUpdateAvailable)
            .OrderBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase)
            .Select(item => item.DisplayName)
            .ToArray();
        var customInstalledCount = InstallerPackages.Count(item => item.UsesCustomInstallFlow && item.IsInstalled);
        var customSuffix = customInstalledCount > 0
            ? F(AppLanguageKeys.InstallerUpdateCustomSuffixFormat, customInstalledCount, PluralSuffix(customInstalledCount))
            : string.Empty;

        return updates.Length switch
        {
            0 => F(AppLanguageKeys.InstallerUpdateNoneFoundFormat, customSuffix).Trim(),
            <= 5 => F(AppLanguageKeys.InstallerUpdateReadyListFormat, string.Join(", ", updates), customSuffix),
            _ => F(AppLanguageKeys.InstallerUpdateReadyMoreFormat, string.Join(", ", updates[..5]), updates.Length - 5, customSuffix),
        };
    }

    private string BuildCleanupSelectionSummary()
    {
        var selectedCount = CleanupPackages.Count(item => item.IsSelected);
        var installedCount = CleanupPackages.Count(item => item.IsInstalled);
        return F(AppLanguageKeys.CleanupSelectionSummaryFormat, selectedCount, installedCount);
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
            ? F(AppLanguageKeys.InstallerUpdateCustomSuffixFormat, customInstalledCount, customInstalledCount == 1 ? string.Empty : L(AppLanguageKeys.InstallerPluralS))
            : string.Empty;

        return updates.Length switch
        {
            0 => F(AppLanguageKeys.InstallerUpdateNoneFoundFormat, customSuffix).Trim(),
            1 => F(AppLanguageKeys.InstallerUpdateReadyListFormat, updates[0], customSuffix),
            _ => F(AppLanguageKeys.InstallerUpdateReadyMoreFormat, updates[0], updates.Length - 1, customSuffix),
        };
    }

    private string BuildInstallerOperationQueueSummary()
    {
        var queuedCount = InstallerOperations.Count(item => item.State == InstallerOperationQueueState.Queued);
        var runningCount = InstallerOperations.Count(item => item.State == InstallerOperationQueueState.Running);
        var completedCount = InstallerOperations.Count(item => item.IsCompleted);
        var attentionCount = InstallerOperations.Count(item => item.RequiresAttention);

        return F(AppLanguageKeys.InstallerQueueSummaryFormat, queuedCount, runningCount, completedCount, attentionCount);
    }

    private static string BuildInstallerQueueCompletionSummary(IReadOnlyList<InstallerOperationItem> completedOperations)
    {
        var changedCount = completedOperations.Count(item => item.State == InstallerOperationQueueState.Succeeded);
        var unchangedCount = completedOperations.Count(item => item.State == InstallerOperationQueueState.Skipped);
        var failedCount = completedOperations.Count(item => item.State == InstallerOperationQueueState.Failed);
        return AppLanguageStrings.FormatForCurrentLanguage(AppLanguageKeys.InstallerQueueCompletionSummaryFormat, changedCount, unchangedCount, failedCount);
    }

    private string BuildInstallerOperationActionLabel(InstallerPackageAction action) =>
        action switch
        {
            InstallerPackageAction.Install => L(AppLanguageKeys.InstallerActionInstalling),
            InstallerPackageAction.Update => L(AppLanguageKeys.InstallerActionUpdating),
            InstallerPackageAction.Uninstall => L(AppLanguageKeys.InstallerActionRemoving),
            InstallerPackageAction.InstallInteractive => L(AppLanguageKeys.InstallerActionInteractiveInstallRunningFor),
            InstallerPackageAction.UpdateInteractive => L(AppLanguageKeys.InstallerActionInteractiveUpdateRunningFor),
            InstallerPackageAction.Reinstall => L(AppLanguageKeys.InstallerActionReinstalling),
            _ => L(AppLanguageKeys.InstallerActionWorkingOn),
        };

    private string BuildInstallerOperationActiveStatusText(InstallerPackageAction action) =>
        action switch
        {
            InstallerPackageAction.Install => L(AppLanguageKeys.InstallerActiveInstalling),
            InstallerPackageAction.Update => L(AppLanguageKeys.InstallerActiveUpdating),
            InstallerPackageAction.Uninstall => L(AppLanguageKeys.InstallerActiveRemoving),
            InstallerPackageAction.InstallInteractive => L(AppLanguageKeys.InstallerActiveInteractiveInstall),
            InstallerPackageAction.UpdateInteractive => L(AppLanguageKeys.InstallerActiveInteractiveUpdate),
            InstallerPackageAction.Reinstall => L(AppLanguageKeys.InstallerActiveReinstalling),
            _ => L(AppLanguageKeys.InstallerActiveWorking),
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

        var supplementalSummary = BuildSupplementalResultSummary(
            supplementalResults,
            AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.InstallerFirefoxAddonsLabel)).Trim();
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
            AddInstallerLog(L(AppLanguageKeys.InstallerLogFirefoxAddonsSkippedBecauseInstallFailed));
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
                    ? F(AppLanguageKeys.InstallerLogUpdateInfoWithUrlFormat, updateInfo.Message, updateInfo.ReleaseUrl)
                    : updateInfo.Message;
                AddInstallerLog(logMessage);
            }
        }
        catch (Exception ex)
        {
            InstallerAppUpdateSummary = F(AppLanguageKeys.InstallerUpdateCheckFailedFormat, ex.Message);
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
        return AppLanguageStrings.FormatForCurrentLanguage(
            AppLanguageKeys.InstallerSupplementalSummaryFormat,
            label,
            changedCount,
            unchangedCount,
            failedCount);
    }

    private void LogInstallerOperationResults(InstallerOperationItem operation, IReadOnlyList<InstallerOperationResult> results)
    {
        foreach (var result in results)
        {
            var guidanceSuffix = string.IsNullOrWhiteSpace(result.Guidance)
                ? string.Empty
                : F(AppLanguageKeys.InstallerLogGuidanceSuffixFormat, result.Guidance);
            AddInstallerLog(F(AppLanguageKeys.InstallerLogOperationResultFormat, operation.SequenceNumber, result.DisplayName, result.Message, guidanceSuffix));
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
                package.StatusText = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.InstallerStatusUnavailable);
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
        AppLanguageStrings.FormatForCurrentLanguage(
            AppLanguageKeys.InstallerQueuedBatchMessageFormat,
            count,
            BuildInstallerActionNoun(actionLabel, count));

    private static string BuildInstallerActionNoun(string actionLabel, int count) =>
        actionLabel.ToUpperInvariant() switch
        {
            "INSTALL" => count == 1
                ? AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.InstallerActionNounInstallSingular)
                : AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.InstallerActionNounInstallPlural),
            "UPDATE" => count == 1
                ? AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.InstallerActionNounUpdateSingular)
                : AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.InstallerActionNounUpdatePlural),
            "REMOVE" => count == 1
                ? AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.InstallerActionNounRemovalSingular)
                : AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.InstallerActionNounRemovalPlural),
            _ => count == 1
                ? AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.InstallerActionNounTaskSingular)
                : AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.InstallerActionNounTaskPlural),
        };
}
