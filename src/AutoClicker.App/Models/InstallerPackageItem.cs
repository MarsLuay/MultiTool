using System.Collections.ObjectModel;
using AutoClicker.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AutoClicker.App.Models;

public partial class InstallerPackageItem : ObservableObject
{
    public InstallerPackageItem(InstallerCatalogItem package, InstallerPackageCapabilities capabilities)
    {
        Package = package;
        Capabilities = capabilities;
    }

    public InstallerCatalogItem Package { get; }

    public InstallerPackageCapabilities Capabilities { get; }

    public string PackageId => Package.PackageId;

    public string DisplayName => Package.DisplayName;

    public string Category => Package.Category;

    public string Description => Package.Description;

    public string PackageHintText =>
        UsesCustomInstallFlow
            ? "Handled by MultiTool"
            : string.Equals(Package.Source, "msstore", StringComparison.OrdinalIgnoreCase)
                ? "Microsoft Store app"
                : UsesGuidedInstall
                    ? "Official setup page"
                    : "Windows app";

    public bool IsRecommended => Package.IsRecommended;

    public bool IsDeveloperTool => Package.IsDeveloperTool;

    public bool UsesCustomInstallFlow => Package.UsesCustomInstallFlow;

    public bool UsesGuidedInstall => !Package.UsesCustomInstallFlow && !string.IsNullOrWhiteSpace(Package.InstallUrl);

    public bool UsesGuidedUpdate => !Package.UsesCustomInstallFlow && (!string.IsNullOrWhiteSpace(Package.UpdateUrl) || UsesGuidedInstall);

    public bool CanInstallWithoutWinget => UsesGuidedInstall || UsesCustomInstallFlow;

    public bool CanUpdateWithoutWinget => UsesGuidedUpdate || UsesCustomInstallFlow;

    public bool CanQueuePrimaryAction =>
        !IsInstalled
            ? Capabilities.SupportsInstall
            : Capabilities.SupportsUpdate;

    public bool CanQueueInteractiveAction =>
        !IsInstalled
            ? Capabilities.SupportsInteractiveInstall
            : Capabilities.SupportsInteractiveUpdate;

    public bool CanQueueReinstallAction => IsInstalled && Capabilities.SupportsReinstall;

    public bool CanOpenRelevantPage =>
        !IsInstalled
            ? Capabilities.SupportsOpenInstallPage
            : Capabilities.SupportsOpenUpdatePage || Capabilities.SupportsOpenInstallPage;

    public bool HasAdvancedActions =>
        CanQueueInteractiveAction
        || CanQueueReinstallAction
        || CanOpenRelevantPage;

    public string PrimaryActionText => IsInstalled ? "Queue Update" : "Queue Install";

    public string InteractiveActionText => IsInstalled ? "Interactive Update" : "Interactive Install";

    public string PageActionText => IsInstalled && Capabilities.SupportsOpenUpdatePage ? "Open Update Page" : "Open Install Page";

    public string CapabilitySummaryText => BuildCapabilitySummaryText();

    public ObservableCollection<InstallerPackageOptionItem> Options { get; } = [];

    public bool HasOptions => Options.Count > 0;

    public string SearchText => $"{DisplayName} {Category} {Description} {PackageId}";

    [ObservableProperty]
    private bool isSelected;

    [ObservableProperty]
    private bool isInstalled;

    [ObservableProperty]
    private bool hasUpdateAvailable;

    [ObservableProperty]
    private string statusText = "Checking status...";

    partial void OnIsInstalledChanged(bool value)
    {
        NotifyActionPropertiesChanged();
    }

    partial void OnHasUpdateAvailableChanged(bool value)
    {
        NotifyActionPropertiesChanged();
    }

    private void NotifyActionPropertiesChanged()
    {
        OnPropertyChanged(nameof(CanQueuePrimaryAction));
        OnPropertyChanged(nameof(CanQueueInteractiveAction));
        OnPropertyChanged(nameof(CanQueueReinstallAction));
        OnPropertyChanged(nameof(CanOpenRelevantPage));
        OnPropertyChanged(nameof(HasAdvancedActions));
        OnPropertyChanged(nameof(PrimaryActionText));
        OnPropertyChanged(nameof(InteractiveActionText));
        OnPropertyChanged(nameof(PageActionText));
        OnPropertyChanged(nameof(CapabilitySummaryText));
    }

    private string BuildCapabilitySummaryText()
    {
        var parts = new List<string>();

        if (Capabilities.UsesCustomFlow)
        {
            parts.Add("Custom flow");
        }
        else if (Capabilities.UsesWinget)
        {
            parts.Add("Quiet winget");
        }

        if (CanQueueInteractiveAction)
        {
            parts.Add("Interactive option");
        }

        if (Capabilities.SupportsReinstall)
        {
            parts.Add("Reinstall");
        }

        if (CanOpenRelevantPage || Capabilities.HasGuidedFallback)
        {
            parts.Add("Official page");
        }

        return parts.Count == 0
            ? PackageHintText
            : string.Join("  •  ", parts);
    }
}
