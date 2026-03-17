using AutoClicker.Core.Models;
using AutoClicker.App.Localization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AutoClicker.App.Models;

public partial class InstallerOperationItem : ObservableObject
{
    public InstallerOperationItem(
        int sequenceNumber,
        InstallerPackageItem package,
        InstallerPackageAction action,
        bool syncFirefoxExtensions)
    {
        SequenceNumber = sequenceNumber;
        Package = package;
        Action = action;
        SyncFirefoxExtensions = syncFirefoxExtensions;
    }

    public int SequenceNumber { get; }

    public InstallerPackageItem Package { get; }

    public string PackageId => Package.PackageId;

    public string DisplayName => Package.DisplayName;

    public InstallerPackageAction Action { get; }

    public bool SyncFirefoxExtensions { get; }

    public string DeduplicationKey => $"{PackageId}|{GetDeduplicationBucket(Action)}";

    public bool IsCompleted =>
        State is InstallerOperationQueueState.Succeeded
            or InstallerOperationQueueState.Failed
            or InstallerOperationQueueState.Skipped;

    public bool RequiresAttention => State == InstallerOperationQueueState.Failed || RequiresManualStep;

    public string HeaderText => AppLanguageStrings.FormatForCurrentLanguage(
        AppLanguageKeys.InstallerOperationHeaderFormat,
        SequenceNumber,
        BuildActionLabel(Action),
        DisplayName);

    [ObservableProperty]
    private InstallerOperationQueueState state = InstallerOperationQueueState.Queued;

    [ObservableProperty]
    private string statusText = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.InstallerStatusQueued);

    [ObservableProperty]
    private string guidanceText = string.Empty;

    [ObservableProperty]
    private bool requiresManualStep;

    partial void OnStateChanged(InstallerOperationQueueState value)
    {
        OnPropertyChanged(nameof(IsCompleted));
        OnPropertyChanged(nameof(RequiresAttention));
        OnPropertyChanged(nameof(HeaderText));
    }

    partial void OnRequiresManualStepChanged(bool value)
    {
        OnPropertyChanged(nameof(RequiresAttention));
    }

    private static string BuildActionLabel(InstallerPackageAction action) =>
        action switch
        {
            InstallerPackageAction.Install => AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.InstallerActionInstall),
            InstallerPackageAction.Update => AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.InstallerActionUpdate),
            InstallerPackageAction.Uninstall => AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.InstallerActionRemove),
            InstallerPackageAction.InstallInteractive => AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.InstallerActionInteractiveInstall),
            InstallerPackageAction.UpdateInteractive => AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.InstallerActionInteractiveUpdate),
            InstallerPackageAction.Reinstall => AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.InstallerActionReinstall),
            _ => AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.InstallerActionRun),
        };

    private static string GetDeduplicationBucket(InstallerPackageAction action) =>
        action switch
        {
            InstallerPackageAction.Install or InstallerPackageAction.InstallInteractive or InstallerPackageAction.Reinstall => "install",
            InstallerPackageAction.Update or InstallerPackageAction.UpdateInteractive => "update",
            InstallerPackageAction.Uninstall => "remove",
            _ => action.ToString(),
        };
}
