using System.Collections.ObjectModel;
using AutoClicker.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AutoClicker.App.Models;

public partial class InstallerPackageItem : ObservableObject
{
    public InstallerPackageItem(InstallerCatalogItem package)
    {
        Package = package;
    }

    public InstallerCatalogItem Package { get; }

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
}
