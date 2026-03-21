namespace MultiTool.App.Localization;

public static partial class AppLanguageStrings
{
    private static void AddCoreValues(Dictionary<string, (string English, string CatSpeak)> values)
    {
        values[AppLanguageKeys.CleanupUninstallFailureTitle] = ("MultiTool Cleanup Uninstall Failure", "MultiTool Cleanup Uninstall Failure");
        values[AppLanguageKeys.CleanupUninstallExceptionTitle] = ("MultiTool Cleanup Uninstall Exception", "MultiTool Cleanup Uninstall Exception");
        values[AppLanguageKeys.CleanupUninstallTimestampFormat] = ("Timestamp: {0}", "Timestamp: {0}");
        values[AppLanguageKeys.CleanupUninstallSelectedCountFormat] = ("Selected count: {0}", "Selected count: {0}");
        values[AppLanguageKeys.CleanupUninstallSelectedPackages] = ("Selected packages:", "Selected packages:");
        values[AppLanguageKeys.CleanupUninstallOperationResults] = ("Operation results:", "Operation results:");
        values[AppLanguageKeys.CleanupUninstallException] = ("Exception:", "Exception:");
        values[AppLanguageKeys.CleanupResultSucceededFormat] = ("  Succeeded: {0}", "  Succeeded: {0}");
        values[AppLanguageKeys.CleanupResultChangedFormat] = ("  Changed: {0}", "  Changed: {0}");
        values[AppLanguageKeys.CleanupResultRequiresManualStepFormat] = ("  RequiresManualStep: {0}", "  RequiresManualStep: {0}");
        values[AppLanguageKeys.CleanupResultMessageFormat] = ("  Message: {0}", "  Message: {0}");
        values[AppLanguageKeys.CleanupResultGuidanceFormat] = ("  Guidance: {0}", "  Guidance: {0}");
        values[AppLanguageKeys.CleanupResultOutputLabel] = ("  Output:", "  Output:");
        values[AppLanguageKeys.StartupFailureMessage] = (
            "MultiTool failed to start. Check the Logs folder next to the EXE for details.{0}{0}{1}",
            "MultiTool refused to pounce at startup. Check the Logs folder next to the EXE fur details.{0}{0}{1}");
        values[AppLanguageKeys.StartupErrorTitle] = ("MultiTool Startup Error", "MultiTool Startup Hiss");

        values[AppLanguageKeys.EnumCount] = ("Count", "Count");
        values[AppLanguageKeys.EnumCursor] = ("Cursor", "Paw-sor");
        values[AppLanguageKeys.EnumFixed] = ("Fixed", "Fixed");
        values[AppLanguageKeys.EnumLeft] = ("Left", "Left");
        values[AppLanguageKeys.EnumRight] = ("Right", "Right");
        values[AppLanguageKeys.EnumMiddle] = ("Middle", "Middle");
        values[AppLanguageKeys.EnumCustom] = ("Custom", "Custom");
        values[AppLanguageKeys.EnumSide1] = ("Side 1", "Side 1");
        values[AppLanguageKeys.EnumSide2] = ("Side 2", "Side 2");
        values[AppLanguageKeys.EnumSingle] = ("Single", "Single");
        values[AppLanguageKeys.EnumDouble] = ("Double", "Double");
        values[AppLanguageKeys.EnumHold] = ("Hold", "Hold");
        values[AppLanguageKeys.EnumRunOnce] = ("Run once", "Purr-lay once");
        values[AppLanguageKeys.EnumStartStop] = ("Start/stop", "Start/stop");
        values[AppLanguageKeys.EnumMoveMouse] = ("Move Mouse", "Move Mouse");
        values[AppLanguageKeys.EnumMouseDown] = ("Mouse Down", "Mouse Down");
        values[AppLanguageKeys.EnumMouseUp] = ("Mouse Up", "Mouse Up");
        values[AppLanguageKeys.EnumKeyDown] = ("Key Down", "Key Down");
        values[AppLanguageKeys.EnumKeyUp] = ("Key Up", "Key Up");

        values[AppLanguageKeys.RightMouseButton] = ("Right Mouse Button", "Right Mouse Button");
        values[AppLanguageKeys.MiddleMouseButton] = ("Middle Mouse Button", "Middle Mouse Button");
        values[AppLanguageKeys.MouseButton4] = ("Mouse Button 4", "Mouse Button 4");
        values[AppLanguageKeys.MouseButton5] = ("Mouse Button 5", "Mouse Button 5");

        values[AppLanguageKeys.DisplayRefreshFrequencySummaryNeedsChange] = (
            "{0}  -  Currently {1}, can go up to {2}",
            "{0}  -  Right meow at {1}, can zoomies up to {2}");
        values[AppLanguageKeys.DisplayRefreshFrequencySummaryBestAvailable] = (
            "{0}  -  Running at {1} (best available)",
            "{0}  -  Purring at {1} (best available)");
        values[AppLanguageKeys.DisplayRefreshDefaultFrequency] = ("Default", "Default");

        values[AppLanguageKeys.DriverClassificationOptional] = ("Optional", "Optional");
        values[AppLanguageKeys.DriverClassificationRecommended] = ("Recommended", "Recommended");
        values[AppLanguageKeys.DriverInstallFlowNeedsInteractive] = (
            "Needs Windows Update's own interactive install flow",
            "Needs Windows Update's own interactive install flow");
        values[AppLanguageKeys.DriverInstallFlowCanInstallDirectly] = (
            "Can install directly in MultiTool",
            "Can install directly in MultiTool");

        values[AppLanguageKeys.EmptyDirectoryHintNested] = (
            "Becomes empty after nested empty folders are removed.",
            "Becomes empty after nested empty folders are eaten.");
        values[AppLanguageKeys.EmptyDirectoryHintAlreadyEmpty] = (
            "Already empty.",
            "Already empty.");

        values[AppLanguageKeys.InstallerActionInstall] = ("Install", "Install");
        values[AppLanguageKeys.InstallerActionUpdate] = ("Update", "Update");
        values[AppLanguageKeys.InstallerActionRemove] = ("Remove", "Remove");
        values[AppLanguageKeys.InstallerActionInteractiveInstall] = ("Interactive Install", "Interactive Install");
        values[AppLanguageKeys.InstallerActionInteractiveUpdate] = ("Interactive Update", "Interactive Update");
        values[AppLanguageKeys.InstallerActionReinstall] = ("Reinstall", "Reinstall");
        values[AppLanguageKeys.InstallerActionRun] = ("Run", "Run");
        values[AppLanguageKeys.InstallerOperationHeaderFormat] = ("#{0} {1} {2}", "#{0} {1} {2}");
        values[AppLanguageKeys.InstallerStatusQueued] = ("Queued", "Queued");
        values[AppLanguageKeys.InstallerPackageHintHandledByMultiTool] = ("Handled by MultiTool", "Handled by MultiTool");
        values[AppLanguageKeys.InstallerPackageHintMicrosoftStoreApp] = ("Microsoft Store app", "Microsoft Store app");
        values[AppLanguageKeys.InstallerPackageHintOfficialSetupPage] = ("Official setup page", "Official setup page");
        values[AppLanguageKeys.InstallerPackageHintWindowsApp] = ("Windows app", "Windows app");
        values[AppLanguageKeys.InstallerPrimaryActionQueueUpdate] = ("Queue Update", "Queue Update");
        values[AppLanguageKeys.InstallerPrimaryActionQueueInstall] = ("Queue Install", "Queue Install");
        values[AppLanguageKeys.InstallerInteractiveActionUpdate] = ("Interactive Update", "Interactive Update");
        values[AppLanguageKeys.InstallerInteractiveActionInstall] = ("Interactive Install", "Interactive Install");
        values[AppLanguageKeys.InstallerPageActionOpenUpdatePage] = ("Open Update Page", "Open Update Page");
        values[AppLanguageKeys.InstallerPageActionOpenInstallPage] = ("Open Install Page", "Open Install Page");
        values[AppLanguageKeys.InstallerStatusChecking] = ("Checking status...", "Checking status...");
        values[AppLanguageKeys.InstallerCapabilityCustomFlow] = ("Custom flow", "Custom flow");
        values[AppLanguageKeys.InstallerCapabilityQuietWinget] = ("Quiet winget", "Quiet winget");
        values[AppLanguageKeys.InstallerCapabilityInteractiveOption] = ("Interactive option", "Interactive option");
        values[AppLanguageKeys.InstallerCapabilityReinstall] = ("Reinstall", "Reinstall");
        values[AppLanguageKeys.InstallerCapabilityOfficialPage] = ("Official page", "Official page");
        values[AppLanguageKeys.UsefulSiteBrowserLabelTor] = ("Tor Browser", "Tor Browser");
        values[AppLanguageKeys.UsefulSiteBrowserLabelDefault] = ("Default browser", "Default browser");

    }
}
