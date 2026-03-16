using AutoClicker.Core.Models;
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

    public string HeaderText => $"#{SequenceNumber} {BuildActionLabel(Action)} {DisplayName}";

    [ObservableProperty]
    private InstallerOperationQueueState state = InstallerOperationQueueState.Queued;

    [ObservableProperty]
    private string statusText = "Queued";

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
            InstallerPackageAction.Install => "Install",
            InstallerPackageAction.Update => "Update",
            InstallerPackageAction.Uninstall => "Remove",
            InstallerPackageAction.InstallInteractive => "Interactive Install",
            InstallerPackageAction.UpdateInteractive => "Interactive Update",
            InstallerPackageAction.Reinstall => "Reinstall",
            _ => "Run",
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
