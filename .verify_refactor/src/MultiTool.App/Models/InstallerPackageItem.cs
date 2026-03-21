using System.Collections.ObjectModel;
using MultiTool.App.Localization;
using MultiTool.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MultiTool.App.Models;

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
            ? AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.InstallerPackageHintHandledByMultiTool)
            : string.Equals(Package.Source, "msstore", StringComparison.OrdinalIgnoreCase)
                ? AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.InstallerPackageHintMicrosoftStoreApp)
                : UsesGuidedInstall
                    ? AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.InstallerPackageHintOfficialSetupPage)
                    : AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.InstallerPackageHintWindowsApp);

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

    public string PrimaryActionText => IsInstalled
        ? AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.InstallerPrimaryActionQueueUpdate)
        : AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.InstallerPrimaryActionQueueInstall);

    public string InteractiveActionText => IsInstalled
        ? AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.InstallerInteractiveActionUpdate)
        : AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.InstallerInteractiveActionInstall);

    public string PageActionText => IsInstalled && Capabilities.SupportsOpenUpdatePage
        ? AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.InstallerPageActionOpenUpdatePage)
        : AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.InstallerPageActionOpenInstallPage);

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
    private string statusText = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.InstallerStatusChecking);

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
            parts.Add(AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.InstallerCapabilityCustomFlow));
        }
        else if (Capabilities.UsesWinget)
        {
            parts.Add(AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.InstallerCapabilityQuietWinget));
        }

        if (CanQueueInteractiveAction)
        {
            parts.Add(AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.InstallerCapabilityInteractiveOption));
        }

        if (Capabilities.SupportsReinstall)
        {
            parts.Add(AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.InstallerCapabilityReinstall));
        }

        if (CanOpenRelevantPage || Capabilities.HasGuidedFallback)
        {
            parts.Add(AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.InstallerCapabilityOfficialPage));
        }

        return parts.Count == 0
            ? PackageHintText
            : string.Join("  •  ", parts);
    }
}
