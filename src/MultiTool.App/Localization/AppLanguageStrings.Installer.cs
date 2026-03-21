namespace MultiTool.App.Localization;

public static partial class AppLanguageStrings
{
    private static void AddInstallerValues(Dictionary<string, (string English, string CatSpeak)> values)
    {
        values[AppLanguageKeys.InstallerStatusPreparingCatalog] = ("Preparing the installer catalog...", "Preparing the installer catalog...");
        values[AppLanguageKeys.InstallerEnvironmentDefault] = ("The installer tab uses winget for silent installs and updates.", "The installer tab uses winget for silent installs and updates.");
        values[AppLanguageKeys.InstallerAppUpdateSummaryDefault] = ("MultiTool release checks run with Check All Updates.", "MultiTool release checks run with Check All Updates.");
        values[AppLanguageKeys.InstallerStatusCleanupLoading] = ("Cleanup options are loading...", "Cleanup options are loading...");
        values[AppLanguageKeys.InstallerSetupFailedFormat] = ("Installer setup failed: {0}", "Installer setup failed: {0}");
        values[AppLanguageKeys.InstallerCheckingTrackedAppsFormat] = ("Checking {0} tracked app{1} for updates...", "Checking {0} tracked app{1} for updates...");
        values[AppLanguageKeys.InstallerSelectedRecommended] = ("Selected the recommended starter apps.", "Selected the recommended starter apps.");
        values[AppLanguageKeys.InstallerSelectedDeveloper] = ("Selected the developer stack.", "Selected the developer stack.");
        values[AppLanguageKeys.InstallerSelectionCleared] = ("Cleared the installer selection.", "Cleared the installer selection.");
        values[AppLanguageKeys.CleanupSelectedRecommended] = ("Selected the recommended cleanup apps.", "Selected the recommended cleanup apps.");
        values[AppLanguageKeys.CleanupSelectionCleared] = ("Cleared the cleanup selection.", "Cleared the cleanup selection.");
        values[AppLanguageKeys.InstallerNoOfficialPageFormat] = ("{0} does not have an official page linked yet.", "{0} does not have an official page linked yet.");
        values[AppLanguageKeys.InstallerOpenedOfficialPageFormat] = ("Opened {0}'s official page in {1}.", "Opened {0}'s official page in {1}.");
        values[AppLanguageKeys.InstallerOpenOfficialPageFailedFormat] = ("Unable to open {0}'s official page: {1}", "Unable to open {0}'s official page: {1}");
        values[AppLanguageKeys.CleanupSelectInstalledFirst] = ("Select at least one installed cleanup app first.", "Select at least one installed cleanup app first.");
        values[AppLanguageKeys.CleanupRemovingAppsFormat] = ("Removing {0} app{1}...", "Removing {0} app{1}...");
        values[AppLanguageKeys.CleanupFailedFormat] = ("Cleanup failed: {0}", "Cleanup failed: {0}");
        values[AppLanguageKeys.InstallerQueueFailedFormat] = ("Installer queue failed: {0}", "Installer queue failed: {0}");
        values[AppLanguageKeys.InstallerStatusInstalledUpdatesFormat] = ("{0} installed, {1} update{2} available.", "{0} installed, {1} update{2} available.");
        values[AppLanguageKeys.InstallerStatusInstalledScanDeferredFormat] = ("{0} installed. Update scan pending until you use Check All Updates.", "{0} installed. Update scan pending until you use Check All Updates.");
        values[AppLanguageKeys.CleanupStatusInstalledRemovableFormat] = ("{0} removable app{1} currently installed.", "{0} removable app{1} currently installed.");
        values[AppLanguageKeys.InstallerRefreshFailedFormat] = ("Unable to refresh installer status: {0}", "Unable to refresh installer status: {0}");
        values[AppLanguageKeys.InstallerSelectionSummaryFormat] = ("{0} selected  |  {1} installed  |  {2} updates ready", "{0} selected  |  {1} installed  |  {2} updates ready");
        values[AppLanguageKeys.InstallerSelectionSummaryPendingUpdatesFormat] = ("{0} selected  |  {1} installed  |  updates not scanned yet", "{0} selected  |  {1} installed  |  updates not scanned yet");
        values[AppLanguageKeys.InstallerUpdateSummaryInitial] = ("Use Check All Updates to scan every tracked app.", "Use Check All Updates to scan every tracked app.");
        values[AppLanguageKeys.InstallerUpdateSummaryUnavailable] = ("Update checks are unavailable until winget is available.", "Update checks are unavailable until winget is available.");
        values[AppLanguageKeys.InstallerUpdateCustomSuffixFormat] = (" Update All Ready also refreshes {0} custom app{1}.", " Update All Ready also refreshes {0} custom app{1}.");
        values[AppLanguageKeys.InstallerUpdateNoneFoundFormat] = ("No winget-tracked updates found.{0}", "No winget-tracked updates found.{0}");
        values[AppLanguageKeys.InstallerUpdateReadyListFormat] = ("Updates ready: {0}.{1}", "Updates ready: {0}.{1}");
        values[AppLanguageKeys.InstallerUpdateReadyMoreFormat] = ("Updates ready: {0}, +{1} more.{2}", "Updates ready: {0}, +{1} more.{2}");
        values[AppLanguageKeys.CleanupSelectionSummaryFormat] = ("{0} selected  |  {1} currently installed", "{0} selected  |  {1} currently installed");
        values[AppLanguageKeys.InstallerQueueSummaryFormat] = ("Queue: {0} queued  |  {1} running  |  {2} finished  |  {3} attention", "Queue: {0} queued  |  {1} running  |  {2} finished  |  {3} attention");
        values[AppLanguageKeys.InstallerQueueCompletionSummaryFormat] = ("{0} applied, {1} already current, {2} need attention.", "{0} applied, {1} already current, {2} need attention.");
        values[AppLanguageKeys.InstallerActionInstalling] = ("Installing", "Installing");
        values[AppLanguageKeys.InstallerActionUpdating] = ("Updating", "Updating");
        values[AppLanguageKeys.InstallerActionRemoving] = ("Removing", "Removing");
        values[AppLanguageKeys.InstallerActionInteractiveInstallRunningFor] = ("Running interactive install for", "Running interactive install for");
        values[AppLanguageKeys.InstallerActionInteractiveUpdateRunningFor] = ("Running interactive update for", "Running interactive update for");
        values[AppLanguageKeys.InstallerActionReinstalling] = ("Reinstalling", "Reinstalling");
        values[AppLanguageKeys.InstallerActionWorkingOn] = ("Working on", "Working on");
        values[AppLanguageKeys.InstallerActiveInstalling] = ("Installing...", "Installing...");
        values[AppLanguageKeys.InstallerActiveUpdating] = ("Updating...", "Updating...");
        values[AppLanguageKeys.InstallerActiveRemoving] = ("Removing...", "Removing...");
        values[AppLanguageKeys.InstallerActiveInteractiveInstall] = ("Interactive install running...", "Interactive install running...");
        values[AppLanguageKeys.InstallerActiveInteractiveUpdate] = ("Interactive update running...", "Interactive update running...");
        values[AppLanguageKeys.InstallerActiveReinstalling] = ("Reinstalling...", "Reinstalling...");
        values[AppLanguageKeys.InstallerActiveWorking] = ("Working...", "Working...");
        values[AppLanguageKeys.InstallerStatusUnavailable] = ("Status unavailable", "Status unavailable");
        values[AppLanguageKeys.InstallerUpdateCheckFailedFormat] = ("Unable to check for MultiTool updates: {0}", "Unable to check for MultiTool updates: {0}");
        values[AppLanguageKeys.InstallerSelectionAllSelectedAlreadyInstalled] = (
            "All selected apps are already installed.",
            "All selected apps are already installed.");
        values[AppLanguageKeys.InstallerSelectionNoUpdatesReady] = (
            "There are no apps with updates ready.",
            "There are no apps with updates ready.");
        values[AppLanguageKeys.InstallerAddedQueuedInstall] = ("Queued install.", "Queued install.");
        values[AppLanguageKeys.InstallerAddedQueuedUpdate] = ("Queued update.", "Queued update.");
        values[AppLanguageKeys.InstallerAddedQueuedInteractiveInstall] = (
            "Queued interactive install.",
            "Queued interactive install.");
        values[AppLanguageKeys.InstallerAddedQueuedInteractiveUpdate] = (
            "Queued interactive update.",
            "Queued interactive update.");
        values[AppLanguageKeys.InstallerAddedQueuedReinstall] = ("Queued reinstall.", "Queued reinstall.");
        values[AppLanguageKeys.InstallerSelectionNoInstalledReadyToUpdate] = (
            "There are no installed apps ready to update.",
            "There are no installed apps ready to update.");
        values[AppLanguageKeys.InstallerSelectionSelectAtLeastOneFirst] = (
            "Select at least one app first.",
            "Select at least one app first.");
        values[AppLanguageKeys.InstallerQueueSourceLabel] = ("Installer queue", "Installer queue");
        values[AppLanguageKeys.InstallerPackageStatusGuidedInstall] = ("Guided install", "Guided install");
        values[AppLanguageKeys.InstallerPackageStatusWingetUnavailable] = ("winget unavailable", "winget unavailable");
        values[AppLanguageKeys.InstallerStatusGuidedAppsCanOpenOfficialPagesFormat] = (
            "{0} {1} guided app{2} can still open official setup pages.",
            "{0} {1} guided app{2} can still open official setup pages.");
        values[AppLanguageKeys.InstallerPackageStatusQueuedSequenceFormat] = ("#{0} queued", "#{0} queued");
        values[AppLanguageKeys.InstallerStatusSourceAlreadyQueuedOrRunningFormat] = (
            "{0}: that action is already queued or running.",
            "{0}: that action is already queued or running.");
        values[AppLanguageKeys.InstallerStatusSourceSelectedAppsAlreadyInstalledFormat] = (
            "{0}: selected app{1} already installed.",
            "{0}: selected app{1} already installed.");
        values[AppLanguageKeys.InstallerStatusSourceNothingNewAddedFormat] = (
            "{0}: nothing new was added to the installer queue.",
            "{0}: nothing new was added to the installer queue.");
        values[AppLanguageKeys.InstallerSuffixSkippedDuplicateRequestsFormat] = (
            "Skipped {0} duplicate request{1}",
            "Skipped {0} duplicate request{1}");
        values[AppLanguageKeys.InstallerSuffixSkippedAlreadyInstalledAppsFormat] = (
            "Skipped {0} already installed app{1}",
            "Skipped {0} already installed app{1}");
        values[AppLanguageKeys.InstallerOperationNoResult] = (
            "The installer did not return a result.",
            "The installer did not return a result.");
        values[AppLanguageKeys.InstallerOperationNoResultGuidance] = (
            "Check the activity log, then try the action again.",
            "Check the activity log, then try the action again.");
        values[AppLanguageKeys.InstallerLogEdgeWingetFailedTryingFallback] = (
            "Microsoft Edge uninstall through winget failed. Trying the elevated Edge removal fallback...",
            "Microsoft Edge uninstall through winget failed. Trying the elevated Edge removal fallback...");
        values[AppLanguageKeys.InstallerEdgeDisplayName] = ("Microsoft Edge", "Microsoft Edge");
        values[AppLanguageKeys.InstallerEdgeFallbackMessageFormat] = (
            "{0} (Fallback: Edge-specific elevated removal)",
            "{0} (Fallback: Edge-specific elevated removal)");
        values[AppLanguageKeys.InstallerEdgeFallbackGuidanceRunAsAdminRetry] = (
            "Run MultiTool as administrator and retry Edge removal.",
            "Run MultiTool as administrator and retry Edge removal.");
        values[AppLanguageKeys.InstallerLogResultDisplayMessageFormat] = ("{0}: {1}", "{0}: {1}");
        values[AppLanguageKeys.CleanupSummaryCountsFormat] = (
            "{0} removed, {1} already gone, {2} failed.",
            "{0} removed, {1} already gone, {2} failed.");
        values[AppLanguageKeys.CleanupSummaryManualStepsFormat] = (
            " {0} require a manual step.",
            " {0} require a manual step.");
        values[AppLanguageKeys.CleanupSummaryFirstFailureFormat] = (
            " First failure: {0} - {1}",
            " First failure: {0} - {1}");
        values[AppLanguageKeys.CleanupSummaryNextStepFormat] = (
            " Next step: {0}",
            " Next step: {0}");
        values[AppLanguageKeys.CleanupSummaryFailureLogPathFormat] = (
            " Failure log: {0}",
            " Failure log: {0}");
        values[AppLanguageKeys.CleanupLogFailureLogPathFormat] = (
            "Cleanup failure log: {0}",
            "Cleanup failure log: {0}");
        values[AppLanguageKeys.InstallerFirefoxAddonsLabel] = ("Firefox add-ons", "Firefox add-ons");
        values[AppLanguageKeys.InstallerLogFirefoxAddonsSkippedBecauseInstallFailed] = (
            "Firefox add-ons skipped because Firefox did not install cleanly.",
            "Firefox add-ons skipped because Firefox did not install cleanly.");
        values[AppLanguageKeys.InstallerLogUpdateInfoWithUrlFormat] = ("{0} {1}", "{0} {1}");
        values[AppLanguageKeys.InstallerSupplementalSummaryFormat] = (
            " {0}: {1} applied, {2} already ready, {3} failed.",
            " {0}: {1} applied, {2} already ready, {3} failed.");
        values[AppLanguageKeys.InstallerLogGuidanceSuffixFormat] = (" Next: {0}", " Next: {0}");
        values[AppLanguageKeys.InstallerLogOperationResultFormat] = ("#{0} {1}: {2}{3}", "#{0} {1}: {2}{3}");
        values[AppLanguageKeys.InstallerLogOpenedOfficialPageInBrowserFormat] = (
            "{0}: opened {1} in {2}.",
            "{0}: opened {1} in {2}.");
        values[AppLanguageKeys.InstallerProgressTextFormat] = (
            "{0} {1} [{2}/{3}]...",
            "{0} {1} [{2}/{3}]...");
        values[AppLanguageKeys.InstallerQueuedBatchMessageFormat] = (
            "Queued {0} {1}.",
            "Queued {0} {1}.");
        values[AppLanguageKeys.InstallerActionNounInstallSingular] = ("install", "install");
        values[AppLanguageKeys.InstallerActionNounInstallPlural] = ("installs", "installs");
        values[AppLanguageKeys.InstallerActionNounUpdateSingular] = ("update", "update");
        values[AppLanguageKeys.InstallerActionNounUpdatePlural] = ("updates", "updates");
        values[AppLanguageKeys.InstallerActionNounRemovalSingular] = ("removal", "removal");
        values[AppLanguageKeys.InstallerActionNounRemovalPlural] = ("removals", "removals");
        values[AppLanguageKeys.InstallerActionNounTaskSingular] = ("task", "task");
        values[AppLanguageKeys.InstallerActionNounTaskPlural] = ("tasks", "tasks");
        values[AppLanguageKeys.InstallerPluralS] = ("s", "s");
        values[AppLanguageKeys.InstallerPluralIs] = (" is", " is");
        values[AppLanguageKeys.InstallerPluralAre] = ("s are", "s are");

    }
}
