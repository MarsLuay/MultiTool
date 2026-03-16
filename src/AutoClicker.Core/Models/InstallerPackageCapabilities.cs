namespace AutoClicker.Core.Models;

public sealed record InstallerPackageCapabilities(
    bool SupportsInstall = false,
    bool SupportsUpdate = false,
    bool SupportsUninstall = false,
    bool SupportsInteractiveInstall = false,
    bool SupportsInteractiveUpdate = false,
    bool SupportsReinstall = false,
    bool SupportsOpenInstallPage = false,
    bool SupportsOpenUpdatePage = false,
    bool UsesWinget = false,
    bool UsesCustomFlow = false,
    bool HasGuidedFallback = false)
{
    public bool HasAdvancedActions =>
        SupportsInteractiveInstall
        || SupportsInteractiveUpdate
        || SupportsReinstall
        || SupportsOpenInstallPage
        || SupportsOpenUpdatePage;
}
