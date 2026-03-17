using System.Globalization;
using AutoClicker.App.ViewModels;

namespace AutoClicker.App.Localization;

public enum AppLanguage
{
    English,
    CatSpeak,
}

public static class AppLanguageKeys
{
    public const string CleanupUninstallFailureTitle = "CleanupUninstallFailureTitle";
    public const string CleanupUninstallExceptionTitle = "CleanupUninstallExceptionTitle";
    public const string CleanupUninstallTimestampFormat = "CleanupUninstallTimestampFormat";
    public const string CleanupUninstallSelectedCountFormat = "CleanupUninstallSelectedCountFormat";
    public const string CleanupUninstallSelectedPackages = "CleanupUninstallSelectedPackages";
    public const string CleanupUninstallOperationResults = "CleanupUninstallOperationResults";
    public const string CleanupUninstallException = "CleanupUninstallException";
    public const string CleanupResultSucceededFormat = "CleanupResultSucceededFormat";
    public const string CleanupResultChangedFormat = "CleanupResultChangedFormat";
    public const string CleanupResultRequiresManualStepFormat = "CleanupResultRequiresManualStepFormat";
    public const string CleanupResultMessageFormat = "CleanupResultMessageFormat";
    public const string CleanupResultGuidanceFormat = "CleanupResultGuidanceFormat";
    public const string CleanupResultOutputLabel = "CleanupResultOutputLabel";
    
    public const string StartupFailureMessage = "StartupFailureMessage";
    public const string StartupErrorTitle = "StartupErrorTitle";

    public const string EnumCount = "EnumCount";
    public const string EnumCursor = "EnumCursor";
    public const string EnumFixed = "EnumFixed";
    public const string EnumLeft = "EnumLeft";
    public const string EnumRight = "EnumRight";
    public const string EnumMiddle = "EnumMiddle";
    public const string EnumCustom = "EnumCustom";
    public const string EnumSide1 = "EnumSide1";
    public const string EnumSide2 = "EnumSide2";
    public const string EnumSingle = "EnumSingle";
    public const string EnumDouble = "EnumDouble";
    public const string EnumHold = "EnumHold";
    public const string EnumRunOnce = "EnumRunOnce";
    public const string EnumStartStop = "EnumStartStop";
    public const string EnumMoveMouse = "EnumMoveMouse";
    public const string EnumMouseDown = "EnumMouseDown";
    public const string EnumMouseUp = "EnumMouseUp";
    public const string EnumKeyDown = "EnumKeyDown";
    public const string EnumKeyUp = "EnumKeyUp";

    public const string RightMouseButton = "RightMouseButton";
    public const string MiddleMouseButton = "MiddleMouseButton";
    public const string MouseButton4 = "MouseButton4";
    public const string MouseButton5 = "MouseButton5";

    public const string DisplayRefreshFrequencySummaryNeedsChange = "DisplayRefreshFrequencySummaryNeedsChange";
    public const string DisplayRefreshFrequencySummaryBestAvailable = "DisplayRefreshFrequencySummaryBestAvailable";
    public const string DisplayRefreshDefaultFrequency = "DisplayRefreshDefaultFrequency";

    public const string DriverClassificationOptional = "DriverClassificationOptional";
    public const string DriverClassificationRecommended = "DriverClassificationRecommended";
    public const string DriverInstallFlowNeedsInteractive = "DriverInstallFlowNeedsInteractive";
    public const string DriverInstallFlowCanInstallDirectly = "DriverInstallFlowCanInstallDirectly";

    public const string EmptyDirectoryHintNested = "EmptyDirectoryHintNested";
    public const string EmptyDirectoryHintAlreadyEmpty = "EmptyDirectoryHintAlreadyEmpty";

    public const string InstallerActionInstall = "InstallerActionInstall";
    public const string InstallerActionUpdate = "InstallerActionUpdate";
    public const string InstallerActionRemove = "InstallerActionRemove";
    public const string InstallerActionInteractiveInstall = "InstallerActionInteractiveInstall";
    public const string InstallerActionInteractiveUpdate = "InstallerActionInteractiveUpdate";
    public const string InstallerActionReinstall = "InstallerActionReinstall";
    public const string InstallerActionRun = "InstallerActionRun";
    public const string InstallerOperationHeaderFormat = "InstallerOperationHeaderFormat";
    public const string InstallerStatusQueued = "InstallerStatusQueued";
    public const string InstallerPackageHintHandledByMultiTool = "InstallerPackageHintHandledByMultiTool";
    public const string InstallerPackageHintMicrosoftStoreApp = "InstallerPackageHintMicrosoftStoreApp";
    public const string InstallerPackageHintOfficialSetupPage = "InstallerPackageHintOfficialSetupPage";
    public const string InstallerPackageHintWindowsApp = "InstallerPackageHintWindowsApp";
    public const string InstallerPrimaryActionQueueUpdate = "InstallerPrimaryActionQueueUpdate";
    public const string InstallerPrimaryActionQueueInstall = "InstallerPrimaryActionQueueInstall";
    public const string InstallerInteractiveActionUpdate = "InstallerInteractiveActionUpdate";
    public const string InstallerInteractiveActionInstall = "InstallerInteractiveActionInstall";
    public const string InstallerPageActionOpenUpdatePage = "InstallerPageActionOpenUpdatePage";
    public const string InstallerPageActionOpenInstallPage = "InstallerPageActionOpenInstallPage";
    public const string InstallerStatusChecking = "InstallerStatusChecking";
    public const string InstallerPackagePickerDescription = "InstallerPackagePickerDescription";
    public const string InstallerSearchLabel = "InstallerSearchLabel";
    public const string InstallerCapabilityCustomFlow = "InstallerCapabilityCustomFlow";
    public const string InstallerCapabilityQuietWinget = "InstallerCapabilityQuietWinget";
    public const string InstallerCapabilityInteractiveOption = "InstallerCapabilityInteractiveOption";
    public const string InstallerCapabilityReinstall = "InstallerCapabilityReinstall";
    public const string InstallerCapabilityOfficialPage = "InstallerCapabilityOfficialPage";
    public const string UsefulSiteBrowserLabelTor = "UsefulSiteBrowserLabelTor";
    public const string UsefulSiteBrowserLabelDefault = "UsefulSiteBrowserLabelDefault";

    public const string AppearanceHelperText = "AppearanceHelperText";
    public const string ClickerTabHeader = "ClickerTabHeader";
    public const string ClickerHotkeyLabel = "ClickerHotkeyLabel";
    public const string ScreenshotTabHeader = "ScreenshotTabHeader";
    public const string MacroTabHeader = "MacroTabHeader";
    public const string MacroSetShortcutButton = "MacroSetShortcutButton";
    public const string MacroEditShortcutsButton = "MacroEditShortcutsButton";
    public const string ToolsTabHeader = "ToolsTabHeader";
    public const string NicheToolsHeader = "NicheToolsHeader";
    public const string ShortcutKeyExplorerHeader = "ShortcutKeyExplorerHeader";
    public const string ShortcutExplorerButton = "ShortcutExplorerButton";
    public const string InstallerTabHeader = "InstallerTabHeader";
    public const string SettingsTabHeader = "SettingsTabHeader";
    public const string AppearanceHeader = "AppearanceHeader";
    public const string DarkModeLabel = "DarkModeLabel";
    public const string AlwaysOnTopLabel = "AlwaysOnTopLabel";
    public const string CatTranslatorLabel = "CatTranslatorLabel";
    public const string AutoHideOnStartupLabel = "AutoHideOnStartupLabel";
    public const string ResetAllSettingsButton = "ResetAllSettingsButton";
    public const string BugCheckingHeader = "BugCheckingHeader";
    public const string BugCheckingHelperText = "BugCheckingHelperText";
    public const string CopyLogButton = "CopyLogButton";
    public const string MainAdminBannerAdmin = "MainAdminBannerAdmin";
    public const string MainAdminBannerNotAdmin = "MainAdminBannerNotAdmin";
    public const string MainAdminActivityNotAdmin = "MainAdminActivityNotAdmin";
    public const string MainWindowTitleDefault = "MainWindowTitleDefault";
    public const string MainWindowTitleRunning = "MainWindowTitleRunning";
    public const string MainWindowTitleNotAdminSuffix = "MainWindowTitleNotAdminSuffix";
    public const string MainStatusLoadingSettings = "MainStatusLoadingSettings";
    public const string MainCustomKeyPrompt = "MainCustomKeyPrompt";
    public const string MainCustomKeyOrMousePrompt = "MainCustomKeyOrMousePrompt";
    public const string HotkeyEditToolTip = "HotkeyEditToolTip";
    public const string HotkeyLabel = "HotkeyLabel";
    public const string CaptureButton = "CaptureButton";
    public const string BrowseButton = "BrowseButton";
    public const string CaptureScreenButton = "CaptureScreenButton";
    public const string OpenFolderButton = "OpenFolderButton";
    public const string NewMacroButton = "NewMacroButton";
    public const string RecordButton = "RecordButton";
    public const string StopButton = "StopButton";
    public const string PlayButton = "PlayButton";
    public const string SaveButton = "SaveButton";
    public const string LoadButton = "LoadButton";
    public const string RefreshButton = "RefreshButton";
    public const string LoadSelectedButton = "LoadSelectedButton";
    public const string EditSelectedButton = "EditSelectedButton";
    public const string MainMouseButtonLeft = "MainMouseButtonLeft";
    public const string MainMouseButtonRight = "MainMouseButtonRight";
    public const string MainMouseButtonMiddle = "MainMouseButtonMiddle";
    public const string MainMouseButton4 = "MainMouseButton4";
    public const string MainMouseButton5 = "MainMouseButton5";
    public const string MainScreenshotFilePrefixDefault = "MainScreenshotFilePrefixDefault";
    public const string MainIntervalLabel = "MainIntervalLabel";
    public const string MainHoursLabel = "MainHoursLabel";
    public const string MainMinutesLabel = "MainMinutesLabel";
    public const string MainSecondsLabel = "MainSecondsLabel";
    public const string MainMillisecondsLabel = "MainMillisecondsLabel";
    public const string MainRepeatLabel = "MainRepeatLabel";
    public const string MainPositionLabel = "MainPositionLabel";
    public const string MainXLabel = "MainXLabel";
    public const string MainYLabel = "MainYLabel";
    public const string MainNameLabel = "MainNameLabel";
    public const string MainPlayCountLabel = "MainPlayCountLabel";
    public const string MainRecordMouseMovementLabel = "MainRecordMouseMovementLabel";
    public const string MainPlayHotkeyLabel = "MainPlayHotkeyLabel";
    public const string MainRecordHotkeyLabel = "MainRecordHotkeyLabel";
    public const string MainSavedLabel = "MainSavedLabel";
    public const string MainNoAssignedMacroShortcuts = "MainNoAssignedMacroShortcuts";
    public const string MainInputLabel = "MainInputLabel";
    public const string MainTypeLabel = "MainTypeLabel";
    public const string MainCustomInputLabel = "MainCustomInputLabel";
    public const string MainFolderLabel = "MainFolderLabel";
    public const string MainPrefixLabel = "MainPrefixLabel";
    public const string MainScreenshotHelperText = "MainScreenshotHelperText";
    public const string MainScreenshotStatusReady = "MainScreenshotStatusReady";
    public const string MainLatestScreenshotNone = "MainLatestScreenshotNone";
    public const string MainLatestVideoNone = "MainLatestVideoNone";
    public const string MainLatestScreenshotPlaceholder = "MainLatestScreenshotPlaceholder";
    public const string MouseSensitivityVerySlow = "MouseSensitivityVerySlow";
    public const string MouseSensitivitySlow = "MouseSensitivitySlow";
    public const string MouseSensitivityBalanced = "MouseSensitivityBalanced";
    public const string MouseSensitivityFast = "MouseSensitivityFast";
    public const string MouseSensitivityVeryFast = "MouseSensitivityVeryFast";
    public const string MainMacroNameDefault = "MainMacroNameDefault";
    public const string MainMacroLogReady = "MainMacroLogReady";
    public const string MainMacroLogNoRecordedYet = "MainMacroLogNoRecordedYet";
    public const string MainMacroSummaryNoRecorded = "MainMacroSummaryNoRecorded";
    public const string MainMacroStatusReady = "MainMacroStatusReady";
    public const string MainSettingsStatusInitial = "MainSettingsStatusInitial";
    public const string MainSettingsStatusDarkModeOn = "MainSettingsStatusDarkModeOn";
    public const string MainSettingsStatusDarkModeOff = "MainSettingsStatusDarkModeOff";
    public const string MainActivityDarkModeEnabled = "MainActivityDarkModeEnabled";
    public const string MainActivityDarkModeDisabled = "MainActivityDarkModeDisabled";
    public const string MainSettingsStatusCtrlWheelZoomOn = "MainSettingsStatusCtrlWheelZoomOn";
    public const string MainSettingsStatusCtrlWheelZoomOff = "MainSettingsStatusCtrlWheelZoomOff";
    public const string MainActivityCtrlWheelZoomEnabled = "MainActivityCtrlWheelZoomEnabled";
    public const string MainActivityCtrlWheelZoomDisabled = "MainActivityCtrlWheelZoomDisabled";
    public const string MainSettingsStatusAutoHideOn = "MainSettingsStatusAutoHideOn";
    public const string MainSettingsStatusAutoHideOff = "MainSettingsStatusAutoHideOff";
    public const string MainActivityAutoHideEnabled = "MainActivityAutoHideEnabled";
    public const string MainActivityAutoHideDisabled = "MainActivityAutoHideDisabled";
    public const string MainSettingsStatusCatTranslatorOn = "MainSettingsStatusCatTranslatorOn";
    public const string MainActivityCatTranslatorEnabled = "MainActivityCatTranslatorEnabled";
    public const string MainActivityCatTranslatorDisabled = "MainActivityCatTranslatorDisabled";
    public const string MainStatusReady = "MainStatusReady";
    public const string MainActivityLogReady = "MainActivityLogReady";
    public const string MainActivitySettingsLoaded = "MainActivitySettingsLoaded";
    public const string MainStatusClicking = "MainStatusClicking";
    public const string MainStatusAutomationStopped = "MainStatusAutomationStopped";
    public const string MainStatusSettingsSaved = "MainStatusSettingsSaved";
    public const string MainStatusCapturedCoordinatesFormat = "MainStatusCapturedCoordinatesFormat";
    public const string MainScreenshotStatusOpenedFolder = "MainScreenshotStatusOpenedFolder";
    public const string MainScreenshotStatusOpenFolderFailedFormat = "MainScreenshotStatusOpenFolderFailedFormat";
    public const string MainMacroStatusStartedNew = "MainMacroStatusStartedNew";
    public const string MainMacroSummaryRecordingFormat = "MainMacroSummaryRecordingFormat";
    public const string MainMacroStatusRecordingWithMouseFormat = "MainMacroStatusRecordingWithMouseFormat";
    public const string MainMacroStatusRecordingWithoutMouseFormat = "MainMacroStatusRecordingWithoutMouseFormat";
    public const string MainMacroLogStartedRecordingFormat = "MainMacroLogStartedRecordingFormat";
    public const string MainMacroStatusStartRecordingFailedFormat = "MainMacroStatusStartRecordingFailedFormat";
    public const string MainMacroStatusCannotRecordWhilePlaying = "MainMacroStatusCannotRecordWhilePlaying";
    public const string MainMacroLogRecordHotkeyIgnoredPlaying = "MainMacroLogRecordHotkeyIgnoredPlaying";
    public const string MainMacroSummaryRecordedFormat = "MainMacroSummaryRecordedFormat";
    public const string MainMacroSummaryNoInputCapturedFormat = "MainMacroSummaryNoInputCapturedFormat";
    public const string MainMacroStatusStoppedRecordingFormat = "MainMacroStatusStoppedRecordingFormat";
    public const string MainMacroStatusStoppedRecordingNoInputFormat = "MainMacroStatusStoppedRecordingNoInputFormat";
    public const string MainMacroLogStoppedRecordingFormat = "MainMacroLogStoppedRecordingFormat";
    public const string MainMacroStatusStopRecordingFailedFormat = "MainMacroStatusStopRecordingFailedFormat";
    public const string MainMacroStatusNoRecordedToPlay = "MainMacroStatusNoRecordedToPlay";
    public const string MainMacroLogPlaybackRequestedNoMacro = "MainMacroLogPlaybackRequestedNoMacro";
    public const string MainMacroStatusPlayingFormat = "MainMacroStatusPlayingFormat";
    public const string MainMacroStatusFinishedPlayingFormat = "MainMacroStatusFinishedPlayingFormat";
    public const string MainMacroStatusPlayFailedFormat = "MainMacroStatusPlayFailedFormat";
    public const string MainMacroStatusNoRecordedToSave = "MainMacroStatusNoRecordedToSave";
    public const string MainMacroLogSaveRequestedNoMacro = "MainMacroLogSaveRequestedNoMacro";
    public const string MainMacroStatusSaveCanceled = "MainMacroStatusSaveCanceled";
    public const string MainMacroStatusSavedToMacrosFormat = "MainMacroStatusSavedToMacrosFormat";
    public const string MainMacroLogSavedToPathFormat = "MainMacroLogSavedToPathFormat";
    public const string MainMacroStatusSaveFailedFormat = "MainMacroStatusSaveFailedFormat";
    public const string MainMacroStatusLoadCanceled = "MainMacroStatusLoadCanceled";
    public const string MainMacroStatusLoadedFromFileFormat = "MainMacroStatusLoadedFromFileFormat";
    public const string MainMacroLogLoadedFromPathFormat = "MainMacroLogLoadedFromPathFormat";
    public const string MainMacroStatusLoadFailedFormat = "MainMacroStatusLoadFailedFormat";
    public const string MainMacroStatusChooseSavedFirst = "MainMacroStatusChooseSavedFirst";
    public const string MainMacroLogLoadSelectedNoSaved = "MainMacroLogLoadSelectedNoSaved";
    public const string MainMacroStatusLoadedSavedFormat = "MainMacroStatusLoadedSavedFormat";
    public const string MainMacroLogLoadedSavedPathFormat = "MainMacroLogLoadedSavedPathFormat";
    public const string MainMacroStatusLoadSavedFailedFormat = "MainMacroStatusLoadSavedFailedFormat";
    public const string MainMacroLogEditRequestedNoSaved = "MainMacroLogEditRequestedNoSaved";
    public const string MainMacroStatusEditCanceled = "MainMacroStatusEditCanceled";
    public const string MainMacroStatusSavedEditsFormat = "MainMacroStatusSavedEditsFormat";
    public const string MainMacroLogSavedEditedToPathFormat = "MainMacroLogSavedEditedToPathFormat";
    public const string MainMacroStatusEditSavedFailedFormat = "MainMacroStatusEditSavedFailedFormat";
    public const string MainMacroStatusFoundSavedFormat = "MainMacroStatusFoundSavedFormat";
    public const string MainMacroStatusNoSavedInDefaultFolder = "MainMacroStatusNoSavedInDefaultFolder";
    public const string MainMacroStatusSavedFolderUnavailableFormat = "MainMacroStatusSavedFolderUnavailableFormat";
    public const string MainMacroStatusOpenedSavedFolder = "MainMacroStatusOpenedSavedFolder";
    public const string MainMacroStatusOpenSavedFolderFailedFormat = "MainMacroStatusOpenSavedFolderFailedFormat";
    public const string MainMacroStatusLogCleared = "MainMacroStatusLogCleared";
    public const string MainStatusSettingsAutoSaved = "MainStatusSettingsAutoSaved";
    public const string MainStatusCustomKeySetFormat = "MainStatusCustomKeySetFormat";
    public const string MainScreenshotStatusHotkeySetFormat = "MainScreenshotStatusHotkeySetFormat";
    public const string MainStatusClickerHotkeySetFormat = "MainStatusClickerHotkeySetFormat";
    public const string MainMacroStatusHotkeySetFormat = "MainMacroStatusHotkeySetFormat";
    public const string MainMacroStatusRecordHotkeySetFormat = "MainMacroStatusRecordHotkeySetFormat";
    public const string MainStatusCustomInputSetFormat = "MainStatusCustomInputSetFormat";
    public const string MainScreenshotFolderPickerPrompt = "MainScreenshotFolderPickerPrompt";
    public const string MainScreenshotStatusFolderSelectionCanceled = "MainScreenshotStatusFolderSelectionCanceled";
    public const string MainScreenshotStatusFolderSetFormat = "MainScreenshotStatusFolderSetFormat";
    public const string MainActivityLogEmpty = "MainActivityLogEmpty";
    public const string MainSettingsStatusCopiedActivityLog = "MainSettingsStatusCopiedActivityLog";
    public const string MainSettingsStatusResetRequested = "MainSettingsStatusResetRequested";
    public const string MainSettingsStatusResetCompleted = "MainSettingsStatusResetCompleted";
    public const string MainSettingsStatusResetSaveFailed = "MainSettingsStatusResetSaveFailed";
    public const string MainScreenshotStatusSavedVideoFormat = "MainScreenshotStatusSavedVideoFormat";
    public const string MainScreenshotStatusVideoHandledInOptionsWindow = "MainScreenshotStatusVideoHandledInOptionsWindow";
    public const string MainScreenshotStatusOptionsCanceled = "MainScreenshotStatusOptionsCanceled";
    public const string MainScreenshotStatusSavedAndCopiedFormat = "MainScreenshotStatusSavedAndCopiedFormat";
    public const string MainScreenshotLogSavedFullScreenFormat = "MainScreenshotLogSavedFullScreenFormat";
    public const string MainScreenshotStatusAreaCanceled = "MainScreenshotStatusAreaCanceled";
    public const string MainScreenshotLogSavedAreaFormat = "MainScreenshotLogSavedAreaFormat";
    public const string MainScreenshotStatusFailedFormat = "MainScreenshotStatusFailedFormat";
    public const string MainStatusUnableSaveSettingsFormat = "MainStatusUnableSaveSettingsFormat";
    public const string TrayStartupHiddenStatus = "TrayStartupHiddenStatus";
    public const string TrayMinimizedStatus = "TrayMinimizedStatus";
    public const string HotkeysRegisterWhenReadyStatus = "HotkeysRegisterWhenReadyStatus";

    public const string AboutWindowTitle = "AboutWindowTitle";
    public const string AboutSubtitle = "AboutSubtitle";
    public const string AboutCloseButton = "AboutCloseButton";
    public const string AboutVersionFormat = "AboutVersionFormat";

    public const string CoordinateCaptureInstruction = "CoordinateCaptureInstruction";
    public const string CoordinateCaptureEscHint = "CoordinateCaptureEscHint";
    public const string CoordinateCapturePositionFormat = "CoordinateCapturePositionFormat";

    public const string AreaSelectionInstruction = "AreaSelectionInstruction";
    public const string AreaSelectionEscHint = "AreaSelectionEscHint";

    public const string HotkeySettingsTitle = "HotkeySettingsTitle";
    public const string HotkeySettingsCaptureTooltip = "HotkeySettingsCaptureTooltip";
    public const string HotkeySettingsToggleLabel = "HotkeySettingsToggleLabel";
    public const string HotkeySettingsPinWindowLabel = "HotkeySettingsPinWindowLabel";
    public const string HotkeySettingsWaitingAnyKey = "HotkeySettingsWaitingAnyKey";
    public const string HotkeySettingsWaitingKey = "HotkeySettingsWaitingKey";
    public const string HotkeySettingsResetButton = "HotkeySettingsResetButton";
    public const string HotkeySettingsCancelButton = "HotkeySettingsCancelButton";
    public const string HotkeySettingsSaveButton = "HotkeySettingsSaveButton";

    public const string MacroEditorTitle = "MacroEditorTitle";
    public const string MacroEditorNameLabel = "MacroEditorNameLabel";
    public const string MacroEditorDescription = "MacroEditorDescription";
    public const string MacroEditorEventsHeader = "MacroEditorEventsHeader";
    public const string MacroEditorPickEventHint = "MacroEditorPickEventHint";
    public const string MacroEditorAddEventButton = "MacroEditorAddEventButton";
    public const string MacroEditorRemoveSelectedButton = "MacroEditorRemoveSelectedButton";
    public const string MacroEditorSortByOffsetButton = "MacroEditorSortByOffsetButton";
    public const string MacroEditorColumnNumber = "MacroEditorColumnNumber";
    public const string MacroEditorColumnOffset = "MacroEditorColumnOffset";
    public const string MacroEditorColumnAction = "MacroEditorColumnAction";
    public const string MacroEditorColumnDetails = "MacroEditorColumnDetails";
    public const string MacroEditorSelectedEventHeader = "MacroEditorSelectedEventHeader";
    public const string MacroEditorSelectedEventNull = "MacroEditorSelectedEventNull";
    public const string MacroEditorActionLabel = "MacroEditorActionLabel";
    public const string MacroEditorOffsetLabel = "MacroEditorOffsetLabel";
    public const string MacroEditorKeyCodeLabel = "MacroEditorKeyCodeLabel";
    public const string MacroEditorMouseButtonLabel = "MacroEditorMouseButtonLabel";
    public const string MacroEditorXPositionLabel = "MacroEditorXPositionLabel";
    public const string MacroEditorYPositionLabel = "MacroEditorYPositionLabel";
    public const string MacroEditorFieldHint = "MacroEditorFieldHint";
    public const string MacroEditorCancelButton = "MacroEditorCancelButton";
    public const string MacroEditorSaveButton = "MacroEditorSaveButton";
    public const string MacroEditorStatusInitial = "MacroEditorStatusInitial";
    public const string MacroEditorStatusAdded = "MacroEditorStatusAdded";
    public const string MacroEditorStatusRemoved = "MacroEditorStatusRemoved";
    public const string MacroEditorStatusSorted = "MacroEditorStatusSorted";
    public const string MacroEditorErrorEnterName = "MacroEditorErrorEnterName";
    public const string MacroEditorSummaryEventsFormat = "MacroEditorSummaryEventsFormat";
    public const string MacroEditorSelectedHintNone = "MacroEditorSelectedHintNone";
    public const string MacroEditorSelectedHintKeyboard = "MacroEditorSelectedHintKeyboard";
    public const string MacroEditorSelectedHintMouseMove = "MacroEditorSelectedHintMouseMove";
    public const string MacroEditorSelectedHintMouseButton = "MacroEditorSelectedHintMouseButton";

    public const string MacroHotkeyAssignmentsTitle = "MacroHotkeyAssignmentsTitle";
    public const string MacroHotkeyAssignmentsHeading = "MacroHotkeyAssignmentsHeading";
    public const string MacroHotkeyAssignmentsDescription = "MacroHotkeyAssignmentsDescription";
    public const string MacroHotkeyAssignmentsEmpty = "MacroHotkeyAssignmentsEmpty";
    public const string MacroHotkeyAssignmentsActive = "MacroHotkeyAssignmentsActive";
    public const string MacroHotkeyAssignmentsShortcutKey = "MacroHotkeyAssignmentsShortcutKey";
    public const string MacroHotkeyAssignmentsBehavior = "MacroHotkeyAssignmentsBehavior";
    public const string MacroHotkeyAssignmentsRemoveKey = "MacroHotkeyAssignmentsRemoveKey";
    public const string MacroHotkeyAssignmentsClear = "MacroHotkeyAssignmentsClear";
    public const string MacroHotkeyAssignmentsCancel = "MacroHotkeyAssignmentsCancel";
    public const string MacroHotkeyAssignmentsSave = "MacroHotkeyAssignmentsSave";
    public const string MacroHotkeyAssignmentsCaptureTooltip = "MacroHotkeyAssignmentsCaptureTooltip";
    public const string MacroHotkeyAssignmentsClickToAssign = "MacroHotkeyAssignmentsClickToAssign";
    public const string MacroHotkeyAssignmentsStatusNoSaved = "MacroHotkeyAssignmentsStatusNoSaved";
    public const string MacroHotkeyAssignmentsStatusPick = "MacroHotkeyAssignmentsStatusPick";
    public const string MacroHotkeyAssignmentsStatusAssignedFormat = "MacroHotkeyAssignmentsStatusAssignedFormat";
    public const string MacroHotkeyAssignmentsStatusRemovedFormat = "MacroHotkeyAssignmentsStatusRemovedFormat";

    public const string MacroNamePromptTitle = "MacroNamePromptTitle";
    public const string MacroNamePromptHeading = "MacroNamePromptHeading";
    public const string MacroNamePromptDescription = "MacroNamePromptDescription";
    public const string MacroNamePromptNameLabel = "MacroNamePromptNameLabel";
    public const string MacroNamePromptSaveToLabel = "MacroNamePromptSaveToLabel";
    public const string MacroNamePromptErrorEnterName = "MacroNamePromptErrorEnterName";
    public const string MacroNamePromptOverwriteHint = "MacroNamePromptOverwriteHint";
    public const string MacroNamePromptCancel = "MacroNamePromptCancel";
    public const string MacroNamePromptSave = "MacroNamePromptSave";
    public const string MacroNamePromptNameTooltip = "MacroNamePromptNameTooltip";
    public const string MacroNamePromptDefaultName = "MacroNamePromptDefaultName";
    public const string MacroNamePromptSavePreviewFormat = "MacroNamePromptSavePreviewFormat";

    public const string ShortcutExplorerTitle = "ShortcutExplorerTitle";
    public const string ShortcutExplorerHeading = "ShortcutExplorerHeading";
    public const string ShortcutExplorerSearchLabel = "ShortcutExplorerSearchLabel";
    public const string ShortcutExplorerConflictsOnly = "ShortcutExplorerConflictsOnly";
    public const string ShortcutExplorerColumnHotkey = "ShortcutExplorerColumnHotkey";
    public const string ShortcutExplorerColumnShortcut = "ShortcutExplorerColumnShortcut";
    public const string ShortcutExplorerColumnSource = "ShortcutExplorerColumnSource";
    public const string ShortcutExplorerColumnAppliesTo = "ShortcutExplorerColumnAppliesTo";
    public const string ShortcutExplorerColumnConflict = "ShortcutExplorerColumnConflict";
    public const string ShortcutExplorerColumnDetails = "ShortcutExplorerColumnDetails";
    public const string ShortcutExplorerColumnFile = "ShortcutExplorerColumnFile";
    public const string ShortcutExplorerClose = "ShortcutExplorerClose";
    public const string ShortcutExplorerSummaryNoneFormat = "ShortcutExplorerSummaryNoneFormat";
    public const string ShortcutExplorerSummaryFoundFormat = "ShortcutExplorerSummaryFoundFormat";
    public const string ShortcutExplorerSummaryNoAssigned = "ShortcutExplorerSummaryNoAssigned";
    public const string ShortcutExplorerReferenceIncludedFormat = "ShortcutExplorerReferenceIncludedFormat";
    public const string ShortcutExplorerWarningSkippedFormat = "ShortcutExplorerWarningSkippedFormat";
    public const string ShortcutExplorerReferenceNote = "ShortcutExplorerReferenceNote";
    public const string ShortcutExplorerConflictWarningFormat = "ShortcutExplorerConflictWarningFormat";
    public const string ShortcutExplorerFilterListedFormat = "ShortcutExplorerFilterListedFormat";
    public const string ShortcutExplorerFilterMatchingFormat = "ShortcutExplorerFilterMatchingFormat";
    public const string ShortcutExplorerFilterConflictSuffixFormat = "ShortcutExplorerFilterConflictSuffixFormat";
    public const string ShortcutExplorerFilterSourceSuffixFormat = "ShortcutExplorerFilterSourceSuffixFormat";

    public const string MacroHotkeyNoSelectedSummary = "MacroHotkeyNoSelectedSummary";
    public const string MacroHotkeyNotSetForSelectedFormat = "MacroHotkeyNotSetForSelectedFormat";
    public const string MacroHotkeySelectedSummaryFormat = "MacroHotkeySelectedSummaryFormat";
    public const string MacroHotkeyDefaultAssignmentsSummary = "MacroHotkeyDefaultAssignmentsSummary";
    public const string MacroHotkeyLogSetupUnavailableNoSaved = "MacroHotkeyLogSetupUnavailableNoSaved";
    public const string MacroHotkeyLogChangesRejectedFormat = "MacroHotkeyLogChangesRejectedFormat";
    public const string MacroHotkeyStatusStoppedActiveBecauseShortcutChanged = "MacroHotkeyStatusStoppedActiveBecauseShortcutChanged";
    public const string MacroHotkeyStatusShortcutsUpdated = "MacroHotkeyStatusShortcutsUpdated";
    public const string MacroHotkeyStatusShortcutsUpdatedButSaveFailed = "MacroHotkeyStatusShortcutsUpdatedButSaveFailed";
    public const string MacroHotkeyStatusShortcutUpdated = "MacroHotkeyStatusShortcutUpdated";
    public const string MacroHotkeyLogSetRequestedNoSaved = "MacroHotkeyLogSetRequestedNoSaved";
    public const string MacroHotkeyLogChangesRejectedForFormat = "MacroHotkeyLogChangesRejectedForFormat";
    public const string MacroHotkeyStatusSaveMacroFirst = "MacroHotkeyStatusSaveMacroFirst";
    public const string MacroHotkeyStatusChangesCanceled = "MacroHotkeyStatusChangesCanceled";
    public const string MacroHotkeyStatusNoChanges = "MacroHotkeyStatusNoChanges";
    public const string MacroHotkeyStatusChooseSavedMacro = "MacroHotkeyStatusChooseSavedMacro";
    public const string MacroHotkeyStatusChangesCanceledForFormat = "MacroHotkeyStatusChangesCanceledForFormat";
    public const string MacroHotkeyStatusUpdatedForFormat = "MacroHotkeyStatusUpdatedForFormat";
    public const string MacroHotkeyStatusUpdatedButSaveFailedForFormat = "MacroHotkeyStatusUpdatedButSaveFailedForFormat";
    public const string MacroHotkeyStatusNoLongerSet = "MacroHotkeyStatusNoLongerSet";
    public const string MacroHotkeyLogIgnoredAssignmentMissing = "MacroHotkeyLogIgnoredAssignmentMissing";
    public const string MacroHotkeyStatusRecordingConflict = "MacroHotkeyStatusRecordingConflict";
    public const string MacroHotkeyLogIgnoredRecordingActiveFormat = "MacroHotkeyLogIgnoredRecordingActiveFormat";
    public const string MacroHotkeyStatusAnotherPlaying = "MacroHotkeyStatusAnotherPlaying";
    public const string MacroHotkeyLogIgnoredAnotherPlayingFormat = "MacroHotkeyLogIgnoredAnotherPlayingFormat";
    public const string MacroHotkeyStatusStoppedFormat = "MacroHotkeyStatusStoppedFormat";
    public const string MacroHotkeyStatusStoppedAndStartedFormat = "MacroHotkeyStatusStoppedAndStartedFormat";
    public const string MacroHotkeyStatusStoppedToRunOnceFormat = "MacroHotkeyStatusStoppedToRunOnceFormat";
    public const string MacroHotkeyStatusRunningOnceFormat = "MacroHotkeyStatusRunningOnceFormat";
    public const string MacroHotkeyLogRunningOnceFromFormat = "MacroHotkeyLogRunningOnceFromFormat";
    public const string MacroHotkeyStatusFinishedFormat = "MacroHotkeyStatusFinishedFormat";
    public const string MacroHotkeyStatusRunFailedFormat = "MacroHotkeyStatusRunFailedFormat";
    public const string MacroHotkeyStatusToggleRunningFormat = "MacroHotkeyStatusToggleRunningFormat";
    public const string MacroHotkeyLogStartedFromAndPressAgainFormat = "MacroHotkeyLogStartedFromAndPressAgainFormat";
    public const string MacroHotkeyStatusToggleStoppedErrorFormat = "MacroHotkeyStatusToggleStoppedErrorFormat";
    public const string MacroHotkeyStatusMissingFileFormat = "MacroHotkeyStatusMissingFileFormat";
    public const string MacroHotkeyLogMissingFilePathFormat = "MacroHotkeyLogMissingFilePathFormat";
    public const string MacroHotkeyStatusNoEventsFormat = "MacroHotkeyStatusNoEventsFormat";
    public const string MacroHotkeyLogNoEventsFormat = "MacroHotkeyLogNoEventsFormat";
    public const string MacroHotkeyStatusLoadFailedFormat = "MacroHotkeyStatusLoadFailedFormat";
    public const string MacroHotkeyStatusStoppedActiveFileUnavailable = "MacroHotkeyStatusStoppedActiveFileUnavailable";
    public const string MacroHotkeyLogUpdatedAfterSavedMacrosChanged = "MacroHotkeyLogUpdatedAfterSavedMacrosChanged";
    public const string MacroHotkeySummaryFormat = "MacroHotkeySummaryFormat";
    public const string MacroHotkeyPlaybackModeRunOnce = "MacroHotkeyPlaybackModeRunOnce";
    public const string MacroHotkeyPlaybackModeToggleRepeat = "MacroHotkeyPlaybackModeToggleRepeat";
    public const string MacroHotkeyOnState = "MacroHotkeyOnState";
    public const string MacroHotkeyOffState = "MacroHotkeyOffState";
    public const string MacroHotkeyActiveFallbackName = "MacroHotkeyActiveFallbackName";

    public const string MacroEventItemPositionNotUsed = "MacroEventItemPositionNotUsed";
    public const string MacroEventItemDetailsMoveMouseFormat = "MacroEventItemDetailsMoveMouseFormat";
    public const string MacroEventItemDetailsMouseDownFormat = "MacroEventItemDetailsMouseDownFormat";
    public const string MacroEventItemDetailsMouseUpFormat = "MacroEventItemDetailsMouseUpFormat";
    public const string MacroEventItemDetailsKeyDownFormat = "MacroEventItemDetailsKeyDownFormat";
    public const string MacroEventItemDetailsKeyUpFormat = "MacroEventItemDetailsKeyUpFormat";
    public const string MacroEventItemDetailsUnavailable = "MacroEventItemDetailsUnavailable";
    public const string MacroEventItemVirtualKeyNone = "MacroEventItemVirtualKeyNone";
    public const string MacroEventItemVirtualKeyOnlyFormat = "MacroEventItemVirtualKeyOnlyFormat";
    public const string MacroEventItemVirtualKeyNamedFormat = "MacroEventItemVirtualKeyNamedFormat";

    public const string InstallerStatusPreparingCatalog = "InstallerStatusPreparingCatalog";
    public const string InstallerEnvironmentDefault = "InstallerEnvironmentDefault";
    public const string InstallerAppUpdateSummaryDefault = "InstallerAppUpdateSummaryDefault";
    public const string InstallerStatusCleanupLoading = "InstallerStatusCleanupLoading";
    public const string InstallerSetupFailedFormat = "InstallerSetupFailedFormat";
    public const string InstallerCheckingTrackedAppsFormat = "InstallerCheckingTrackedAppsFormat";
    public const string InstallerSelectedRecommended = "InstallerSelectedRecommended";
    public const string InstallerSelectedDeveloper = "InstallerSelectedDeveloper";
    public const string InstallerSelectionCleared = "InstallerSelectionCleared";
    public const string CleanupSelectedRecommended = "CleanupSelectedRecommended";
    public const string CleanupSelectionCleared = "CleanupSelectionCleared";
    public const string InstallerNoOfficialPageFormat = "InstallerNoOfficialPageFormat";
    public const string InstallerOpenedOfficialPageFormat = "InstallerOpenedOfficialPageFormat";
    public const string InstallerOpenOfficialPageFailedFormat = "InstallerOpenOfficialPageFailedFormat";
    public const string CleanupSelectInstalledFirst = "CleanupSelectInstalledFirst";
    public const string CleanupRemovingAppsFormat = "CleanupRemovingAppsFormat";
    public const string CleanupFailedFormat = "CleanupFailedFormat";
    public const string InstallerQueueFailedFormat = "InstallerQueueFailedFormat";
    public const string InstallerStatusInstalledUpdatesFormat = "InstallerStatusInstalledUpdatesFormat";
    public const string CleanupStatusInstalledRemovableFormat = "CleanupStatusInstalledRemovableFormat";
    public const string InstallerRefreshFailedFormat = "InstallerRefreshFailedFormat";
    public const string InstallerSelectionSummaryFormat = "InstallerSelectionSummaryFormat";
    public const string InstallerUpdateSummaryInitial = "InstallerUpdateSummaryInitial";
    public const string InstallerUpdateSummaryUnavailable = "InstallerUpdateSummaryUnavailable";
    public const string InstallerUpdateCustomSuffixFormat = "InstallerUpdateCustomSuffixFormat";
    public const string InstallerUpdateNoneFoundFormat = "InstallerUpdateNoneFoundFormat";
    public const string InstallerUpdateReadyListFormat = "InstallerUpdateReadyListFormat";
    public const string InstallerUpdateReadyMoreFormat = "InstallerUpdateReadyMoreFormat";
    public const string CleanupSelectionSummaryFormat = "CleanupSelectionSummaryFormat";
    public const string InstallerQueueSummaryFormat = "InstallerQueueSummaryFormat";
    public const string InstallerQueueCompletionSummaryFormat = "InstallerQueueCompletionSummaryFormat";
    public const string InstallerActionInstalling = "InstallerActionInstalling";
    public const string InstallerActionUpdating = "InstallerActionUpdating";
    public const string InstallerActionRemoving = "InstallerActionRemoving";
    public const string InstallerActionInteractiveInstallRunningFor = "InstallerActionInteractiveInstallRunningFor";
    public const string InstallerActionInteractiveUpdateRunningFor = "InstallerActionInteractiveUpdateRunningFor";
    public const string InstallerActionReinstalling = "InstallerActionReinstalling";
    public const string InstallerActionWorkingOn = "InstallerActionWorkingOn";
    public const string InstallerActiveInstalling = "InstallerActiveInstalling";
    public const string InstallerActiveUpdating = "InstallerActiveUpdating";
    public const string InstallerActiveRemoving = "InstallerActiveRemoving";
    public const string InstallerActiveInteractiveInstall = "InstallerActiveInteractiveInstall";
    public const string InstallerActiveInteractiveUpdate = "InstallerActiveInteractiveUpdate";
    public const string InstallerActiveReinstalling = "InstallerActiveReinstalling";
    public const string InstallerActiveWorking = "InstallerActiveWorking";
    public const string InstallerStatusUnavailable = "InstallerStatusUnavailable";
    public const string InstallerUpdateCheckFailedFormat = "InstallerUpdateCheckFailedFormat";
    public const string InstallerSelectionAllSelectedAlreadyInstalled = "InstallerSelectionAllSelectedAlreadyInstalled";
    public const string InstallerSelectionNoUpdatesReady = "InstallerSelectionNoUpdatesReady";
    public const string InstallerAddedQueuedInstall = "InstallerAddedQueuedInstall";
    public const string InstallerAddedQueuedUpdate = "InstallerAddedQueuedUpdate";
    public const string InstallerAddedQueuedInteractiveInstall = "InstallerAddedQueuedInteractiveInstall";
    public const string InstallerAddedQueuedInteractiveUpdate = "InstallerAddedQueuedInteractiveUpdate";
    public const string InstallerAddedQueuedReinstall = "InstallerAddedQueuedReinstall";
    public const string InstallerSelectionNoInstalledReadyToUpdate = "InstallerSelectionNoInstalledReadyToUpdate";
    public const string InstallerSelectionSelectAtLeastOneFirst = "InstallerSelectionSelectAtLeastOneFirst";
    public const string InstallerQueueSourceLabel = "InstallerQueueSourceLabel";
    public const string InstallerPackageStatusGuidedInstall = "InstallerPackageStatusGuidedInstall";
    public const string InstallerPackageStatusWingetUnavailable = "InstallerPackageStatusWingetUnavailable";
    public const string InstallerStatusGuidedAppsCanOpenOfficialPagesFormat = "InstallerStatusGuidedAppsCanOpenOfficialPagesFormat";
    public const string InstallerPackageStatusQueuedSequenceFormat = "InstallerPackageStatusQueuedSequenceFormat";
    public const string InstallerStatusSourceAlreadyQueuedOrRunningFormat = "InstallerStatusSourceAlreadyQueuedOrRunningFormat";
    public const string InstallerStatusSourceSelectedAppsAlreadyInstalledFormat = "InstallerStatusSourceSelectedAppsAlreadyInstalledFormat";
    public const string InstallerStatusSourceNothingNewAddedFormat = "InstallerStatusSourceNothingNewAddedFormat";
    public const string InstallerSuffixSkippedDuplicateRequestsFormat = "InstallerSuffixSkippedDuplicateRequestsFormat";
    public const string InstallerSuffixSkippedAlreadyInstalledAppsFormat = "InstallerSuffixSkippedAlreadyInstalledAppsFormat";
    public const string InstallerOperationNoResult = "InstallerOperationNoResult";
    public const string InstallerOperationNoResultGuidance = "InstallerOperationNoResultGuidance";
    public const string InstallerLogEdgeWingetFailedTryingFallback = "InstallerLogEdgeWingetFailedTryingFallback";
    public const string InstallerEdgeDisplayName = "InstallerEdgeDisplayName";
    public const string InstallerEdgeFallbackMessageFormat = "InstallerEdgeFallbackMessageFormat";
    public const string InstallerEdgeFallbackGuidanceRunAsAdminRetry = "InstallerEdgeFallbackGuidanceRunAsAdminRetry";
    public const string InstallerLogResultDisplayMessageFormat = "InstallerLogResultDisplayMessageFormat";
    public const string CleanupSummaryCountsFormat = "CleanupSummaryCountsFormat";
    public const string CleanupSummaryManualStepsFormat = "CleanupSummaryManualStepsFormat";
    public const string CleanupSummaryFirstFailureFormat = "CleanupSummaryFirstFailureFormat";
    public const string CleanupSummaryNextStepFormat = "CleanupSummaryNextStepFormat";
    public const string CleanupSummaryFailureLogPathFormat = "CleanupSummaryFailureLogPathFormat";
    public const string CleanupLogFailureLogPathFormat = "CleanupLogFailureLogPathFormat";
    public const string InstallerFirefoxAddonsLabel = "InstallerFirefoxAddonsLabel";
    public const string InstallerLogFirefoxAddonsSkippedBecauseInstallFailed = "InstallerLogFirefoxAddonsSkippedBecauseInstallFailed";
    public const string InstallerLogUpdateInfoWithUrlFormat = "InstallerLogUpdateInfoWithUrlFormat";
    public const string InstallerSupplementalSummaryFormat = "InstallerSupplementalSummaryFormat";
    public const string InstallerLogGuidanceSuffixFormat = "InstallerLogGuidanceSuffixFormat";
    public const string InstallerLogOperationResultFormat = "InstallerLogOperationResultFormat";
    public const string InstallerLogOpenedOfficialPageInBrowserFormat = "InstallerLogOpenedOfficialPageInBrowserFormat";
    public const string InstallerProgressTextFormat = "InstallerProgressTextFormat";
    public const string InstallerQueuedBatchMessageFormat = "InstallerQueuedBatchMessageFormat";
    public const string InstallerActionNounInstallSingular = "InstallerActionNounInstallSingular";
    public const string InstallerActionNounInstallPlural = "InstallerActionNounInstallPlural";
    public const string InstallerActionNounUpdateSingular = "InstallerActionNounUpdateSingular";
    public const string InstallerActionNounUpdatePlural = "InstallerActionNounUpdatePlural";
    public const string InstallerActionNounRemovalSingular = "InstallerActionNounRemovalSingular";
    public const string InstallerActionNounRemovalPlural = "InstallerActionNounRemovalPlural";
    public const string InstallerActionNounTaskSingular = "InstallerActionNounTaskSingular";
    public const string InstallerActionNounTaskPlural = "InstallerActionNounTaskPlural";
    public const string InstallerPluralS = "InstallerPluralS";
    public const string InstallerPluralIs = "InstallerPluralIs";
    public const string InstallerPluralAre = "InstallerPluralAre";

    public const string ToolsUsefulSitesToggleHide = "ToolsUsefulSitesToggleHide";
    public const string ToolsUsefulSitesToggleShow = "ToolsUsefulSitesToggleShow";
    public const string ToolsStatusEmptyDirectoryInitial = "ToolsStatusEmptyDirectoryInitial";
    public const string ToolsStatusShortcutHotkeyInitial = "ToolsStatusShortcutHotkeyInitial";
    public const string ToolsStatusMouseSensitivityInitial = "ToolsStatusMouseSensitivityInitial";
    public const string ToolsStatusDisplayRefreshInitial = "ToolsStatusDisplayRefreshInitial";
    public const string ToolsStatusHardwareSystemInitial = "ToolsStatusHardwareSystemInitial";
    public const string ToolsStatusHardwareHealthInitial = "ToolsStatusHardwareHealthInitial";
    public const string ToolsStatusHardwareOperatingSystemInitial = "ToolsStatusHardwareOperatingSystemInitial";
    public const string ToolsStatusHardwareProcessorInitial = "ToolsStatusHardwareProcessorInitial";
    public const string ToolsStatusHardwareMemoryInitial = "ToolsStatusHardwareMemoryInitial";
    public const string ToolsStatusHardwareMotherboardInitial = "ToolsStatusHardwareMotherboardInitial";
    public const string ToolsStatusHardwareBiosInitial = "ToolsStatusHardwareBiosInitial";
    public const string ToolsStatusHardwareCheckInitial = "ToolsStatusHardwareCheckInitial";
    public const string ToolsStatusDriverUpdateInitial = "ToolsStatusDriverUpdateInitial";
    public const string ToolsStatusDarkModeInitial = "ToolsStatusDarkModeInitial";
    public const string ToolsStatusSearchReplacementInitial = "ToolsStatusSearchReplacementInitial";
    public const string ToolsStatusSearchReindexInitial = "ToolsStatusSearchReindexInitial";
    public const string ToolsStatusTelemetryInitial = "ToolsStatusTelemetryInitial";
    public const string ToolsStatusPinWindowInitial = "ToolsStatusPinWindowInitial";
    public const string ToolsStatusOneDriveInitial = "ToolsStatusOneDriveInitial";
    public const string ToolsStatusEdgeInitial = "ToolsStatusEdgeInitial";
    public const string ToolsStatusFnCtrlSwapInitial = "ToolsStatusFnCtrlSwapInitial";
    public const string ToolsStatusUsefulSitesInitial = "ToolsStatusUsefulSitesInitial";
    public const string ToolsStatusWindows11EeaInitial = "ToolsStatusWindows11EeaInitial";
    public const string ToolsErrorReadMouseSensitivityFormat = "ToolsErrorReadMouseSensitivityFormat";
    public const string ToolsErrorCheckOneDriveFormat = "ToolsErrorCheckOneDriveFormat";
    public const string ToolsErrorCheckEdgeFormat = "ToolsErrorCheckEdgeFormat";
    public const string ToolsErrorCheckSearchReplacementFormat = "ToolsErrorCheckSearchReplacementFormat";
    public const string ToolsErrorCheckFnCtrlSwapFormat = "ToolsErrorCheckFnCtrlSwapFormat";
    public const string ToolsFolderPickerSelectEmptyDirectoryRoot = "ToolsFolderPickerSelectEmptyDirectoryRoot";
    public const string ToolsStatusFolderSelectionCanceled = "ToolsStatusFolderSelectionCanceled";
    public const string ToolsStatusEmptyDirectoryRootSetFormat = "ToolsStatusEmptyDirectoryRootSetFormat";
    public const string ToolsStatusShortcutScanRunning = "ToolsStatusShortcutScanRunning";
    public const string ToolsStatusShortcutScanWarningsSuffixFormat = "ToolsStatusShortcutScanWarningsSuffixFormat";
    public const string ToolsStatusShortcutScanNoShortcutsFormat = "ToolsStatusShortcutScanNoShortcutsFormat";
    public const string ToolsStatusShortcutScanOpenedViewerFormat = "ToolsStatusShortcutScanOpenedViewerFormat";
    public const string ToolsStatusShortcutScanFailedFormat = "ToolsStatusShortcutScanFailedFormat";
    public const string ToolsStatusUsefulSitesShowingFormat = "ToolsStatusUsefulSitesShowingFormat";
    public const string ToolsStatusUsefulSitesHidden = "ToolsStatusUsefulSitesHidden";
    public const string ToolsStatusUsefulSiteOpenedFormat = "ToolsStatusUsefulSiteOpenedFormat";
    public const string ToolsStatusUsefulSiteOpenFailedFormat = "ToolsStatusUsefulSiteOpenFailedFormat";
    public const string ToolsStatusWindows11EeaPreparing = "ToolsStatusWindows11EeaPreparing";
    public const string ToolsStatusWindows11EeaFailedFormat = "ToolsStatusWindows11EeaFailedFormat";
    public const string ToolsStatusSearchReplacementApplying = "ToolsStatusSearchReplacementApplying";
    public const string ToolsStatusSearchReplacementApplyFailedFormat = "ToolsStatusSearchReplacementApplyFailedFormat";
    public const string ToolsStatusSearchReplacementRestoring = "ToolsStatusSearchReplacementRestoring";
    public const string ToolsStatusSearchReplacementRestoreFailedFormat = "ToolsStatusSearchReplacementRestoreFailedFormat";
    public const string ToolsStatusTelemetryApplying = "ToolsStatusTelemetryApplying";
    public const string ToolsStatusTelemetryApplyFailedFormat = "ToolsStatusTelemetryApplyFailedFormat";
    public const string ToolsStatusTelemetryRestoring = "ToolsStatusTelemetryRestoring";
    public const string ToolsStatusTelemetryRestoreFailedFormat = "ToolsStatusTelemetryRestoreFailedFormat";
    public const string ToolsStatusSearchReindexRequesting = "ToolsStatusSearchReindexRequesting";
    public const string ToolsStatusSearchReindexFailedFormat = "ToolsStatusSearchReindexFailedFormat";
    public const string ToolsErrorCheckSearchReindexFormat = "ToolsErrorCheckSearchReindexFormat";
    public const string ToolsErrorCheckTelemetryFormat = "ToolsErrorCheckTelemetryFormat";
    public const string ToolsStatusPinWindowPinnedFormat = "ToolsStatusPinWindowPinnedFormat";
    public const string ToolsStatusPinWindowUnpinnedFormat = "ToolsStatusPinWindowUnpinnedFormat";
    public const string ToolsPinWindowStatePinned = "ToolsPinWindowStatePinned";
    public const string ToolsPinWindowStateUnpinned = "ToolsPinWindowStateUnpinned";
    public const string ToolsStatusPinWindowToggledFormat = "ToolsStatusPinWindowToggledFormat";
    public const string ToolsPinWindowTriggerHotkey = "ToolsPinWindowTriggerHotkey";
    public const string ToolsPinWindowTriggerToolButton = "ToolsPinWindowTriggerToolButton";
    public const string ToolsStatusSettingsDarkModeOn = "ToolsStatusSettingsDarkModeOn";
    public const string ToolsStatusOpenedColorSettings = "ToolsStatusOpenedColorSettings";
    public const string ToolsStatusOpenColorSettingsFailedFormat = "ToolsStatusOpenColorSettingsFailedFormat";
    public const string ToolsStatusOpenedScanRoot = "ToolsStatusOpenedScanRoot";
    public const string ToolsStatusOpenScanRootFailedFormat = "ToolsStatusOpenScanRootFailedFormat";
    public const string ToolsStatusMouseSensitivityApplyingFormat = "ToolsStatusMouseSensitivityApplyingFormat";
    public const string ToolsStatusMouseSensitivityUpdateFailedFormat = "ToolsStatusMouseSensitivityUpdateFailedFormat";
    public const string ToolsStatusOpenedMouseSettings = "ToolsStatusOpenedMouseSettings";
    public const string ToolsStatusOpenMouseSettingsFailedFormat = "ToolsStatusOpenMouseSettingsFailedFormat";
    public const string ToolsStatusDisplayRefreshApplying = "ToolsStatusDisplayRefreshApplying";
    public const string ToolsStatusDisplayRefreshAlreadyBest = "ToolsStatusDisplayRefreshAlreadyBest";
    public const string ToolsStatusDisplayRefreshAppliedSummaryFormat = "ToolsStatusDisplayRefreshAppliedSummaryFormat";
    public const string ToolsStatusDisplayRefreshUpdateFailedFormat = "ToolsStatusDisplayRefreshUpdateFailedFormat";
    public const string ToolsStatusHardwareCopiedClipboard = "ToolsStatusHardwareCopiedClipboard";
    public const string ToolsStatusHardwareCopyFailedFormat = "ToolsStatusHardwareCopyFailedFormat";
    public const string ToolsStatusDriverSelectRecommended = "ToolsStatusDriverSelectRecommended";
    public const string ToolsStatusDriverSelectAll = "ToolsStatusDriverSelectAll";
    public const string ToolsStatusDriverSelectionCleared = "ToolsStatusDriverSelectionCleared";
    public const string ToolsStatusDriverSelectAtLeastOne = "ToolsStatusDriverSelectAtLeastOne";
    public const string ToolsStatusDriverInstallingFormat = "ToolsStatusDriverInstallingFormat";
    public const string ToolsStatusDriverResultInstalledFormat = "ToolsStatusDriverResultInstalledFormat";
    public const string ToolsStatusDriverResultManualFlowFormat = "ToolsStatusDriverResultManualFlowFormat";
    public const string ToolsStatusDriverResultFailedFormat = "ToolsStatusDriverResultFailedFormat";
    public const string ToolsStatusDriverResultNoChanges = "ToolsStatusDriverResultNoChanges";
    public const string ToolsStatusDriverSummaryFormat = "ToolsStatusDriverSummaryFormat";
    public const string ToolsStatusDriverManualFlowHint = "ToolsStatusDriverManualFlowHint";
    public const string ToolsStatusDriverRestartHintFormat = "ToolsStatusDriverRestartHintFormat";
    public const string ToolsStatusDriverInstallFailedFormat = "ToolsStatusDriverInstallFailedFormat";
    public const string ToolsStatusDriverOpenOptionalUpdatesFailedFormat = "ToolsStatusDriverOpenOptionalUpdatesFailedFormat";
    public const string ToolsStatusDriverOpenOptionalUpdatesAndScan = "ToolsStatusDriverOpenOptionalUpdatesAndScan";
    public const string ToolsStatusDriverOpenOptionalUpdatesNoScan = "ToolsStatusDriverOpenOptionalUpdatesNoScan";
    public const string ToolsStatusDriverOpenUpdatesAndScan = "ToolsStatusDriverOpenUpdatesAndScan";
    public const string ToolsStatusDriverOpenUpdatesNoScan = "ToolsStatusDriverOpenUpdatesNoScan";
    public const string ToolsStatusOneDriveRemoving = "ToolsStatusOneDriveRemoving";
    public const string ToolsStatusOneDriveRemoveFailedFormat = "ToolsStatusOneDriveRemoveFailedFormat";
    public const string ToolsStatusEdgeRemoving = "ToolsStatusEdgeRemoving";
    public const string ToolsStatusEdgeRemoveFailedFormat = "ToolsStatusEdgeRemoveFailedFormat";
    public const string ToolsStatusFnCtrlSwapApplying = "ToolsStatusFnCtrlSwapApplying";
    public const string ToolsStatusFnCtrlSwapFailedFormat = "ToolsStatusFnCtrlSwapFailedFormat";
    public const string ToolsStatusEmptyDirectorySelectAll = "ToolsStatusEmptyDirectorySelectAll";
    public const string ToolsStatusEmptyDirectorySelectionCleared = "ToolsStatusEmptyDirectorySelectionCleared";
    public const string ToolsStatusEmptyDirectorySelectAtLeastOne = "ToolsStatusEmptyDirectorySelectAtLeastOne";
    public const string ToolsStatusEmptyDirectoryDeletingFormat = "ToolsStatusEmptyDirectoryDeletingFormat";
    public const string ToolsStatusEmptyDirectoryDeleteSummaryFormat = "ToolsStatusEmptyDirectoryDeleteSummaryFormat";
    public const string ToolsStatusEmptyDirectoryDeleteFailedFormat = "ToolsStatusEmptyDirectoryDeleteFailedFormat";
    public const string ToolsStatusEmptyDirectoryChooseRootFirst = "ToolsStatusEmptyDirectoryChooseRootFirst";
    public const string ToolsStatusEmptyDirectoryRootMissingFormat = "ToolsStatusEmptyDirectoryRootMissingFormat";
    public const string ToolsStatusEmptyDirectoryScanningFormat = "ToolsStatusEmptyDirectoryScanningFormat";
    public const string ToolsStatusEmptyDirectoryWarningsSuffixFormat = "ToolsStatusEmptyDirectoryWarningsSuffixFormat";
    public const string ToolsStatusEmptyDirectoryNoneFoundFormat = "ToolsStatusEmptyDirectoryNoneFoundFormat";
    public const string ToolsStatusEmptyDirectoryFoundFormat = "ToolsStatusEmptyDirectoryFoundFormat";
    public const string ToolsStatusEmptyDirectoryScanFailedFormat = "ToolsStatusEmptyDirectoryScanFailedFormat";
    public const string ToolsStatusDriverScanStarting = "ToolsStatusDriverScanStarting";
    public const string ToolsStatusDriverScanWarningsSuffixFormat = "ToolsStatusDriverScanWarningsSuffixFormat";
    public const string ToolsStatusDriverScanInteractiveSuffixFormat = "ToolsStatusDriverScanInteractiveSuffixFormat";
    public const string ToolsStatusDriverScanNoneFormat = "ToolsStatusDriverScanNoneFormat";
    public const string ToolsStatusDriverScanFoundFormat = "ToolsStatusDriverScanFoundFormat";
    public const string ToolsStatusDriverScanFailedFormat = "ToolsStatusDriverScanFailedFormat";
    public const string ToolsStatusHardwareScanStarting = "ToolsStatusHardwareScanStarting";
    public const string ToolsStatusHardwareScanWarningsSuffixFormat = "ToolsStatusHardwareScanWarningsSuffixFormat";
    public const string ToolsStatusHardwareScanCompleteFormat = "ToolsStatusHardwareScanCompleteFormat";
    public const string ToolsStatusHardwareScanFailedFormat = "ToolsStatusHardwareScanFailedFormat";
    public const string ToolsStatusDisplayRefreshScanStarting = "ToolsStatusDisplayRefreshScanStarting";
    public const string ToolsStatusDisplayRefreshNoDisplays = "ToolsStatusDisplayRefreshNoDisplays";
    public const string ToolsStatusDisplayRefreshCheckedAllBestFormat = "ToolsStatusDisplayRefreshCheckedAllBestFormat";
    public const string ToolsStatusDisplayRefreshCheckedCanRunFasterFormat = "ToolsStatusDisplayRefreshCheckedCanRunFasterFormat";
    public const string ToolsStatusDisplayRefreshScanFailedFormat = "ToolsStatusDisplayRefreshScanFailedFormat";
    public const string ToolsShortcutHotkeyScanPreparing = "ToolsShortcutHotkeyScanPreparing";
    public const string ToolsEmptyDirectorySelectionSummaryFormat = "ToolsEmptyDirectorySelectionSummaryFormat";
    public const string ToolsEmptyDirectoryScanProgressSummaryFormat = "ToolsEmptyDirectoryScanProgressSummaryFormat";
    public const string ToolsShortcutHotkeyScanProgressSummaryFormat = "ToolsShortcutHotkeyScanProgressSummaryFormat";
    public const string ToolsMouseSensitivitySummaryCurrentFormat = "ToolsMouseSensitivitySummaryCurrentFormat";
    public const string ToolsMouseSensitivitySummaryPickedFormat = "ToolsMouseSensitivitySummaryPickedFormat";
    public const string ToolsMouseSensitivityGuidanceVerySlow = "ToolsMouseSensitivityGuidanceVerySlow";
    public const string ToolsMouseSensitivityGuidanceSlow = "ToolsMouseSensitivityGuidanceSlow";
    public const string ToolsMouseSensitivityGuidanceBalanced = "ToolsMouseSensitivityGuidanceBalanced";
    public const string ToolsMouseSensitivityGuidanceFast = "ToolsMouseSensitivityGuidanceFast";
    public const string ToolsMouseSensitivityGuidanceVeryFast = "ToolsMouseSensitivityGuidanceVeryFast";
    public const string ToolsMouseSensitivitySelectionGuidanceFormat = "ToolsMouseSensitivitySelectionGuidanceFormat";
    public const string ToolsMouseSensitivityFeelVerySlow = "ToolsMouseSensitivityFeelVerySlow";
    public const string ToolsMouseSensitivityFeelSlow = "ToolsMouseSensitivityFeelSlow";
    public const string ToolsMouseSensitivityFeelBalanced = "ToolsMouseSensitivityFeelBalanced";
    public const string ToolsMouseSensitivityFeelFast = "ToolsMouseSensitivityFeelFast";
    public const string ToolsMouseSensitivityFeelVeryFast = "ToolsMouseSensitivityFeelVeryFast";
    public const string ToolsMouseSensitivityLevelTextMiddleFormat = "ToolsMouseSensitivityLevelTextMiddleFormat";
    public const string ToolsMouseSensitivityLevelTextFormat = "ToolsMouseSensitivityLevelTextFormat";
    public const string ToolsDisplayRefreshSummaryNone = "ToolsDisplayRefreshSummaryNone";
    public const string ToolsDisplayRefreshSummaryAllBestFormat = "ToolsDisplayRefreshSummaryAllBestFormat";
    public const string ToolsDisplayRefreshSummaryCanRunFasterFormat = "ToolsDisplayRefreshSummaryCanRunFasterFormat";
    public const string ToolsHardwareGraphicsSummaryFormat = "ToolsHardwareGraphicsSummaryFormat";
    public const string ToolsHardwareStorageSummaryFormat = "ToolsHardwareStorageSummaryFormat";
    public const string ToolsHardwarePartitionSummaryNone = "ToolsHardwarePartitionSummaryNone";
    public const string ToolsHardwarePartitionSummaryFormat = "ToolsHardwarePartitionSummaryFormat";
    public const string ToolsHardwareSensorSummaryNone = "ToolsHardwareSensorSummaryNone";
    public const string ToolsHardwareSensorSummaryFormat = "ToolsHardwareSensorSummaryFormat";
    public const string ToolsHardwarePciSummaryNone = "ToolsHardwarePciSummaryNone";
    public const string ToolsHardwarePciSummaryFormat = "ToolsHardwarePciSummaryFormat";
    public const string ToolsHardwareRaidSummaryNone = "ToolsHardwareRaidSummaryNone";
    public const string ToolsHardwareRaidSummaryFormat = "ToolsHardwareRaidSummaryFormat";
    public const string ToolsDriverHardwareSummaryFormat = "ToolsDriverHardwareSummaryFormat";
    public const string ToolsDriverUpdateSelectionSummaryFormat = "ToolsDriverUpdateSelectionSummaryFormat";
    public const string ToolsHardwareClipboardTitle = "ToolsHardwareClipboardTitle";
    public const string ToolsHardwareClipboardCapturedFormat = "ToolsHardwareClipboardCapturedFormat";
    public const string ToolsHardwareClipboardSystemSection = "ToolsHardwareClipboardSystemSection";
    public const string ToolsHardwareClipboardOverviewFormat = "ToolsHardwareClipboardOverviewFormat";
    public const string ToolsHardwareClipboardHealthSummaryFormat = "ToolsHardwareClipboardHealthSummaryFormat";
    public const string ToolsHardwareClipboardOperatingSystemFormat = "ToolsHardwareClipboardOperatingSystemFormat";
    public const string ToolsHardwareClipboardProcessorFormat = "ToolsHardwareClipboardProcessorFormat";
    public const string ToolsHardwareClipboardMemoryFormat = "ToolsHardwareClipboardMemoryFormat";
    public const string ToolsHardwareClipboardMotherboardFormat = "ToolsHardwareClipboardMotherboardFormat";
    public const string ToolsHardwareClipboardBiosFormat = "ToolsHardwareClipboardBiosFormat";
    public const string ToolsHardwareClipboardGraphicsSection = "ToolsHardwareClipboardGraphicsSection";
    public const string ToolsHardwareClipboardSummaryFormat = "ToolsHardwareClipboardSummaryFormat";
    public const string ToolsHardwareClipboardDriverFormat = "ToolsHardwareClipboardDriverFormat";
    public const string ToolsHardwareClipboardMemoryFieldFormat = "ToolsHardwareClipboardMemoryFieldFormat";
    public const string ToolsHardwareClipboardStorageSection = "ToolsHardwareClipboardStorageSection";
    public const string ToolsHardwareClipboardSizeFormat = "ToolsHardwareClipboardSizeFormat";
    public const string ToolsHardwareClipboardInterfaceFormat = "ToolsHardwareClipboardInterfaceFormat";
    public const string ToolsHardwareClipboardMediaFormat = "ToolsHardwareClipboardMediaFormat";
    public const string ToolsHardwareClipboardHealthFormat = "ToolsHardwareClipboardHealthFormat";
    public const string ToolsHardwareClipboardSmartFormat = "ToolsHardwareClipboardSmartFormat";
    public const string ToolsHardwareClipboardFirmwareFormat = "ToolsHardwareClipboardFirmwareFormat";
    public const string ToolsHardwareClipboardSerialFormat = "ToolsHardwareClipboardSerialFormat";
    public const string ToolsHardwareClipboardNotesFormat = "ToolsHardwareClipboardNotesFormat";
    public const string ToolsHardwareClipboardPartitionsSection = "ToolsHardwareClipboardPartitionsSection";
    public const string ToolsHardwareClipboardDiskFormat = "ToolsHardwareClipboardDiskFormat";
    public const string ToolsHardwareClipboardTypeFormat = "ToolsHardwareClipboardTypeFormat";
    public const string ToolsHardwareClipboardVolumeFormat = "ToolsHardwareClipboardVolumeFormat";
    public const string ToolsHardwareClipboardFileSystemFormat = "ToolsHardwareClipboardFileSystemFormat";
    public const string ToolsHardwareClipboardFreeSpaceFormat = "ToolsHardwareClipboardFreeSpaceFormat";
    public const string ToolsHardwareClipboardStatusFormat = "ToolsHardwareClipboardStatusFormat";
    public const string ToolsHardwareClipboardSensorsSection = "ToolsHardwareClipboardSensorsSection";
    public const string ToolsHardwareClipboardCategoryFormat = "ToolsHardwareClipboardCategoryFormat";
    public const string ToolsHardwareClipboardReadingFormat = "ToolsHardwareClipboardReadingFormat";
    public const string ToolsHardwareClipboardSourceFormat = "ToolsHardwareClipboardSourceFormat";
    public const string ToolsHardwareClipboardPciSection = "ToolsHardwareClipboardPciSection";
    public const string ToolsHardwareClipboardClassFormat = "ToolsHardwareClipboardClassFormat";
    public const string ToolsHardwareClipboardManufacturerFormat = "ToolsHardwareClipboardManufacturerFormat";
    public const string ToolsHardwareClipboardLocationFormat = "ToolsHardwareClipboardLocationFormat";
    public const string ToolsHardwareClipboardRaidSection = "ToolsHardwareClipboardRaidSection";
    public const string ToolsHardwareClipboardDetailsFormat = "ToolsHardwareClipboardDetailsFormat";
    public const string ToolsUsefulSiteFmhyName = "ToolsUsefulSiteFmhyName";
    public const string ToolsUsefulSiteFmhyDescription = "ToolsUsefulSiteFmhyDescription";
    public const string ToolsUsefulSiteSevenSeasName = "ToolsUsefulSiteSevenSeasName";
    public const string ToolsUsefulSiteSevenSeasDescription = "ToolsUsefulSiteSevenSeasDescription";
    public const string ToolsUsefulSiteZLibraryName = "ToolsUsefulSiteZLibraryName";
    public const string ToolsUsefulSiteZLibraryDescription = "ToolsUsefulSiteZLibraryDescription";
    public const string ToolsUsefulSiteFmhyBackupName = "ToolsUsefulSiteFmhyBackupName";
    public const string ToolsUsefulSiteFmhyBackupDescription = "ToolsUsefulSiteFmhyBackupDescription";

    public const string ScreenshotOptionsTitle = "ScreenshotOptionsTitle";
    public const string ScreenshotFullScreenHeader = "ScreenshotFullScreenHeader";
    public const string ScreenshotFullScreenDescription = "ScreenshotFullScreenDescription";
    public const string ScreenshotAreaHeader = "ScreenshotAreaHeader";
    public const string ScreenshotAreaDescription = "ScreenshotAreaDescription";
    public const string ScreenshotRecordVideo = "ScreenshotRecordVideo";
    public const string ScreenshotCancel = "ScreenshotCancel";
    public const string ScreenshotWaiting = "ScreenshotWaiting";

    public const string ScreenshotStatusOptionsOpen = "ScreenshotStatusOptionsOpen";
    public const string ScreenshotStatusChooseModeFirst = "ScreenshotStatusChooseModeFirst";
    public const string ScreenshotStatusRecordingStarted = "ScreenshotStatusRecordingStarted";
    public const string ScreenshotStatusUnableToStart = "ScreenshotStatusUnableToStart";
    public const string ScreenshotStatusVideoTargetFullScreen = "ScreenshotStatusVideoTargetFullScreen";
    public const string ScreenshotStatusAreaSelectionCanceled = "ScreenshotStatusAreaSelectionCanceled";
    public const string ScreenshotStatusAreaSelected = "ScreenshotStatusAreaSelected";
    public const string ScreenshotStatusVideoModeArmed = "ScreenshotStatusVideoModeArmed";
    public const string ScreenshotStatusStopRecordingFirst = "ScreenshotStatusStopRecordingFirst";
    public const string ScreenshotStatusVideoModeOff = "ScreenshotStatusVideoModeOff";
    public const string ScreenshotStatusStillRecordingSaveFirst = "ScreenshotStatusStillRecordingSaveFirst";
    public const string ScreenshotStatusRecordingStopped = "ScreenshotStatusRecordingStopped";
    public const string ScreenshotStatusSavedVideoTo = "ScreenshotStatusSavedVideoTo";
    public const string ScreenshotStatusUnableToStop = "ScreenshotStatusUnableToStop";
    public const string ScreenshotStatusVideoOff = "ScreenshotStatusVideoOff";
}

public static class AppLanguageStrings
{
    private static readonly Dictionary<string, (string English, string CatSpeak)> Values = new(StringComparer.Ordinal)
    {
        [AppLanguageKeys.CleanupUninstallFailureTitle] = ("MultiTool Cleanup Uninstall Failure", "MultiTool Cleanup Uninstall Failure"),
        [AppLanguageKeys.CleanupUninstallExceptionTitle] = ("MultiTool Cleanup Uninstall Exception", "MultiTool Cleanup Uninstall Exception"),
        [AppLanguageKeys.CleanupUninstallTimestampFormat] = ("Timestamp: {0}", "Timestamp: {0}"),
        [AppLanguageKeys.CleanupUninstallSelectedCountFormat] = ("Selected count: {0}", "Selected count: {0}"),
        [AppLanguageKeys.CleanupUninstallSelectedPackages] = ("Selected packages:", "Selected packages:"),
        [AppLanguageKeys.CleanupUninstallOperationResults] = ("Operation results:", "Operation results:"),
        [AppLanguageKeys.CleanupUninstallException] = ("Exception:", "Exception:"),
        [AppLanguageKeys.CleanupResultSucceededFormat] = ("  Succeeded: {0}", "  Succeeded: {0}"),
        [AppLanguageKeys.CleanupResultChangedFormat] = ("  Changed: {0}", "  Changed: {0}"),
        [AppLanguageKeys.CleanupResultRequiresManualStepFormat] = ("  RequiresManualStep: {0}", "  RequiresManualStep: {0}"),
        [AppLanguageKeys.CleanupResultMessageFormat] = ("  Message: {0}", "  Message: {0}"),
        [AppLanguageKeys.CleanupResultGuidanceFormat] = ("  Guidance: {0}", "  Guidance: {0}"),
        [AppLanguageKeys.CleanupResultOutputLabel] = ("  Output:", "  Output:"),
        [AppLanguageKeys.StartupFailureMessage] = (
            "MultiTool failed to start. Check the Logs folder next to the EXE for details.{0}{0}{1}",
            "MultiTool refused to pounce at startup. Check the Logs folder next to the EXE fur details.{0}{0}{1}"),
        [AppLanguageKeys.StartupErrorTitle] = ("MultiTool Startup Error", "MultiTool Startup Hiss"),

        [AppLanguageKeys.EnumCount] = ("Count", "Count"),
        [AppLanguageKeys.EnumCursor] = ("Cursor", "Paw-sor"),
        [AppLanguageKeys.EnumFixed] = ("Fixed", "Fixed"),
        [AppLanguageKeys.EnumLeft] = ("Left", "Left"),
        [AppLanguageKeys.EnumRight] = ("Right", "Right"),
        [AppLanguageKeys.EnumMiddle] = ("Middle", "Middle"),
        [AppLanguageKeys.EnumCustom] = ("Custom", "Custom"),
        [AppLanguageKeys.EnumSide1] = ("Side 1", "Side 1"),
        [AppLanguageKeys.EnumSide2] = ("Side 2", "Side 2"),
        [AppLanguageKeys.EnumSingle] = ("Single", "Single"),
        [AppLanguageKeys.EnumDouble] = ("Double", "Double"),
        [AppLanguageKeys.EnumHold] = ("Hold", "Hold"),
        [AppLanguageKeys.EnumRunOnce] = ("Run once", "Purr-lay once"),
        [AppLanguageKeys.EnumStartStop] = ("Start/stop", "Start/stop"),
        [AppLanguageKeys.EnumMoveMouse] = ("Move Mouse", "Move Mouse"),
        [AppLanguageKeys.EnumMouseDown] = ("Mouse Down", "Mouse Down"),
        [AppLanguageKeys.EnumMouseUp] = ("Mouse Up", "Mouse Up"),
        [AppLanguageKeys.EnumKeyDown] = ("Key Down", "Key Down"),
        [AppLanguageKeys.EnumKeyUp] = ("Key Up", "Key Up"),

        [AppLanguageKeys.RightMouseButton] = ("Right Mouse Button", "Right Mouse Button"),
        [AppLanguageKeys.MiddleMouseButton] = ("Middle Mouse Button", "Middle Mouse Button"),
        [AppLanguageKeys.MouseButton4] = ("Mouse Button 4", "Mouse Button 4"),
        [AppLanguageKeys.MouseButton5] = ("Mouse Button 5", "Mouse Button 5"),

        [AppLanguageKeys.DisplayRefreshFrequencySummaryNeedsChange] = (
            "{0}  -  Currently {1}, can go up to {2}",
            "{0}  -  Right meow at {1}, can zoomies up to {2}"),
        [AppLanguageKeys.DisplayRefreshFrequencySummaryBestAvailable] = (
            "{0}  -  Running at {1} (best available)",
            "{0}  -  Purring at {1} (best available)"),
        [AppLanguageKeys.DisplayRefreshDefaultFrequency] = ("Default", "Default"),

        [AppLanguageKeys.DriverClassificationOptional] = ("Optional", "Optional"),
        [AppLanguageKeys.DriverClassificationRecommended] = ("Recommended", "Recommended"),
        [AppLanguageKeys.DriverInstallFlowNeedsInteractive] = (
            "Needs Windows Update's own interactive install flow",
            "Needs Windows Update's own interactive install flow"),
        [AppLanguageKeys.DriverInstallFlowCanInstallDirectly] = (
            "Can install directly in MultiTool",
            "Can install directly in MultiTool"),

        [AppLanguageKeys.EmptyDirectoryHintNested] = (
            "Becomes empty after nested empty folders are removed.",
            "Becomes empty after nested empty folders are eaten."),
        [AppLanguageKeys.EmptyDirectoryHintAlreadyEmpty] = (
            "Already empty.",
            "Already empty."),

        [AppLanguageKeys.InstallerActionInstall] = ("Install", "Install"),
        [AppLanguageKeys.InstallerActionUpdate] = ("Update", "Update"),
        [AppLanguageKeys.InstallerActionRemove] = ("Remove", "Remove"),
        [AppLanguageKeys.InstallerActionInteractiveInstall] = ("Interactive Install", "Interactive Install"),
        [AppLanguageKeys.InstallerActionInteractiveUpdate] = ("Interactive Update", "Interactive Update"),
        [AppLanguageKeys.InstallerActionReinstall] = ("Reinstall", "Reinstall"),
        [AppLanguageKeys.InstallerActionRun] = ("Run", "Run"),
        [AppLanguageKeys.InstallerOperationHeaderFormat] = ("#{0} {1} {2}", "#{0} {1} {2}"),
        [AppLanguageKeys.InstallerStatusQueued] = ("Queued", "Queued"),
        [AppLanguageKeys.InstallerPackageHintHandledByMultiTool] = ("Handled by MultiTool", "Handled by MultiTool"),
        [AppLanguageKeys.InstallerPackageHintMicrosoftStoreApp] = ("Microsoft Store app", "Microsoft Store app"),
        [AppLanguageKeys.InstallerPackageHintOfficialSetupPage] = ("Official setup page", "Official setup page"),
        [AppLanguageKeys.InstallerPackageHintWindowsApp] = ("Windows app", "Windows app"),
        [AppLanguageKeys.InstallerPrimaryActionQueueUpdate] = ("Queue Update", "Queue Update"),
        [AppLanguageKeys.InstallerPrimaryActionQueueInstall] = ("Queue Install", "Queue Install"),
        [AppLanguageKeys.InstallerInteractiveActionUpdate] = ("Interactive Update", "Interactive Update"),
        [AppLanguageKeys.InstallerInteractiveActionInstall] = ("Interactive Install", "Interactive Install"),
        [AppLanguageKeys.InstallerPageActionOpenUpdatePage] = ("Open Update Page", "Open Update Page"),
        [AppLanguageKeys.InstallerPageActionOpenInstallPage] = ("Open Install Page", "Open Install Page"),
        [AppLanguageKeys.InstallerStatusChecking] = ("Checking status...", "Checking status..."),
        [AppLanguageKeys.InstallerCapabilityCustomFlow] = ("Custom flow", "Custom flow"),
        [AppLanguageKeys.InstallerCapabilityQuietWinget] = ("Quiet winget", "Quiet winget"),
        [AppLanguageKeys.InstallerCapabilityInteractiveOption] = ("Interactive option", "Interactive option"),
        [AppLanguageKeys.InstallerCapabilityReinstall] = ("Reinstall", "Reinstall"),
        [AppLanguageKeys.InstallerCapabilityOfficialPage] = ("Official page", "Official page"),
        [AppLanguageKeys.UsefulSiteBrowserLabelTor] = ("Tor Browser", "Tor Browser"),
        [AppLanguageKeys.UsefulSiteBrowserLabelDefault] = ("Default browser", "Default browser"),

        [AppLanguageKeys.AppearanceHelperText] = (
            "On first run, the app starts in the same light or dark mode that Windows is already using. Ctrl + mouse wheel zoom is always enabled, and you can still keep the window on top or have startup launches hide straight to the tray.",
            "This is not bloat. Deal with it."),
        [AppLanguageKeys.ClickerTabHeader] = ("Clicker", "Clicker :3"),
        [AppLanguageKeys.ClickerHotkeyLabel] = ("Clicker Start/Stop Hotkey", "Clicker Start/Stop Hotkey"),
        [AppLanguageKeys.ScreenshotTabHeader] = ("Screenshot", "Screenshot"),
        [AppLanguageKeys.MacroTabHeader] = ("Macro", "Meowcro"),
        [AppLanguageKeys.MacroSetShortcutButton] = ("Set Shortcut", "Set Shortcut"),
        [AppLanguageKeys.MacroEditShortcutsButton] = ("Edit Shortcuts", "Edit Shortcuts"),
        [AppLanguageKeys.ToolsTabHeader] = ("Tools", "Cat Tools"),
        [AppLanguageKeys.NicheToolsHeader] = ("Niche Tools", "Niche Cat Tools"),
        [AppLanguageKeys.ShortcutKeyExplorerHeader] = ("Shortcut Key Explorer", "Shortcut Key Explorer"),
        [AppLanguageKeys.ShortcutExplorerButton] = ("Open Shortcut Explorer", "Open Shortcut Explorer"),
        [AppLanguageKeys.InstallerTabHeader] = ("Installer", "Instawller"),
        [AppLanguageKeys.SettingsTabHeader] = ("Settings", "Settings"),
        [AppLanguageKeys.AppearanceHeader] = ("Appearance", "Appearance"),
        [AppLanguageKeys.DarkModeLabel] = ("Dark mode", "Tabby Mode"),
        [AppLanguageKeys.AlwaysOnTopLabel] = ("Always on top", "Always on top"),
        [AppLanguageKeys.CatTranslatorLabel] = ("Cat Translator", "Cat Translator :3"),
        [AppLanguageKeys.AutoHideOnStartupLabel] = ("Auto-hide to tray on startup", "Auto-hide to tray on startup"),
        [AppLanguageKeys.ResetAllSettingsButton] = ("Reset All Settings", "Reset All Settings"),
        [AppLanguageKeys.BugCheckingHeader] = ("Bug Checking", "Bug Checking :3"),
        [AppLanguageKeys.BugCheckingHelperText] = (
            "If something looks off, use Copy Log and paste it when reporting the bug.",
            "If something looks off, use Copy Log and paste it when repurrting the bug."),
        [AppLanguageKeys.CopyLogButton] = ("Copy Log", "Copy Log"),
        [AppLanguageKeys.MainAdminBannerAdmin] = ("MultiTool is running as administrator.", "MultiTool is running as administrator."),
        [AppLanguageKeys.MainAdminBannerNotAdmin] = ("Running without administrator access. Open MultiTool as administrator for installs, hardware sensors, drivers, and Windows changes that need elevated access.", "Running without administrator access. Open MultiTool as administrator for installs, hardware sensors, drivers, and Windows changes that need elevated access."),
        [AppLanguageKeys.MainAdminActivityNotAdmin] = ("Running without administrator access. Open MultiTool as administrator for full access to elevated features.", "Running without administrator access. Open MultiTool as administrator for full access to elevated features."),
        [AppLanguageKeys.MainWindowTitleDefault] = ("MultiTool", "MultiTool"),
        [AppLanguageKeys.MainWindowTitleRunning] = ("MultiTool - Running...", "MultiTool - Running..."),
        [AppLanguageKeys.MainWindowTitleNotAdminSuffix] = (" (Not Admin)", " (Not Admin)"),
        [AppLanguageKeys.MainStatusLoadingSettings] = ("Loading settings...", "Loading settings..."),
        [AppLanguageKeys.MainCustomKeyPrompt] = ("Click here and press a key", "Click here and press a key"),
        [AppLanguageKeys.MainCustomKeyOrMousePrompt] = ("Click here and press a key or mouse button", "Click here and press a key or mouse button"),
        [AppLanguageKeys.HotkeyEditToolTip] = ("Click here, then press the new hotkey.", "Click here, then press the new hotkey."),
        [AppLanguageKeys.HotkeyLabel] = ("Hotkey", "Hotkey"),
        [AppLanguageKeys.CaptureButton] = ("Capture", "Capture"),
        [AppLanguageKeys.BrowseButton] = ("Browse", "Browse"),
        [AppLanguageKeys.CaptureScreenButton] = ("Capture Screen", "Capture Screen"),
        [AppLanguageKeys.OpenFolderButton] = ("Open Folder", "Open Folder"),
        [AppLanguageKeys.NewMacroButton] = ("New Macro", "New Macro"),
        [AppLanguageKeys.RecordButton] = ("Record", "Record"),
        [AppLanguageKeys.StopButton] = ("Stop", "Stop"),
        [AppLanguageKeys.PlayButton] = ("Play", "Play"),
        [AppLanguageKeys.SaveButton] = ("Save", "Save"),
        [AppLanguageKeys.LoadButton] = ("Load", "Load"),
        [AppLanguageKeys.RefreshButton] = ("Refresh", "Refresh"),
        [AppLanguageKeys.LoadSelectedButton] = ("Load Selected", "Load Selected"),
        [AppLanguageKeys.EditSelectedButton] = ("Edit Selected", "Edit Selected"),
        [AppLanguageKeys.MainMouseButtonLeft] = ("Left Mouse Button", "Left Mouse Button"),
        [AppLanguageKeys.MainMouseButtonRight] = ("Right Mouse Button", "Right Mouse Button"),
        [AppLanguageKeys.MainMouseButtonMiddle] = ("Middle Mouse Button", "Middle Mouse Button"),
        [AppLanguageKeys.MainMouseButton4] = ("Mouse Button 4", "Mouse Button 4"),
        [AppLanguageKeys.MainMouseButton5] = ("Mouse Button 5", "Mouse Button 5"),
        [AppLanguageKeys.MainScreenshotFilePrefixDefault] = ("Screenshot", "Screenshot"),
        [AppLanguageKeys.MainIntervalLabel] = ("Interval", "Interval"),
        [AppLanguageKeys.MainHoursLabel] = ("Hours", "Hours"),
        [AppLanguageKeys.MainMinutesLabel] = ("Minutes", "Minutes"),
        [AppLanguageKeys.MainSecondsLabel] = ("Seconds", "Seconds"),
        [AppLanguageKeys.MainMillisecondsLabel] = ("Milliseconds", "Milliseconds"),
        [AppLanguageKeys.MainRepeatLabel] = ("Repeat", "Repeat"),
        [AppLanguageKeys.MainPositionLabel] = ("Position", "Position"),
        [AppLanguageKeys.MainXLabel] = ("X", "X"),
        [AppLanguageKeys.MainYLabel] = ("Y", "Y"),
        [AppLanguageKeys.MainNameLabel] = ("Name", "Name"),
        [AppLanguageKeys.MainPlayCountLabel] = ("Play Count", "Play Count"),
        [AppLanguageKeys.MainRecordMouseMovementLabel] = ("Record mouse movement", "Record mouse movement"),
        [AppLanguageKeys.MainPlayHotkeyLabel] = ("Play Hotkey", "Play Hotkey"),
        [AppLanguageKeys.MainRecordHotkeyLabel] = ("Record Hotkey", "Record Hotkey"),
        [AppLanguageKeys.MainSavedLabel] = ("Saved", "Saved"),
        [AppLanguageKeys.MainNoAssignedMacroShortcuts] = ("No keyboard shortcuts are set yet.", "No keyboard shortcuts are set yet."),
        [AppLanguageKeys.MainInputLabel] = ("Input", "Input"),
        [AppLanguageKeys.MainTypeLabel] = ("Type", "Type"),
        [AppLanguageKeys.MainCustomInputLabel] = ("Custom Input", "Custom Input"),
        [AppLanguageKeys.MainFolderLabel] = ("Folder", "Folder"),
        [AppLanguageKeys.MainPrefixLabel] = ("Prefix", "Prefix"),
        [AppLanguageKeys.MainScreenshotHelperText] = (
            "Press the screenshot hotkey to save a full-screen PNG and copy it to the clipboard. Press Shift + that same hotkey to choose Full Screen, Area, or start and stop local Video recording.",
            "Press the screenshot hotkey to save a full-screen PNG and copy it to the clipboard. Press Shift + that same hotkey to choose Full Screen, Area, or start and stop local Video recording."),
        [AppLanguageKeys.MainScreenshotStatusReady] = ("Ready to capture the desktop.", "Ready to capture the desktop."),
        [AppLanguageKeys.MainLatestScreenshotNone] = ("No screenshot captured yet.", "No screenshot captured yet."),
        [AppLanguageKeys.MainLatestVideoNone] = ("No video recorded yet.", "No video recorded yet."),
        [AppLanguageKeys.MainLatestScreenshotPlaceholder] = ("Take a screenshot and the latest image will appear here.", "Take a screenshot and the latest image will appear here."),
        [AppLanguageKeys.MouseSensitivityVerySlow] = ("Very Slow", "Very Slow"),
        [AppLanguageKeys.MouseSensitivitySlow] = ("Slow", "Slow"),
        [AppLanguageKeys.MouseSensitivityBalanced] = ("Balanced", "Balanced"),
        [AppLanguageKeys.MouseSensitivityFast] = ("Fast", "Fast"),
        [AppLanguageKeys.MouseSensitivityVeryFast] = ("Very Fast", "Very Fast"),
        [AppLanguageKeys.MainMacroNameDefault] = ("New Macro", "New Macro"),
        [AppLanguageKeys.MainMacroLogReady] = ("Macro log ready.", "Macro log ready."),
        [AppLanguageKeys.MainMacroLogNoRecordedYet] = ("No macro recorded yet.", "No macro recorded yet."),
        [AppLanguageKeys.MainMacroSummaryNoRecorded] = ("No macro recorded yet.", "No macro recorded yet."),
        [AppLanguageKeys.MainMacroStatusReady] = ("Ready for recording or playback setup.", "Ready for recording or playback setup."),
        [AppLanguageKeys.MainSettingsStatusInitial] = ("Dark mode will match Windows the first time the app runs.", "Dark mode will match Windows the first time the app runs."),
        [AppLanguageKeys.MainSettingsStatusDarkModeOn] = ("Dark mode is on.", "Dark mode is on."),
        [AppLanguageKeys.MainSettingsStatusDarkModeOff] = ("Dark mode is off.", "Dark mode is off."),
        [AppLanguageKeys.MainActivityDarkModeEnabled] = ("Dark mode enabled.", "Dark mode enabled."),
        [AppLanguageKeys.MainActivityDarkModeDisabled] = ("Dark mode disabled.", "Dark mode disabled."),
        [AppLanguageKeys.MainSettingsStatusCtrlWheelZoomOn] = ("Ctrl + mouse wheel UI zoom is on.", "Ctrl + mouse wheel UI zoom is on."),
        [AppLanguageKeys.MainSettingsStatusCtrlWheelZoomOff] = ("Ctrl + mouse wheel UI zoom is off.", "Ctrl + mouse wheel UI zoom is off."),
        [AppLanguageKeys.MainActivityCtrlWheelZoomEnabled] = ("Enabled Ctrl + mouse wheel UI zoom.", "Enabled Ctrl + mouse wheel UI zoom."),
        [AppLanguageKeys.MainActivityCtrlWheelZoomDisabled] = ("Disabled Ctrl + mouse wheel UI zoom.", "Disabled Ctrl + mouse wheel UI zoom."),
        [AppLanguageKeys.MainSettingsStatusAutoHideOn] = ("Auto-hide on startup is on.", "Auto-hide on startup is on."),
        [AppLanguageKeys.MainSettingsStatusAutoHideOff] = ("Auto-hide on startup is off.", "Auto-hide on startup is off."),
        [AppLanguageKeys.MainActivityAutoHideEnabled] = ("Enabled auto-hide on startup.", "Enabled auto-hide on startup."),
        [AppLanguageKeys.MainActivityAutoHideDisabled] = ("Disabled auto-hide on startup.", "Disabled auto-hide on startup."),
        [AppLanguageKeys.MainSettingsStatusCatTranslatorOn] = ("Cat Translator is on. Meow.", "Cat Translator is on. Meow."),
        [AppLanguageKeys.MainActivityCatTranslatorEnabled] = ("Enabled Cat Translator.", "Enabled Cat Translator."),
        [AppLanguageKeys.MainActivityCatTranslatorDisabled] = ("Disabled Cat Translator.", "Disabled Cat Translator."),
        [AppLanguageKeys.MainStatusReady] = ("Ready.", "Ready."),
        [AppLanguageKeys.MainActivityLogReady] = ("Activity log ready.", "Activity log ready."),
        [AppLanguageKeys.MainActivitySettingsLoaded] = ("Settings loaded.", "Settings loaded."),
        [AppLanguageKeys.MainStatusClicking] = ("Clicking...", "Clicking..."),
        [AppLanguageKeys.MainStatusAutomationStopped] = ("Automation stopped.", "Automation stopped."),
        [AppLanguageKeys.MainStatusSettingsSaved] = ("Settings saved.", "Settings saved."),
        [AppLanguageKeys.MainStatusCapturedCoordinatesFormat] = ("Captured coordinates: {0}, {1}.", "Captured coordinates: {0}, {1}."),
        [AppLanguageKeys.MainScreenshotStatusOpenedFolder] = ("Opened screenshot folder.", "Opened screenshot folder."),
        [AppLanguageKeys.MainScreenshotStatusOpenFolderFailedFormat] = ("Unable to open screenshot folder: {0}", "Unable to open screenshot folder: {0}"),
        [AppLanguageKeys.MainMacroStatusStartedNew] = ("Started a new macro.", "Started a new macro."),
        [AppLanguageKeys.MainMacroSummaryRecordingFormat] = ("Recording '{0}'...", "Recording '{0}'..."),
        [AppLanguageKeys.MainMacroStatusRecordingWithMouseFormat] = ("Recording '{0}' with mouse movement. Input inside this window is ignored while it stays focused.", "Recording '{0}' with mouse movement. Input inside this window is ignored while it stays focused."),
        [AppLanguageKeys.MainMacroStatusRecordingWithoutMouseFormat] = ("Recording '{0}' without mouse movement. Input inside this window is ignored while it stays focused.", "Recording '{0}' without mouse movement. Input inside this window is ignored while it stays focused."),
        [AppLanguageKeys.MainMacroLogStartedRecordingFormat] = ("Started recording '{0}'.", "Started recording '{0}'."),
        [AppLanguageKeys.MainMacroStatusStartRecordingFailedFormat] = ("Unable to start recording: {0}", "Unable to start recording: {0}"),
        [AppLanguageKeys.MainMacroStatusCannotRecordWhilePlaying] = ("Cannot start recording while a macro is playing.", "Cannot start recording while a macro is playing."),
        [AppLanguageKeys.MainMacroLogRecordHotkeyIgnoredPlaying] = ("Record hotkey ignored because a macro is currently playing.", "Record hotkey ignored because a macro is currently playing."),
        [AppLanguageKeys.MainMacroSummaryRecordedFormat] = ("{0}: {1} events over {2:N0} ms", "{0}: {1} events over {2:N0} ms"),
        [AppLanguageKeys.MainMacroSummaryNoInputCapturedFormat] = ("{0}: no input captured", "{0}: no input captured"),
        [AppLanguageKeys.MainMacroStatusStoppedRecordingFormat] = ("Stopped recording '{0}'.", "Stopped recording '{0}'."),
        [AppLanguageKeys.MainMacroStatusStoppedRecordingNoInputFormat] = ("Stopped recording '{0}', but no input was captured.", "Stopped recording '{0}', but no input was captured."),
        [AppLanguageKeys.MainMacroLogStoppedRecordingFormat] = ("Stopped recording '{0}' with {1} events.", "Stopped recording '{0}' with {1} events."),
        [AppLanguageKeys.MainMacroStatusStopRecordingFailedFormat] = ("Unable to stop recording: {0}", "Unable to stop recording: {0}"),
        [AppLanguageKeys.MainMacroStatusNoRecordedToPlay] = ("There is no recorded macro to play.", "There is no recorded macro to play."),
        [AppLanguageKeys.MainMacroLogPlaybackRequestedNoMacro] = ("Playback requested, but no recorded macro is available.", "Playback requested, but no recorded macro is available."),
        [AppLanguageKeys.MainMacroStatusPlayingFormat] = ("Playing '{0}' x{1}.", "Playing '{0}' x{1}."),
        [AppLanguageKeys.MainMacroStatusFinishedPlayingFormat] = ("Finished playing '{0}'.", "Finished playing '{0}'."),
        [AppLanguageKeys.MainMacroStatusPlayFailedFormat] = ("Unable to play macro: {0}", "Unable to play macro: {0}"),
        [AppLanguageKeys.MainMacroStatusNoRecordedToSave] = ("There is no recorded macro to save.", "There is no recorded macro to save."),
        [AppLanguageKeys.MainMacroLogSaveRequestedNoMacro] = ("Save requested, but no macro is available.", "Save requested, but no macro is available."),
        [AppLanguageKeys.MainMacroStatusSaveCanceled] = ("Save canceled.", "Save canceled."),
        [AppLanguageKeys.MainMacroStatusSavedToMacrosFormat] = ("Saved macro to Macros\\{0}.", "Saved macro to Macros\\{0}."),
        [AppLanguageKeys.MainMacroLogSavedToPathFormat] = ("Saved macro to {0}.", "Saved macro to {0}."),
        [AppLanguageKeys.MainMacroStatusSaveFailedFormat] = ("Unable to save macro: {0}", "Unable to save macro: {0}"),
        [AppLanguageKeys.MainMacroStatusLoadCanceled] = ("Load canceled.", "Load canceled."),
        [AppLanguageKeys.MainMacroStatusLoadedFromFileFormat] = ("Loaded macro from {0}.", "Loaded macro from {0}."),
        [AppLanguageKeys.MainMacroLogLoadedFromPathFormat] = ("Loaded macro from {0}.", "Loaded macro from {0}."),
        [AppLanguageKeys.MainMacroStatusLoadFailedFormat] = ("Unable to load macro: {0}", "Unable to load macro: {0}"),
        [AppLanguageKeys.MainMacroStatusChooseSavedFirst] = ("Choose a saved macro first.", "Choose a saved macro first."),
        [AppLanguageKeys.MainMacroLogLoadSelectedNoSaved] = ("Load selected requested, but no saved macro is selected.", "Load selected requested, but no saved macro is selected."),
        [AppLanguageKeys.MainMacroStatusLoadedSavedFormat] = ("Loaded saved macro '{0}'.", "Loaded saved macro '{0}'."),
        [AppLanguageKeys.MainMacroLogLoadedSavedPathFormat] = ("Loaded saved macro from {0}.", "Loaded saved macro from {0}."),
        [AppLanguageKeys.MainMacroStatusLoadSavedFailedFormat] = ("Unable to load saved macro: {0}", "Unable to load saved macro: {0}"),
        [AppLanguageKeys.MainMacroLogEditRequestedNoSaved] = ("Edit requested, but no saved macro is selected.", "Edit requested, but no saved macro is selected."),
        [AppLanguageKeys.MainMacroStatusEditCanceled] = ("Edit canceled.", "Edit canceled."),
        [AppLanguageKeys.MainMacroStatusSavedEditsFormat] = ("Saved edits to '{0}'.", "Saved edits to '{0}'."),
        [AppLanguageKeys.MainMacroLogSavedEditedToPathFormat] = ("Saved edited macro to {0}.", "Saved edited macro to {0}."),
        [AppLanguageKeys.MainMacroStatusEditSavedFailedFormat] = ("Unable to edit saved macro: {0}", "Unable to edit saved macro: {0}"),
        [AppLanguageKeys.MainMacroStatusFoundSavedFormat] = ("Found {0} saved macro{1}.", "Found {0} saved macro{1}."),
        [AppLanguageKeys.MainMacroStatusNoSavedInDefaultFolder] = ("No saved macros found in the default macros folder.", "No saved macros found in the default macros folder."),
        [AppLanguageKeys.MainMacroStatusSavedFolderUnavailableFormat] = ("Saved macros folder is unavailable: {0}", "Saved macros folder is unavailable: {0}"),
        [AppLanguageKeys.MainMacroStatusOpenedSavedFolder] = ("Opened the saved macros folder.", "Opened the saved macros folder."),
        [AppLanguageKeys.MainMacroStatusOpenSavedFolderFailedFormat] = ("Unable to open the saved macros folder: {0}", "Unable to open the saved macros folder: {0}"),
        [AppLanguageKeys.MainMacroStatusLogCleared] = ("Macro log cleared.", "Macro log cleared."),
        [AppLanguageKeys.MainStatusSettingsAutoSaved] = ("Settings auto-saved.", "Settings auto-saved."),
        [AppLanguageKeys.MainStatusCustomKeySetFormat] = ("Custom key set to {0}.", "Custom key set to {0}."),
        [AppLanguageKeys.MainScreenshotStatusHotkeySetFormat] = ("Screenshot hotkey set to {0}.", "Screenshot hotkey set to {0}."),
        [AppLanguageKeys.MainStatusClickerHotkeySetFormat] = ("Clicker hotkey set to {0}.", "Clicker hotkey set to {0}."),
        [AppLanguageKeys.MainMacroStatusHotkeySetFormat] = ("Macro hotkey set to {0}.", "Macro hotkey set to {0}."),
        [AppLanguageKeys.MainMacroStatusRecordHotkeySetFormat] = ("Macro record hotkey set to {0}.", "Macro record hotkey set to {0}."),
        [AppLanguageKeys.MainStatusCustomInputSetFormat] = ("Custom input set to {0}.", "Custom input set to {0}."),
        [AppLanguageKeys.MainScreenshotFolderPickerPrompt] = ("Select the folder to save screenshots in", "Select the folder to save screenshots in"),
        [AppLanguageKeys.MainScreenshotStatusFolderSelectionCanceled] = ("Folder selection canceled.", "Folder selection canceled."),
        [AppLanguageKeys.MainScreenshotStatusFolderSetFormat] = ("Screenshot folder set to {0}.", "Screenshot folder set to {0}."),
        [AppLanguageKeys.MainActivityLogEmpty] = ("Activity log is empty.", "Activity log is empty."),
        [AppLanguageKeys.MainSettingsStatusCopiedActivityLog] = ("Copied activity log to the clipboard.", "Copied activity log to the clipboard."),
        [AppLanguageKeys.MainSettingsStatusResetRequested] = ("Settings reset to defaults.", "Settings reset to defaults."),
        [AppLanguageKeys.MainSettingsStatusResetCompleted] = ("All settings were reset to defaults.", "All settings were reset to defaults."),
        [AppLanguageKeys.MainSettingsStatusResetSaveFailed] = ("Reset applied in the UI, but saving the defaults failed.", "Reset applied in the UI, but saving the defaults failed."),
        [AppLanguageKeys.MainScreenshotStatusSavedVideoFormat] = ("Saved video: {0}.", "Saved video: {0}."),
        [AppLanguageKeys.MainScreenshotStatusVideoHandledInOptionsWindow] = ("Video recording was handled in the screenshot options window.", "Video recording was handled in the screenshot options window."),
        [AppLanguageKeys.MainScreenshotStatusOptionsCanceled] = ("Screenshot options canceled.", "Screenshot options canceled."),
        [AppLanguageKeys.MainScreenshotStatusSavedAndCopiedFormat] = ("Saved {0} and copied it to the clipboard.", "Saved {0} and copied it to the clipboard."),
        [AppLanguageKeys.MainScreenshotLogSavedFullScreenFormat] = ("Saved full-screen capture to {0} and copied it to the clipboard.", "Saved full-screen capture to {0} and copied it to the clipboard."),
        [AppLanguageKeys.MainScreenshotStatusAreaCanceled] = ("Area capture canceled.", "Area capture canceled."),
        [AppLanguageKeys.MainScreenshotLogSavedAreaFormat] = ("Saved area capture to {0} and copied it to the clipboard.", "Saved area capture to {0} and copied it to the clipboard."),
        [AppLanguageKeys.MainScreenshotStatusFailedFormat] = ("Screenshot failed: {0}", "Screenshot failed: {0}"),
        [AppLanguageKeys.MainStatusUnableSaveSettingsFormat] = ("Unable to save settings: {0}", "Unable to save settings: {0}"),
        [AppLanguageKeys.TrayStartupHiddenStatus] = (
            "MultiTool started hidden in the tray.",
            "MultiTool started hiding in the tray :3"),
        [AppLanguageKeys.TrayMinimizedStatus] = (
            "MultiTool was minimized to the tray.",
            "MultiTool was minimized to the tray :3"),
        [AppLanguageKeys.HotkeysRegisterWhenReadyStatus] = (
            "Hotkeys will register once the main window is ready.",
            "Pawkeys will register once the main window is ready."),

        [AppLanguageKeys.AboutWindowTitle] = ("About", "About"),
        [AppLanguageKeys.AboutSubtitle] = (
            "Desktop utility with clicker, screenshot, and macro tools.",
            "Desktop utility with clicker, screenshot, and meowcro tools."),
        [AppLanguageKeys.AboutCloseButton] = ("Close", "Close"),
        [AppLanguageKeys.AboutVersionFormat] = ("Version {0}", "Version {0}"),

        [AppLanguageKeys.CoordinateCaptureInstruction] = (
            "Click anywhere to capture coordinates",
            "Click anywhere to capture paw-ordinates"),
        [AppLanguageKeys.CoordinateCaptureEscHint] = (
            "Press Esc to cancel.",
            "Press Esc to cancel."),
        [AppLanguageKeys.CoordinateCapturePositionFormat] = ("X: {0}  Y: {1}", "X: {0}  Y: {1}"),

        [AppLanguageKeys.AreaSelectionInstruction] = (
            "Drag to select an area",
            "Drag to select an area"),
        [AppLanguageKeys.AreaSelectionEscHint] = (
            "Release to capture. Press Esc to cancel.",
            "Release to capture. Press Esc to cancel."),

        [AppLanguageKeys.HotkeySettingsTitle] = ("Clicker Hotkey Settings", "Clicker Hotkey Settings"),
        [AppLanguageKeys.HotkeySettingsCaptureTooltip] = (
            "Click here, then press the new hotkey.",
            "Click here, then press the new pawkey."),
        [AppLanguageKeys.HotkeySettingsToggleLabel] = ("Clicker Start/Stop Hotkey", "Clicker Start/Stop Hotkey"),
        [AppLanguageKeys.HotkeySettingsPinWindowLabel] = (
            "Pin Window Hotkey (Tools tab)",
            "Pin Window Hotkey (Cat Tools tab)"),
        [AppLanguageKeys.HotkeySettingsWaitingAnyKey] = (
            "Waiting for a key or mouse button...",
            "Waiting for a key or mouse button..."),
        [AppLanguageKeys.HotkeySettingsWaitingKey] = ("Waiting for a key...", "Waiting for a key..."),
        [AppLanguageKeys.HotkeySettingsResetButton] = ("Reset", "Reset"),
        [AppLanguageKeys.HotkeySettingsCancelButton] = ("Cancel", "Cancel"),
        [AppLanguageKeys.HotkeySettingsSaveButton] = ("Save", "Save"),

        [AppLanguageKeys.MacroEditorTitle] = ("Edit Macro", "Edit Meowcro"),
        [AppLanguageKeys.MacroEditorNameLabel] = ("Macro name", "Macro name"),
        [AppLanguageKeys.MacroEditorDescription] = (
            "Choose an event from the top list, then use the details panel below to change what it does. Offsets are in milliseconds from the start of the macro.",
            "Choose an event from the top list, then use the details panel below to change what it does. Offsets are in milliseconds from the start of the meowcro."),
        [AppLanguageKeys.MacroEditorEventsHeader] = ("Events", "Events"),
        [AppLanguageKeys.MacroEditorPickEventHint] = ("Pick the event you want to edit.", "Pick the event you want to edit."),
        [AppLanguageKeys.MacroEditorAddEventButton] = ("Add Event", "Add Event"),
        [AppLanguageKeys.MacroEditorRemoveSelectedButton] = ("Remove Selected", "Remove Selected"),
        [AppLanguageKeys.MacroEditorSortByOffsetButton] = ("Sort by Offset", "Sort by Offset"),
        [AppLanguageKeys.MacroEditorColumnNumber] = ("#", "#"),
        [AppLanguageKeys.MacroEditorColumnOffset] = ("Offset", "Offset"),
        [AppLanguageKeys.MacroEditorColumnAction] = ("Action", "Action"),
        [AppLanguageKeys.MacroEditorColumnDetails] = ("Details", "Details"),
        [AppLanguageKeys.MacroEditorSelectedEventHeader] = ("Selected Event", "Selected Event"),
        [AppLanguageKeys.MacroEditorSelectedEventNull] = ("Choose an event from the list first.", "Choose an event from the list first."),
        [AppLanguageKeys.MacroEditorActionLabel] = ("Action", "Action"),
        [AppLanguageKeys.MacroEditorOffsetLabel] = ("Offset (ms)", "Offset (ms)"),
        [AppLanguageKeys.MacroEditorKeyCodeLabel] = ("Key Code", "Key Code"),
        [AppLanguageKeys.MacroEditorMouseButtonLabel] = ("Mouse Button", "Mouse Button"),
        [AppLanguageKeys.MacroEditorXPositionLabel] = ("X Position", "X Position"),
        [AppLanguageKeys.MacroEditorYPositionLabel] = ("Y Position", "Y Position"),
        [AppLanguageKeys.MacroEditorFieldHint] = (
            "Only the fields that matter for the selected action stay enabled.",
            "Only the fields that matter for the selected action stay enabled."),
        [AppLanguageKeys.MacroEditorCancelButton] = ("Cancel", "Cancel"),
        [AppLanguageKeys.MacroEditorSaveButton] = ("Save", "Save"),
        [AppLanguageKeys.MacroEditorStatusInitial] = (
            "Pick an event on the left, adjust its details on the right, then press Save.",
            "Pick an event on the left, adjust its details on the right, then press Save."),
        [AppLanguageKeys.MacroEditorStatusAdded] = (
            "Added a new event. You can edit its details on the right.",
            "Added a new event. You can edit its details on the right."),
        [AppLanguageKeys.MacroEditorStatusRemoved] = ("Removed the selected event.", "Removed the selected event."),
        [AppLanguageKeys.MacroEditorStatusSorted] = ("Sorted events by offset.", "Sorted events by offset."),
        [AppLanguageKeys.MacroEditorErrorEnterName] = ("Enter a macro name before saving.", "Enter a macro name before saving."),
        [AppLanguageKeys.MacroEditorSummaryEventsFormat] = ("{0} event(s)", "{0} event(s)"),
        [AppLanguageKeys.MacroEditorSelectedHintNone] = (
            "Select an event from the list to start editing it.",
            "Select an event from the list to start editing it."),
        [AppLanguageKeys.MacroEditorSelectedHintKeyboard] = (
            "Keyboard events only use the Action, Offset, and Key Code fields.",
            "Keyboard events only use the Action, Offset, and Key Code fields."),
        [AppLanguageKeys.MacroEditorSelectedHintMouseMove] = (
            "Mouse move events only use the Action, Offset, and X / Y position fields.",
            "Mouse move events only use the Action, Offset, and X / Y position fields."),
        [AppLanguageKeys.MacroEditorSelectedHintMouseButton] = (
            "Mouse button events use the Action, Offset, Mouse Button, and X / Y position fields.",
            "Mouse button events use the Action, Offset, Mouse Button, and X / Y position fields."),

        [AppLanguageKeys.MacroHotkeyAssignmentsTitle] = ("Macro Keyboard Shortcuts", "Macro Keyboard Shortcuts"),
        [AppLanguageKeys.MacroHotkeyAssignmentsHeading] = ("Macro Keyboard Shortcuts", "Macro Keyboard Shortcuts"),
        [AppLanguageKeys.MacroHotkeyAssignmentsDescription] = (
            "Choose a keyboard shortcut for each saved macro. 'Run once' plays it one time. 'Start/stop' keeps it running until you press the same key again.",
            "Choose a keyboard shortcut for each saved meowcro. 'Run once' plays it one time. 'Start/stop' keeps it running until you press the same key again."),
        [AppLanguageKeys.MacroHotkeyAssignmentsEmpty] = (
            "No saved macros found yet. Save a macro on the Macro tab first.",
            "No saved meowcros found yet. Save a meowcro on the Macro tab first."),
        [AppLanguageKeys.InstallerPackagePickerDescription] = (
            "Pick a batch of common apps and MultiTool will install them, update them, or scan the full list for available updates with winget in quiet mode.",
            "Pick a batch of common apps and MultiTool will install them, update them, or scan the full list for available updates with winget in quiet mode."),
        [AppLanguageKeys.InstallerSearchLabel] = ("Search", "Search"),
        [AppLanguageKeys.MacroHotkeyAssignmentsActive] = ("Active", "Active"),
        [AppLanguageKeys.MacroHotkeyAssignmentsShortcutKey] = ("Shortcut key", "Shortcut key"),
        [AppLanguageKeys.MacroHotkeyAssignmentsBehavior] = ("What this key does", "What this key does"),
        [AppLanguageKeys.MacroHotkeyAssignmentsRemoveKey] = ("Remove key", "Remove key"),
        [AppLanguageKeys.MacroHotkeyAssignmentsClear] = ("Clear", "Clear"),
        [AppLanguageKeys.MacroHotkeyAssignmentsCancel] = ("Cancel", "Cancel"),
        [AppLanguageKeys.MacroHotkeyAssignmentsSave] = ("Save", "Save"),
        [AppLanguageKeys.MacroHotkeyAssignmentsCaptureTooltip] = (
            "Click here, then press the keyboard shortcut you want to use for this macro.",
            "Click here, then press the keyboard shortcut you want to use for this meowcro."),
        [AppLanguageKeys.MacroHotkeyAssignmentsClickToAssign] = ("Click to assign", "Click to assign"),
        [AppLanguageKeys.MacroHotkeyAssignmentsStatusNoSaved] = (
            "No saved macros were found in the Macros folder.",
            "No saved meowcros were found in the Macros folder."),
        [AppLanguageKeys.MacroHotkeyAssignmentsStatusPick] = (
            "Pick a keyboard shortcut for any saved macro. 'Run once' plays it one time. 'Start/stop' keeps it running until you press the same key again.",
            "Pick a keyboard shortcut for any saved meowcro. 'Run once' plays it one time. 'Start/stop' keeps it running until you press the same key again."),
        [AppLanguageKeys.MacroHotkeyAssignmentsStatusAssignedFormat] = (
            "'{0}' will now run when you press {1}.",
            "'{0}' will now run when you press {1}."),
        [AppLanguageKeys.MacroHotkeyAssignmentsStatusRemovedFormat] = (
            "Removed the keyboard shortcut for '{0}'.",
            "Removed the keyboard shortcut for '{0}'."),

        [AppLanguageKeys.MacroNamePromptTitle] = ("Save Macro", "Save Meowcro"),
        [AppLanguageKeys.MacroNamePromptHeading] = ("Name this macro", "Name this meowcro"),
        [AppLanguageKeys.MacroNamePromptDescription] = (
            "Type the macro name below. Press Save and it will go straight into the Macros folder next to MultiTool.exe.",
            "Type the meowcro name below. Press Save and it will go straight into the Macros folder next to MultiTool.exe."),
        [AppLanguageKeys.MacroNamePromptNameLabel] = ("Macro name", "Meowcro name"),
        [AppLanguageKeys.MacroNamePromptSaveToLabel] = ("Will save to", "Will save to"),
        [AppLanguageKeys.MacroNamePromptErrorEnterName] = ("Enter a macro name.", "Enter a meowcro name."),
        [AppLanguageKeys.MacroNamePromptOverwriteHint] = (
            "Saving again with the same name will overwrite the existing macro.",
            "Saving again with the same name will overwrite the existing meowcro."),
        [AppLanguageKeys.MacroNamePromptCancel] = ("Cancel", "Cancel"),
        [AppLanguageKeys.MacroNamePromptSave] = ("Save", "Save"),
        [AppLanguageKeys.MacroNamePromptNameTooltip] = ("Enter the macro name here.", "Enter the meowcro name here."),
        [AppLanguageKeys.MacroNamePromptDefaultName] = ("New Macro", "New Meowcro"),
        [AppLanguageKeys.MacroNamePromptSavePreviewFormat] = ("{0}.acmacro.json", "{0}.acmacro.json"),

        [AppLanguageKeys.ShortcutExplorerTitle] = ("Shortcut Key Explorer", "Shortcut Key Explorer"),
        [AppLanguageKeys.ShortcutExplorerHeading] = ("Shortcut Key Explorer", "Shortcut Key Explorer"),
        [AppLanguageKeys.ShortcutExplorerSearchLabel] = ("Search", "Search"),
        [AppLanguageKeys.ShortcutExplorerConflictsOnly] = ("Show only conflicts", "Show only conflicts"),
        [AppLanguageKeys.ShortcutExplorerColumnHotkey] = ("Hotkey", "Hotkey"),
        [AppLanguageKeys.ShortcutExplorerColumnShortcut] = ("Shortcut", "Shortcut"),
        [AppLanguageKeys.ShortcutExplorerColumnSource] = ("Source", "Source"),
        [AppLanguageKeys.ShortcutExplorerColumnAppliesTo] = ("Applies To", "Applies To"),
        [AppLanguageKeys.ShortcutExplorerColumnConflict] = ("Conflict", "Conflict"),
        [AppLanguageKeys.ShortcutExplorerColumnDetails] = ("Details", "Details"),
        [AppLanguageKeys.ShortcutExplorerColumnFile] = ("Shortcut File", "Shortcut File"),
        [AppLanguageKeys.ShortcutExplorerClose] = ("Close", "Close"),
        [AppLanguageKeys.ShortcutExplorerSummaryNoneFormat] = (
            "Scanned {0} .lnk shortcut file{1} on fixed drives. No shortcut keys were found.",
            "Scanned {0} .lnk shortcut file{1} on fixed drives. No shortcut keys were found."),
        [AppLanguageKeys.ShortcutExplorerSummaryFoundFormat] = (
            "Found {0} assigned .lnk shortcut hotkey{1} after scanning {2} .lnk shortcut file{3} on fixed drives.",
            "Found {0} assigned .lnk shortcut hotkey{1} after scanning {2} .lnk shortcut file{3} on fixed drives."),
        [AppLanguageKeys.ShortcutExplorerSummaryNoAssigned] = (
            "No assigned .lnk shortcut hotkeys were found on this PC.",
            "No assigned .lnk shortcut hotkeys were found on this PC."),
        [AppLanguageKeys.ShortcutExplorerReferenceIncludedFormat] = (
            " Included {0} built-in Windows and common app shortcut reference entr{1}.",
            " Included {0} built-in Windows and common app shortcut reference entr{1}."),
        [AppLanguageKeys.ShortcutExplorerWarningSkippedFormat] = (
            "Skipped {0} folder or shortcut read{1} during the scan.",
            "Skipped {0} folder or shortcut read{1} during the scan."),
        [AppLanguageKeys.ShortcutExplorerReferenceNote] = (
            "Built-in Windows and common app shortcuts are included as a reference catalog. The scanner now also pulls real keymaps from supported apps when available, but Windows still has no universal API that exposes every private keybind from every program.",
            "Built-in Windows and common app shortcuts are included as a reference catalog. The scanner now also pulls real keymaps from supported apps when available, but Windows still has no universal API that exposes every private keybind from every program."),
        [AppLanguageKeys.ShortcutExplorerConflictWarningFormat] = (
            "Warning: {0} shortcut{1} share {2} hotkey{3}. Some overlaps are harmless reference combos, but detected Windows shortcut-file hotkeys can still conflict in real use.",
            "Warning: {0} shortcut{1} share {2} hotkey{3}. Some overlaps are harmless reference combos, but detected Windows shortcut-file hotkeys can still conflict in real use."),
        [AppLanguageKeys.ShortcutExplorerFilterListedFormat] = ("{0} shortcut{1} listed", "{0} shortcut{1} listed"),
        [AppLanguageKeys.ShortcutExplorerFilterMatchingFormat] = ("{0} matching shortcut{1} shown", "{0} matching shortcut{1} shown"),
        [AppLanguageKeys.ShortcutExplorerFilterConflictSuffixFormat] = (
            ". {0} shortcut{1} are in conflict across {2} shared hotkey{3}.",
            ". {0} shortcut{1} are in conflict across {2} shared hotkey{3}."),
        [AppLanguageKeys.ShortcutExplorerFilterSourceSuffixFormat] = (
            ". {0} detected from this PC, {1} built-in or common references",
            ". {0} detected from this PC, {1} built-in or common references"),

        [AppLanguageKeys.MacroHotkeyNoSelectedSummary] = (
            "Select a saved macro to set a keyboard shortcut.",
            "Select a saved meowcro to set a keyboard shortcut."),
        [AppLanguageKeys.MacroHotkeyNotSetForSelectedFormat] = ("No keyboard shortcut is set for '{0}' yet.", "No keyboard shortcut is set for '{0}' yet."),
        [AppLanguageKeys.MacroHotkeySelectedSummaryFormat] = (
            "Shortcut: {0}. Action: {1}. Turned {2}.",
            "Shortcut: {0}. Action: {1}. Turned {2}."),
        [AppLanguageKeys.MacroHotkeyDefaultAssignmentsSummary] = (
            "No saved macros have keyboard shortcuts yet.",
            "No saved meowcros have keyboard shortcuts yet."),
        [AppLanguageKeys.MacroHotkeyLogSetupUnavailableNoSaved] = (
            "Shortcut setup is unavailable because there are no saved macros yet.",
            "Shortcut setup is unavailable because there are no saved meowcros yet."),
        [AppLanguageKeys.MacroHotkeyLogChangesRejectedFormat] = (
            "Shortcut changes were rejected: {0}",
            "Shortcut changes were rejected: {0}"),
        [AppLanguageKeys.MacroHotkeyStatusStoppedActiveBecauseShortcutChanged] = (
            "Stopped the active repeating macro because its shortcut changed.",
            "Stopped the active repeating meowcro because its shortcut changed."),
        [AppLanguageKeys.MacroHotkeyStatusShortcutsUpdated] = (
            "Keyboard shortcuts updated.",
            "Keyboard shortcuts updated."),
        [AppLanguageKeys.MacroHotkeyStatusShortcutsUpdatedButSaveFailed] = (
            "The shortcuts were updated on screen, but saving them failed.",
            "The shortcuts were updated on screen, but saving them failed."),
        [AppLanguageKeys.MacroHotkeyStatusShortcutUpdated] = (
            "Keyboard shortcut updated.",
            "Keyboard shortcut updated."),
        [AppLanguageKeys.MacroHotkeyLogSetRequestedNoSaved] = (
            "Set shortcut was requested, but no saved macro is selected.",
            "Set shortcut was requested, but no saved meowcro is selected."),
        [AppLanguageKeys.MacroHotkeyLogChangesRejectedForFormat] = (
            "Shortcut changes for '{0}' were rejected: {1}",
            "Shortcut changes for '{0}' were rejected: {1}"),
        [AppLanguageKeys.MacroHotkeyStatusSaveMacroFirst] = ("Save a macro first, then set a keyboard shortcut for it.", "Save a meowcro first, then set a keyboard shortcut for it."),
        [AppLanguageKeys.MacroHotkeyStatusChangesCanceled] = ("Shortcut changes were canceled.", "Shortcut changes were canceled."),
        [AppLanguageKeys.MacroHotkeyStatusNoChanges] = ("No shortcut changes were made.", "No shortcut changes were made."),
        [AppLanguageKeys.MacroHotkeyStatusChooseSavedMacro] = ("Choose a saved macro first.", "Choose a saved meowcro first."),
        [AppLanguageKeys.MacroHotkeyStatusChangesCanceledForFormat] = ("Shortcut changes for '{0}' were canceled.", "Shortcut changes for '{0}' were canceled."),
        [AppLanguageKeys.MacroHotkeyStatusUpdatedForFormat] = ("Updated the keyboard shortcut for '{0}'.", "Updated the keyboard shortcut for '{0}'."),
        [AppLanguageKeys.MacroHotkeyStatusUpdatedButSaveFailedForFormat] = ("Updated the shortcut for '{0}' on screen, but saving it failed.", "Updated the shortcut for '{0}' on screen, but saving it failed."),
        [AppLanguageKeys.MacroHotkeyStatusNoLongerSet] = ("That shortcut is no longer set.", "That shortcut is no longer set."),
        [AppLanguageKeys.MacroHotkeyLogIgnoredAssignmentMissing] = (
            "Ignored a shortcut because its assignment no longer exists.",
            "Ignored a shortcut because its assignment no longer exists."),
        [AppLanguageKeys.MacroHotkeyStatusRecordingConflict] = ("You can't run a saved macro from a shortcut while recording.", "You can't run a saved meowcro from a shortcut while recording."),
        [AppLanguageKeys.MacroHotkeyLogIgnoredRecordingActiveFormat] = (
            "Ignored shortcut for '{0}' because a recording is active.",
            "Ignored shortcut for '{0}' because a recording is active."),
        [AppLanguageKeys.MacroHotkeyStatusAnotherPlaying] = ("Another macro is already playing.", "Another meowcro is already playing."),
        [AppLanguageKeys.MacroHotkeyLogIgnoredAnotherPlayingFormat] = (
            "Ignored shortcut for '{0}' because another macro is already playing.",
            "Ignored shortcut for '{0}' because another meowcro is already playing."),
        [AppLanguageKeys.MacroHotkeyStatusStoppedFormat] = ("Stopped '{0}'.", "Stopped '{0}'."),
        [AppLanguageKeys.MacroHotkeyStatusStoppedAndStartedFormat] = (
            "Stopped '{0}' and started '{1}'.",
            "Stopped '{0}' and started '{1}'."),
        [AppLanguageKeys.MacroHotkeyStatusStoppedToRunOnceFormat] = (
            "Stopped '{0}' so '{1}' could run once.",
            "Stopped '{0}' so '{1}' could run once."),
        [AppLanguageKeys.MacroHotkeyStatusRunningOnceFormat] = ("Running '{0}' once.", "Running '{0}' once."),
        [AppLanguageKeys.MacroHotkeyLogRunningOnceFromFormat] = (
            "Running '{0}' once from {1}.",
            "Running '{0}' once from {1}."),
        [AppLanguageKeys.MacroHotkeyStatusFinishedFormat] = ("Finished '{0}'.", "Finished '{0}'."),
        [AppLanguageKeys.MacroHotkeyStatusRunFailedFormat] = ("Couldn't run '{0}': {1}", "Couldn't run '{0}': {1}"),
        [AppLanguageKeys.MacroHotkeyStatusToggleRunningFormat] = ("'{0}' is running. Press {1} again to stop.", "'{0}' is running. Press {1} again to stop."),
        [AppLanguageKeys.MacroHotkeyLogStartedFromAndPressAgainFormat] = (
            "Started '{0}' from {1}. Press the same key again to stop.",
            "Started '{0}' from {1}. Press the same key again to stop."),
        [AppLanguageKeys.MacroHotkeyStatusToggleStoppedErrorFormat] = ("'{0}' stopped because of an error: {1}", "'{0}' stopped because of an error: {1}"),
        [AppLanguageKeys.MacroHotkeyStatusMissingFileFormat] = ("'{0}' couldn't be found anymore.", "'{0}' couldn't be found anymore."),
        [AppLanguageKeys.MacroHotkeyLogMissingFilePathFormat] = (
            "Shortcut for '{0}' points to a missing file: {1}",
            "Shortcut for '{0}' points to a missing file: {1}"),
        [AppLanguageKeys.MacroHotkeyStatusNoEventsFormat] = ("'{0}' has nothing recorded to run.", "'{0}' has nothing recorded to run."),
        [AppLanguageKeys.MacroHotkeyLogNoEventsFormat] = (
            "Shortcut for '{0}' was skipped because the macro file has no events.",
            "Shortcut for '{0}' was skipped because the meowcro file has no events."),
        [AppLanguageKeys.MacroHotkeyStatusLoadFailedFormat] = ("Couldn't load '{0}': {1}", "Couldn't load '{0}': {1}"),
        [AppLanguageKeys.MacroHotkeyStatusStoppedActiveFileUnavailable] = (
            "Stopped the active repeating macro because its file is no longer available.",
            "Stopped the active repeating meowcro because its file is no longer available."),
        [AppLanguageKeys.MacroHotkeyLogUpdatedAfterSavedMacrosChanged] = (
            "Updated macro shortcuts after the saved macros list changed.",
            "Updated meowcro shortcuts after the saved macros list changed."),
        [AppLanguageKeys.MacroHotkeySummaryFormat] = ("{0} shortcut{1} set. {2} turned on. {3} set to start and stop with the same key.", "{0} shortcut{1} set. {2} turned on. {3} set to start and stop with the same key."),
        [AppLanguageKeys.MacroHotkeyPlaybackModeRunOnce] = ("Run once", "Run once"),
        [AppLanguageKeys.MacroHotkeyPlaybackModeToggleRepeat] = ("Start and stop with the same key", "Start and stop with the same key"),
        [AppLanguageKeys.MacroHotkeyOnState] = ("On", "On"),
        [AppLanguageKeys.MacroHotkeyOffState] = ("Off", "Off"),
        [AppLanguageKeys.MacroHotkeyActiveFallbackName] = ("the active saved macro", "the active saved macro"),

        [AppLanguageKeys.MacroEventItemPositionNotUsed] = ("Not used", "Not used"),
        [AppLanguageKeys.MacroEventItemDetailsMoveMouseFormat] = ("Move mouse to {0}, {1}", "Move mouse to {0}, {1}"),
        [AppLanguageKeys.MacroEventItemDetailsMouseDownFormat] = ("{0} down at {1}, {2}", "{0} down at {1}, {2}"),
        [AppLanguageKeys.MacroEventItemDetailsMouseUpFormat] = ("{0} up at {1}, {2}", "{0} up at {1}, {2}"),
        [AppLanguageKeys.MacroEventItemDetailsKeyDownFormat] = ("{0} down", "{0} down"),
        [AppLanguageKeys.MacroEventItemDetailsKeyUpFormat] = ("{0} up", "{0} up"),
        [AppLanguageKeys.MacroEventItemDetailsUnavailable] = ("Event details unavailable", "Event details unavailable"),
        [AppLanguageKeys.MacroEventItemVirtualKeyNone] = ("None", "None"),
        [AppLanguageKeys.MacroEventItemVirtualKeyOnlyFormat] = ("VK {0}", "VK {0}"),
        [AppLanguageKeys.MacroEventItemVirtualKeyNamedFormat] = ("{0} (VK {1})", "{0} (VK {1})"),

        [AppLanguageKeys.InstallerStatusPreparingCatalog] = ("Preparing the installer catalog...", "Preparing the installer catalog..."),
        [AppLanguageKeys.InstallerEnvironmentDefault] = ("The installer tab uses winget for silent installs and updates.", "The installer tab uses winget for silent installs and updates."),
        [AppLanguageKeys.InstallerAppUpdateSummaryDefault] = ("MultiTool release checks run with Check All Updates.", "MultiTool release checks run with Check All Updates."),
        [AppLanguageKeys.InstallerStatusCleanupLoading] = ("Cleanup options are loading...", "Cleanup options are loading..."),
        [AppLanguageKeys.InstallerSetupFailedFormat] = ("Installer setup failed: {0}", "Installer setup failed: {0}"),
        [AppLanguageKeys.InstallerCheckingTrackedAppsFormat] = ("Checking {0} tracked app{1} for updates...", "Checking {0} tracked app{1} for updates..."),
        [AppLanguageKeys.InstallerSelectedRecommended] = ("Selected the recommended starter apps.", "Selected the recommended starter apps."),
        [AppLanguageKeys.InstallerSelectedDeveloper] = ("Selected the developer stack.", "Selected the developer stack."),
        [AppLanguageKeys.InstallerSelectionCleared] = ("Cleared the installer selection.", "Cleared the installer selection."),
        [AppLanguageKeys.CleanupSelectedRecommended] = ("Selected the recommended cleanup apps.", "Selected the recommended cleanup apps."),
        [AppLanguageKeys.CleanupSelectionCleared] = ("Cleared the cleanup selection.", "Cleared the cleanup selection."),
        [AppLanguageKeys.InstallerNoOfficialPageFormat] = ("{0} does not have an official page linked yet.", "{0} does not have an official page linked yet."),
        [AppLanguageKeys.InstallerOpenedOfficialPageFormat] = ("Opened {0}'s official page in {1}.", "Opened {0}'s official page in {1}."),
        [AppLanguageKeys.InstallerOpenOfficialPageFailedFormat] = ("Unable to open {0}'s official page: {1}", "Unable to open {0}'s official page: {1}"),
        [AppLanguageKeys.CleanupSelectInstalledFirst] = ("Select at least one installed cleanup app first.", "Select at least one installed cleanup app first."),
        [AppLanguageKeys.CleanupRemovingAppsFormat] = ("Removing {0} app{1}...", "Removing {0} app{1}..."),
        [AppLanguageKeys.CleanupFailedFormat] = ("Cleanup failed: {0}", "Cleanup failed: {0}"),
        [AppLanguageKeys.InstallerQueueFailedFormat] = ("Installer queue failed: {0}", "Installer queue failed: {0}"),
        [AppLanguageKeys.InstallerStatusInstalledUpdatesFormat] = ("{0} installed, {1} update{2} available.", "{0} installed, {1} update{2} available."),
        [AppLanguageKeys.CleanupStatusInstalledRemovableFormat] = ("{0} removable app{1} currently installed.", "{0} removable app{1} currently installed."),
        [AppLanguageKeys.InstallerRefreshFailedFormat] = ("Unable to refresh installer status: {0}", "Unable to refresh installer status: {0}"),
        [AppLanguageKeys.InstallerSelectionSummaryFormat] = ("{0} selected  |  {1} installed  |  {2} updates ready", "{0} selected  |  {1} installed  |  {2} updates ready"),
        [AppLanguageKeys.InstallerUpdateSummaryInitial] = ("Use Check All Updates to scan every tracked app.", "Use Check All Updates to scan every tracked app."),
        [AppLanguageKeys.InstallerUpdateSummaryUnavailable] = ("Update checks are unavailable until winget is available.", "Update checks are unavailable until winget is available."),
        [AppLanguageKeys.InstallerUpdateCustomSuffixFormat] = (" Update All Ready also refreshes {0} custom app{1}.", " Update All Ready also refreshes {0} custom app{1}."),
        [AppLanguageKeys.InstallerUpdateNoneFoundFormat] = ("No winget-tracked updates found.{0}", "No winget-tracked updates found.{0}"),
        [AppLanguageKeys.InstallerUpdateReadyListFormat] = ("Updates ready: {0}.{1}", "Updates ready: {0}.{1}"),
        [AppLanguageKeys.InstallerUpdateReadyMoreFormat] = ("Updates ready: {0}, +{1} more.{2}", "Updates ready: {0}, +{1} more.{2}"),
        [AppLanguageKeys.CleanupSelectionSummaryFormat] = ("{0} selected  |  {1} currently installed", "{0} selected  |  {1} currently installed"),
        [AppLanguageKeys.InstallerQueueSummaryFormat] = ("Queue: {0} queued  |  {1} running  |  {2} finished  |  {3} attention", "Queue: {0} queued  |  {1} running  |  {2} finished  |  {3} attention"),
        [AppLanguageKeys.InstallerQueueCompletionSummaryFormat] = ("{0} applied, {1} already current, {2} need attention.", "{0} applied, {1} already current, {2} need attention."),
        [AppLanguageKeys.InstallerActionInstalling] = ("Installing", "Installing"),
        [AppLanguageKeys.InstallerActionUpdating] = ("Updating", "Updating"),
        [AppLanguageKeys.InstallerActionRemoving] = ("Removing", "Removing"),
        [AppLanguageKeys.InstallerActionInteractiveInstallRunningFor] = ("Running interactive install for", "Running interactive install for"),
        [AppLanguageKeys.InstallerActionInteractiveUpdateRunningFor] = ("Running interactive update for", "Running interactive update for"),
        [AppLanguageKeys.InstallerActionReinstalling] = ("Reinstalling", "Reinstalling"),
        [AppLanguageKeys.InstallerActionWorkingOn] = ("Working on", "Working on"),
        [AppLanguageKeys.InstallerActiveInstalling] = ("Installing...", "Installing..."),
        [AppLanguageKeys.InstallerActiveUpdating] = ("Updating...", "Updating..."),
        [AppLanguageKeys.InstallerActiveRemoving] = ("Removing...", "Removing..."),
        [AppLanguageKeys.InstallerActiveInteractiveInstall] = ("Interactive install running...", "Interactive install running..."),
        [AppLanguageKeys.InstallerActiveInteractiveUpdate] = ("Interactive update running...", "Interactive update running..."),
        [AppLanguageKeys.InstallerActiveReinstalling] = ("Reinstalling...", "Reinstalling..."),
        [AppLanguageKeys.InstallerActiveWorking] = ("Working...", "Working..."),
        [AppLanguageKeys.InstallerStatusUnavailable] = ("Status unavailable", "Status unavailable"),
        [AppLanguageKeys.InstallerUpdateCheckFailedFormat] = ("Unable to check for MultiTool updates: {0}", "Unable to check for MultiTool updates: {0}"),
        [AppLanguageKeys.InstallerSelectionAllSelectedAlreadyInstalled] = (
            "All selected apps are already installed.",
            "All selected apps are already installed."),
        [AppLanguageKeys.InstallerSelectionNoUpdatesReady] = (
            "There are no apps with updates ready.",
            "There are no apps with updates ready."),
        [AppLanguageKeys.InstallerAddedQueuedInstall] = ("Queued install.", "Queued install."),
        [AppLanguageKeys.InstallerAddedQueuedUpdate] = ("Queued update.", "Queued update."),
        [AppLanguageKeys.InstallerAddedQueuedInteractiveInstall] = (
            "Queued interactive install.",
            "Queued interactive install."),
        [AppLanguageKeys.InstallerAddedQueuedInteractiveUpdate] = (
            "Queued interactive update.",
            "Queued interactive update."),
        [AppLanguageKeys.InstallerAddedQueuedReinstall] = ("Queued reinstall.", "Queued reinstall."),
        [AppLanguageKeys.InstallerSelectionNoInstalledReadyToUpdate] = (
            "There are no installed apps ready to update.",
            "There are no installed apps ready to update."),
        [AppLanguageKeys.InstallerSelectionSelectAtLeastOneFirst] = (
            "Select at least one app first.",
            "Select at least one app first."),
        [AppLanguageKeys.InstallerQueueSourceLabel] = ("Installer queue", "Installer queue"),
        [AppLanguageKeys.InstallerPackageStatusGuidedInstall] = ("Guided install", "Guided install"),
        [AppLanguageKeys.InstallerPackageStatusWingetUnavailable] = ("winget unavailable", "winget unavailable"),
        [AppLanguageKeys.InstallerStatusGuidedAppsCanOpenOfficialPagesFormat] = (
            "{0} {1} guided app{2} can still open official setup pages.",
            "{0} {1} guided app{2} can still open official setup pages."),
        [AppLanguageKeys.InstallerPackageStatusQueuedSequenceFormat] = ("#{0} queued", "#{0} queued"),
        [AppLanguageKeys.InstallerStatusSourceAlreadyQueuedOrRunningFormat] = (
            "{0}: that action is already queued or running.",
            "{0}: that action is already queued or running."),
        [AppLanguageKeys.InstallerStatusSourceSelectedAppsAlreadyInstalledFormat] = (
            "{0}: selected app{1} already installed.",
            "{0}: selected app{1} already installed."),
        [AppLanguageKeys.InstallerStatusSourceNothingNewAddedFormat] = (
            "{0}: nothing new was added to the installer queue.",
            "{0}: nothing new was added to the installer queue."),
        [AppLanguageKeys.InstallerSuffixSkippedDuplicateRequestsFormat] = (
            "Skipped {0} duplicate request{1}",
            "Skipped {0} duplicate request{1}"),
        [AppLanguageKeys.InstallerSuffixSkippedAlreadyInstalledAppsFormat] = (
            "Skipped {0} already installed app{1}",
            "Skipped {0} already installed app{1}"),
        [AppLanguageKeys.InstallerOperationNoResult] = (
            "The installer did not return a result.",
            "The installer did not return a result."),
        [AppLanguageKeys.InstallerOperationNoResultGuidance] = (
            "Check the activity log, then try the action again.",
            "Check the activity log, then try the action again."),
        [AppLanguageKeys.InstallerLogEdgeWingetFailedTryingFallback] = (
            "Microsoft Edge uninstall through winget failed. Trying the elevated Edge removal fallback...",
            "Microsoft Edge uninstall through winget failed. Trying the elevated Edge removal fallback..."),
        [AppLanguageKeys.InstallerEdgeDisplayName] = ("Microsoft Edge", "Microsoft Edge"),
        [AppLanguageKeys.InstallerEdgeFallbackMessageFormat] = (
            "{0} (Fallback: Edge-specific elevated removal)",
            "{0} (Fallback: Edge-specific elevated removal)"),
        [AppLanguageKeys.InstallerEdgeFallbackGuidanceRunAsAdminRetry] = (
            "Run MultiTool as administrator and retry Edge removal.",
            "Run MultiTool as administrator and retry Edge removal."),
        [AppLanguageKeys.InstallerLogResultDisplayMessageFormat] = ("{0}: {1}", "{0}: {1}"),
        [AppLanguageKeys.CleanupSummaryCountsFormat] = (
            "{0} removed, {1} already gone, {2} failed.",
            "{0} removed, {1} already gone, {2} failed."),
        [AppLanguageKeys.CleanupSummaryManualStepsFormat] = (
            " {0} require a manual step.",
            " {0} require a manual step."),
        [AppLanguageKeys.CleanupSummaryFirstFailureFormat] = (
            " First failure: {0} - {1}",
            " First failure: {0} - {1}"),
        [AppLanguageKeys.CleanupSummaryNextStepFormat] = (
            " Next step: {0}",
            " Next step: {0}"),
        [AppLanguageKeys.CleanupSummaryFailureLogPathFormat] = (
            " Failure log: {0}",
            " Failure log: {0}"),
        [AppLanguageKeys.CleanupLogFailureLogPathFormat] = (
            "Cleanup failure log: {0}",
            "Cleanup failure log: {0}"),
        [AppLanguageKeys.InstallerFirefoxAddonsLabel] = ("Firefox add-ons", "Firefox add-ons"),
        [AppLanguageKeys.InstallerLogFirefoxAddonsSkippedBecauseInstallFailed] = (
            "Firefox add-ons skipped because Firefox did not install cleanly.",
            "Firefox add-ons skipped because Firefox did not install cleanly."),
        [AppLanguageKeys.InstallerLogUpdateInfoWithUrlFormat] = ("{0} {1}", "{0} {1}"),
        [AppLanguageKeys.InstallerSupplementalSummaryFormat] = (
            " {0}: {1} applied, {2} already ready, {3} failed.",
            " {0}: {1} applied, {2} already ready, {3} failed."),
        [AppLanguageKeys.InstallerLogGuidanceSuffixFormat] = (" Next: {0}", " Next: {0}"),
        [AppLanguageKeys.InstallerLogOperationResultFormat] = ("#{0} {1}: {2}{3}", "#{0} {1}: {2}{3}"),
        [AppLanguageKeys.InstallerLogOpenedOfficialPageInBrowserFormat] = (
            "{0}: opened {1} in {2}.",
            "{0}: opened {1} in {2}."),
        [AppLanguageKeys.InstallerProgressTextFormat] = (
            "{0} {1} [{2}/{3}]...",
            "{0} {1} [{2}/{3}]..."),
        [AppLanguageKeys.InstallerQueuedBatchMessageFormat] = (
            "Queued {0} {1}.",
            "Queued {0} {1}."),
        [AppLanguageKeys.InstallerActionNounInstallSingular] = ("install", "install"),
        [AppLanguageKeys.InstallerActionNounInstallPlural] = ("installs", "installs"),
        [AppLanguageKeys.InstallerActionNounUpdateSingular] = ("update", "update"),
        [AppLanguageKeys.InstallerActionNounUpdatePlural] = ("updates", "updates"),
        [AppLanguageKeys.InstallerActionNounRemovalSingular] = ("removal", "removal"),
        [AppLanguageKeys.InstallerActionNounRemovalPlural] = ("removals", "removals"),
        [AppLanguageKeys.InstallerActionNounTaskSingular] = ("task", "task"),
        [AppLanguageKeys.InstallerActionNounTaskPlural] = ("tasks", "tasks"),
        [AppLanguageKeys.InstallerPluralS] = ("s", "s"),
        [AppLanguageKeys.InstallerPluralIs] = (" is", " is"),
        [AppLanguageKeys.InstallerPluralAre] = ("s are", "s are"),

        [AppLanguageKeys.ToolsUsefulSitesToggleHide] = ("Hide Useful Sites", "Hide Useful Sites"),
        [AppLanguageKeys.ToolsUsefulSitesToggleShow] = ("Useful Sites", "Useful Sites"),
        [AppLanguageKeys.ToolsStatusEmptyDirectoryInitial] = ("Choose a folder tree to scan for empty directories.", "Choose a folder tree to scan for empty directories."),
        [AppLanguageKeys.ToolsStatusShortcutHotkeyInitial] = ("Scan Windows .lnk hotkeys and supported app keymap files on this PC, then include built-in Windows and common shortcuts in one viewer.", "Scan Windows .lnk hotkeys and supported app keymap files on this PC, then include built-in Windows and common shortcuts in one viewer."),
        [AppLanguageKeys.ToolsStatusMouseSensitivityInitial] = ("Pick a slower or faster mouse speed. 10/20 is the normal middle setting in Windows, so it is a good place to start if you are unsure.", "Pick a slower or faster mouse speed. 10/20 is the normal middle setting in Windows, so it is a good place to start if you are unsure."),
        [AppLanguageKeys.ToolsStatusDisplayRefreshInitial] = ("Click Check Displays to see if your monitors can run at a faster refresh rate.", "Click Check Displays to see if your monitors can run at a faster refresh rate."),
        [AppLanguageKeys.ToolsStatusHardwareSystemInitial] = ("No hardware scan yet.", "No hardware scan yet."),
        [AppLanguageKeys.ToolsStatusHardwareHealthInitial] = ("Health summary will appear after scanning.", "Health summary will appear after scanning."),
        [AppLanguageKeys.ToolsStatusHardwareOperatingSystemInitial] = ("Windows details will appear after scanning.", "Windows details will appear after scanning."),
        [AppLanguageKeys.ToolsStatusHardwareProcessorInitial] = ("Processor details will appear after scanning.", "Processor details will appear after scanning."),
        [AppLanguageKeys.ToolsStatusHardwareMemoryInitial] = ("Memory details will appear after scanning.", "Memory details will appear after scanning."),
        [AppLanguageKeys.ToolsStatusHardwareMotherboardInitial] = ("Motherboard details will appear after scanning.", "Motherboard details will appear after scanning."),
        [AppLanguageKeys.ToolsStatusHardwareBiosInitial] = ("BIOS details will appear after scanning.", "BIOS details will appear after scanning."),
        [AppLanguageKeys.ToolsStatusHardwareCheckInitial] = ("Scan the PC to review core hardware details, live sensor telemetry, PCIe devices, storage health, partitions, and RAID details.", "Scan the PC to review core hardware details, live sensor telemetry, PCIe devices, storage health, partitions, and RAID details."),
        [AppLanguageKeys.ToolsStatusDriverUpdateInitial] = ("Scan this PC's hardware and check Windows Update for recommended and optional driver updates. Some driver offers can only be finished through Windows Update's own Optional Updates page.", "Scan this PC's hardware and check Windows Update for recommended and optional driver updates. Some driver offers can only be finished through Windows Update's own Optional Updates page."),
        [AppLanguageKeys.ToolsStatusDarkModeInitial] = ("Apply Windows dark mode preferences for the shell and supported apps.", "Apply Windows dark mode preferences for the shell and supported apps."),
        [AppLanguageKeys.ToolsStatusSearchReplacementInitial] = ("Replace the built-in Windows Search with Flow Launcher for a faster search experience. Use Restore to switch back to Windows Search at any time.", "Replace the built-in Windows Search with Flow Launcher for a faster search experience. Use Restore to switch back to Windows Search at any time."),
        [AppLanguageKeys.ToolsStatusSearchReindexInitial] = ("Force Windows Search to rebuild its index if results are stale or missing.", "Force Windows Search to rebuild its index if results are stale or missing."),
        [AppLanguageKeys.ToolsStatusTelemetryInitial] = ("Reduce Windows telemetry by setting minimum data collection policy and disabling common telemetry services/tasks.", "Reduce Windows telemetry by setting minimum data collection policy and disabling common telemetry services/tasks."),
        [AppLanguageKeys.ToolsStatusPinWindowInitial] = ("Toggle a pinned-on-top mode for the MultiTool window.", "Toggle a pinned-on-top mode for the MultiTool window."),
        [AppLanguageKeys.ToolsStatusOneDriveInitial] = ("Check whether OneDrive is present, apply the system disable policy, and remove it with Windows' built-in uninstaller when available.", "Check whether OneDrive is present, apply the system disable policy, and remove it with Windows' built-in uninstaller when available."),
        [AppLanguageKeys.ToolsStatusEdgeInitial] = ("Detect whether Microsoft Edge is installed and remove it using the developer-override method. An administrator prompt will appear during removal.", "Detect whether Microsoft Edge is installed and remove it using the developer-override method. An administrator prompt will appear during removal."),
        [AppLanguageKeys.ToolsStatusFnCtrlSwapInitial] = ("Detect Lenovo BIOS Fn/Ctrl key swap support and switch the key positions when available.", "Detect Lenovo BIOS Fn/Ctrl key swap support and switch the key positions when available."),
        [AppLanguageKeys.ToolsStatusUsefulSitesInitial] = ("Open a curated list of useful sites from inside MultiTool.", "Open a curated list of useful sites from inside MultiTool."),
        [AppLanguageKeys.ToolsStatusWindows11EeaInitial] = ("Build the official Windows 11 media prep files with Ireland as the EEA regional default, then let MultiTool watch for the finished USB and copy the answer file automatically.", "Build the official Windows 11 media prep files with Ireland as the EEA regional default, then let MultiTool watch for the finished USB and copy the answer file automatically."),
        [AppLanguageKeys.ToolsErrorReadMouseSensitivityFormat] = ("Unable to read the Windows mouse sensitivity: {0}", "Unable to read the Windows mouse sensitivity: {0}"),
        [AppLanguageKeys.ToolsErrorCheckOneDriveFormat] = ("Unable to check OneDrive status: {0}", "Unable to check OneDrive status: {0}"),
        [AppLanguageKeys.ToolsErrorCheckEdgeFormat] = ("Unable to check Edge status: {0}", "Unable to check Edge status: {0}"),
        [AppLanguageKeys.ToolsErrorCheckSearchReplacementFormat] = ("Unable to check the Flow Launcher search replacement: {0}", "Unable to check the Flow Launcher search replacement: {0}"),
        [AppLanguageKeys.ToolsErrorCheckFnCtrlSwapFormat] = ("Unable to check Fn/Ctrl swap status: {0}", "Unable to check Fn/Ctrl swap status: {0}"),
        [AppLanguageKeys.ToolsFolderPickerSelectEmptyDirectoryRoot] = ("Select the folder tree to scan for empty directories", "Select the folder tree to scan for empty directories"),
        [AppLanguageKeys.ToolsStatusFolderSelectionCanceled] = ("Folder selection canceled.", "Folder selection canceled."),
        [AppLanguageKeys.ToolsStatusEmptyDirectoryRootSetFormat] = ("Empty directory scan root set to {0}.", "Empty directory scan root set to {0}."),
        [AppLanguageKeys.ToolsStatusShortcutScanRunning] = ("Scanning fixed drives for assigned Windows shortcut keys, loading supported app keymaps, and adding built-in shortcut references...", "Scanning fixed drives for assigned Windows shortcut keys, loading supported app keymaps, and adding built-in shortcut references..."),
        [AppLanguageKeys.ToolsStatusShortcutScanWarningsSuffixFormat] = (" Skipped {0} folder or shortcut read{1}.", " Skipped {0} folder or shortcut read{1}."),
        [AppLanguageKeys.ToolsStatusShortcutScanNoShortcutsFormat] = ("Scanned {0} .lnk shortcut file{1}. No shortcut keys were found.{2}", "Scanned {0} .lnk shortcut file{1}. No shortcut keys were found.{2}"),
        [AppLanguageKeys.ToolsStatusShortcutScanOpenedViewerFormat] = ("Opened the shortcut viewer with {0} detected .lnk hotkey{1} and {2} built-in/common shortcut reference entr{3}.{4}", "Opened the shortcut viewer with {0} detected .lnk hotkey{1} and {2} built-in/common shortcut reference entr{3}.{4}"),
        [AppLanguageKeys.ToolsStatusShortcutScanFailedFormat] = ("Shortcut hotkey scan failed: {0}", "Shortcut hotkey scan failed: {0}"),
        [AppLanguageKeys.ToolsStatusUsefulSitesShowingFormat] = ("Showing {0} useful site{1}.", "Showing {0} useful site{1}."),
        [AppLanguageKeys.ToolsStatusUsefulSitesHidden] = ("Useful site list hidden.", "Useful site list hidden."),
        [AppLanguageKeys.ToolsStatusUsefulSiteOpenedFormat] = ("Opened {0} in {1}.", "Opened {0} in {1}."),
        [AppLanguageKeys.ToolsStatusUsefulSiteOpenFailedFormat] = ("Unable to open {0}: {1}", "Unable to open {0}: {1}"),
        [AppLanguageKeys.ToolsStatusWindows11EeaPreparing] = ("Downloading Microsoft's Windows 11 Media Creation Tool and preparing the EEA setup files...", "Downloading Microsoft's Windows 11 Media Creation Tool and preparing the EEA setup files..."),
        [AppLanguageKeys.ToolsStatusWindows11EeaFailedFormat] = ("Windows 11 EEA media prep failed: {0}", "Windows 11 EEA media prep failed: {0}"),
        [AppLanguageKeys.ToolsStatusSearchReplacementApplying] = ("Setting up Flow Launcher + Everything as the Win + S search replacement...", "Setting up Flow Launcher + Everything as the Win + S search replacement..."),
        [AppLanguageKeys.ToolsStatusSearchReplacementApplyFailedFormat] = ("Search replacement setup failed: {0}", "Search replacement setup failed: {0}"),
        [AppLanguageKeys.ToolsStatusSearchReplacementRestoring] = ("Restoring Windows Search and removing the Flow Launcher Win + S replacement...", "Restoring Windows Search and removing the Flow Launcher Win + S replacement..."),
        [AppLanguageKeys.ToolsStatusSearchReplacementRestoreFailedFormat] = ("Search restoration failed: {0}", "Search restoration failed: {0}"),
        [AppLanguageKeys.ToolsStatusTelemetryApplying] = ("Applying telemetry reduction policies and service/task hardening...", "Applying telemetry reduction policies and service/task hardening..."),
        [AppLanguageKeys.ToolsStatusTelemetryApplyFailedFormat] = ("Telemetry reduction failed: {0}", "Telemetry reduction failed: {0}"),
        [AppLanguageKeys.ToolsStatusTelemetryRestoring] = ("Restoring telemetry defaults...", "Restoring telemetry defaults..."),
        [AppLanguageKeys.ToolsStatusTelemetryRestoreFailedFormat] = ("Telemetry defaults restore failed: {0}", "Telemetry defaults restore failed: {0}"),
        [AppLanguageKeys.ToolsStatusSearchReindexRequesting] = ("Requesting a full Windows Search re-index...", "Requesting a full Windows Search re-index..."),
        [AppLanguageKeys.ToolsStatusSearchReindexFailedFormat] = ("Windows Search re-index failed: {0}", "Windows Search re-index failed: {0}"),
        [AppLanguageKeys.ToolsErrorCheckSearchReindexFormat] = ("Unable to check Windows Search re-index status: {0}", "Unable to check Windows Search re-index status: {0}"),
        [AppLanguageKeys.ToolsErrorCheckTelemetryFormat] = ("Unable to check telemetry reduction status: {0}", "Unable to check telemetry reduction status: {0}"),
        [AppLanguageKeys.ToolsStatusPinWindowPinnedFormat] = ("Window is pinned on top. Hotkey: {0}.", "Window is pinned on top. Hotkey: {0}."),
        [AppLanguageKeys.ToolsStatusPinWindowUnpinnedFormat] = ("Window is not pinned. Hotkey: {0}.", "Window is not pinned. Hotkey: {0}."),
        [AppLanguageKeys.ToolsPinWindowStatePinned] = ("pinned on top", "pinned on top"),
        [AppLanguageKeys.ToolsPinWindowStateUnpinned] = ("unpinned", "unpinned"),
        [AppLanguageKeys.ToolsStatusPinWindowToggledFormat] = ("Window {0} via {1}. Hotkey: {2}.", "Window {0} via {1}. Hotkey: {2}."),
        [AppLanguageKeys.ToolsPinWindowTriggerHotkey] = ("hotkey", "hotkey"),
        [AppLanguageKeys.ToolsPinWindowTriggerToolButton] = ("tool button", "tool button"),
        [AppLanguageKeys.ToolsStatusSettingsDarkModeOn] = ("Dark mode is on.", "Dark mode is on."),
        [AppLanguageKeys.ToolsStatusOpenedColorSettings] = ("Opened Windows color settings for anything that still needs a manual dark mode change.", "Opened Windows color settings for anything that still needs a manual dark mode change."),
        [AppLanguageKeys.ToolsStatusOpenColorSettingsFailedFormat] = ("Unable to open Windows color settings: {0}", "Unable to open Windows color settings: {0}"),
        [AppLanguageKeys.ToolsStatusOpenedScanRoot] = ("Opened the scan root folder.", "Opened the scan root folder."),
        [AppLanguageKeys.ToolsStatusOpenScanRootFailedFormat] = ("Unable to open the scan root folder: {0}", "Unable to open the scan root folder: {0}"),
        [AppLanguageKeys.ToolsStatusMouseSensitivityApplyingFormat] = ("Applying {0}...", "Applying {0}..."),
        [AppLanguageKeys.ToolsStatusMouseSensitivityUpdateFailedFormat] = ("Mouse sensitivity update failed: {0}", "Mouse sensitivity update failed: {0}"),
        [AppLanguageKeys.ToolsStatusOpenedMouseSettings] = ("Opened Windows mouse settings.", "Opened Windows mouse settings."),
        [AppLanguageKeys.ToolsStatusOpenMouseSettingsFailedFormat] = ("Unable to open Windows mouse settings: {0}", "Unable to open Windows mouse settings: {0}"),
        [AppLanguageKeys.ToolsStatusDisplayRefreshApplying] = ("Applying top refresh rates to connected displays...", "Applying top refresh rates to connected displays..."),
        [AppLanguageKeys.ToolsStatusDisplayRefreshAlreadyBest] = ("All displays were already at their top refresh rate for the current resolution.", "All displays were already at their top refresh rate for the current resolution."),
        [AppLanguageKeys.ToolsStatusDisplayRefreshAppliedSummaryFormat] = ("{0} display{1} updated, {2} failed.", "{0} display{1} updated, {2} failed."),
        [AppLanguageKeys.ToolsStatusDisplayRefreshUpdateFailedFormat] = ("Display refresh update failed: {0}", "Display refresh update failed: {0}"),
        [AppLanguageKeys.ToolsStatusHardwareCopiedClipboard] = ("Copied hardware check details to the clipboard.", "Copied hardware check details to the clipboard."),
        [AppLanguageKeys.ToolsStatusHardwareCopyFailedFormat] = ("Unable to copy the hardware check details: {0}", "Unable to copy the hardware check details: {0}"),
        [AppLanguageKeys.ToolsStatusDriverSelectRecommended] = ("Selected recommended driver updates and left optional ones unchecked.", "Selected recommended driver updates and left optional ones unchecked."),
        [AppLanguageKeys.ToolsStatusDriverSelectAll] = ("Selected all discovered driver updates.", "Selected all discovered driver updates."),
        [AppLanguageKeys.ToolsStatusDriverSelectionCleared] = ("Cleared the driver update selection.", "Cleared the driver update selection."),
        [AppLanguageKeys.ToolsStatusDriverSelectAtLeastOne] = ("Select at least one driver update first.", "Select at least one driver update first."),
        [AppLanguageKeys.ToolsStatusDriverInstallingFormat] = ("Installing {0} driver update{1} through Windows Update...", "Installing {0} driver update{1} through Windows Update..."),
        [AppLanguageKeys.ToolsStatusDriverResultInstalledFormat] = ("{0} installed", "{0} installed"),
        [AppLanguageKeys.ToolsStatusDriverResultManualFlowFormat] = ("{0} need Windows Update's own interactive flow", "{0} need Windows Update's own interactive flow"),
        [AppLanguageKeys.ToolsStatusDriverResultFailedFormat] = ("{0} failed", "{0} failed"),
        [AppLanguageKeys.ToolsStatusDriverResultNoChanges] = ("No driver changes were applied", "No driver changes were applied"),
        [AppLanguageKeys.ToolsStatusDriverSummaryFormat] = ("{0}. {1} update{2} remain.", "{0}. {1} update{2} remain."),
        [AppLanguageKeys.ToolsStatusDriverManualFlowHint] = (" Use Open Optional Updates for the ones Windows will not install silently.", " Use Open Optional Updates for the ones Windows will not install silently."),
        [AppLanguageKeys.ToolsStatusDriverRestartHintFormat] = (" Restart required for {0} item{1}.", " Restart required for {0} item{1}."),
        [AppLanguageKeys.ToolsStatusDriverInstallFailedFormat] = ("Driver install failed: {0}", "Driver install failed: {0}"),
        [AppLanguageKeys.ToolsStatusDriverOpenOptionalUpdatesFailedFormat] = ("Unable to open Windows Update Optional Updates: {0}", "Unable to open Windows Update Optional Updates: {0}"),
        [AppLanguageKeys.ToolsStatusDriverOpenOptionalUpdatesAndScan] = ("Opened Windows Update Optional Updates and asked Windows Update to start a scan so interactive driver offers can appear immediately.", "Opened Windows Update Optional Updates and asked Windows Update to start a scan so interactive driver offers can appear immediately."),
        [AppLanguageKeys.ToolsStatusDriverOpenOptionalUpdatesNoScan] = ("Opened Windows Update Optional Updates. If pending driver offers are not visible yet, click Check for updates in Windows Update.", "Opened Windows Update Optional Updates. If pending driver offers are not visible yet, click Check for updates in Windows Update."),
        [AppLanguageKeys.ToolsStatusDriverOpenUpdatesAndScan] = ("Opened Windows Update and asked Windows Update to start a scan. Open Advanced options > Optional updates to finish interactive driver installs.", "Opened Windows Update and asked Windows Update to start a scan. Open Advanced options > Optional updates to finish interactive driver installs."),
        [AppLanguageKeys.ToolsStatusDriverOpenUpdatesNoScan] = ("Opened Windows Update. Open Advanced options > Optional updates and click Check for updates to finish interactive driver installs.", "Opened Windows Update. Open Advanced options > Optional updates and click Check for updates to finish interactive driver installs."),
        [AppLanguageKeys.ToolsStatusOneDriveRemoving] = ("Removing OneDrive...", "Removing OneDrive..."),
        [AppLanguageKeys.ToolsStatusOneDriveRemoveFailedFormat] = ("OneDrive removal failed: {0}", "OneDrive removal failed: {0}"),
        [AppLanguageKeys.ToolsStatusEdgeRemoving] = ("Removing Microsoft Edge...", "Removing Microsoft Edge..."),
        [AppLanguageKeys.ToolsStatusEdgeRemoveFailedFormat] = ("Edge removal failed: {0}", "Edge removal failed: {0}"),
        [AppLanguageKeys.ToolsStatusFnCtrlSwapApplying] = ("Applying Fn/Ctrl key swap setting...", "Applying Fn/Ctrl key swap setting..."),
        [AppLanguageKeys.ToolsStatusFnCtrlSwapFailedFormat] = ("Fn/Ctrl swap failed: {0}", "Fn/Ctrl swap failed: {0}"),
        [AppLanguageKeys.ToolsStatusEmptyDirectorySelectAll] = ("Selected all empty-directory results.", "Selected all empty-directory results."),
        [AppLanguageKeys.ToolsStatusEmptyDirectorySelectionCleared] = ("Cleared the empty-directory selection.", "Cleared the empty-directory selection."),
        [AppLanguageKeys.ToolsStatusEmptyDirectorySelectAtLeastOne] = ("Select at least one empty directory first.", "Select at least one empty directory first."),
        [AppLanguageKeys.ToolsStatusEmptyDirectoryDeletingFormat] = ("Deleting {0} empty director{1}...", "Deleting {0} empty director{1}..."),
        [AppLanguageKeys.ToolsStatusEmptyDirectoryDeleteSummaryFormat] = ("{0} deleted, {1} already gone, {2} failed. {3} deletable director{4} remain.", "{0} deleted, {1} already gone, {2} failed. {3} deletable director{4} remain."),
        [AppLanguageKeys.ToolsStatusEmptyDirectoryDeleteFailedFormat] = ("Delete failed: {0}", "Delete failed: {0}"),
        [AppLanguageKeys.ToolsStatusEmptyDirectoryChooseRootFirst] = ("Choose a folder tree first.", "Choose a folder tree first."),
        [AppLanguageKeys.ToolsStatusEmptyDirectoryRootMissingFormat] = ("The folder '{0}' does not exist.", "The folder '{0}' does not exist."),
        [AppLanguageKeys.ToolsStatusEmptyDirectoryScanningFormat] = ("Scanning {0} for empty directories...", "Scanning {0} for empty directories..."),
        [AppLanguageKeys.ToolsStatusEmptyDirectoryWarningsSuffixFormat] = (" Skipped {0} folder{1} due to access or IO errors.", " Skipped {0} folder{1} due to access or IO errors."),
        [AppLanguageKeys.ToolsStatusEmptyDirectoryNoneFoundFormat] = ("No deletable empty directories found.{0}", "No deletable empty directories found.{0}"),
        [AppLanguageKeys.ToolsStatusEmptyDirectoryFoundFormat] = ("Found {0} deletable empty director{1}.{2}", "Found {0} deletable empty director{1}.{2}"),
        [AppLanguageKeys.ToolsStatusEmptyDirectoryScanFailedFormat] = ("Scan failed: {0}", "Scan failed: {0}"),
        [AppLanguageKeys.ToolsStatusDriverScanStarting] = ("Detecting hardware and checking Windows Update for driver updates...", "Detecting hardware and checking Windows Update for driver updates..."),
        [AppLanguageKeys.ToolsStatusDriverScanWarningsSuffixFormat] = (" Warnings: {0}.", " Warnings: {0}."),
        [AppLanguageKeys.ToolsStatusDriverScanInteractiveSuffixFormat] = (" {0} need Windows Update's own interactive flow instead of MultiTool's silent install path.", " {0} need Windows Update's own interactive flow instead of MultiTool's silent install path."),
        [AppLanguageKeys.ToolsStatusDriverScanNoneFormat] = ("Detected {0} hardware component{1}. No driver updates are currently available from Windows Update.{2}", "Detected {0} hardware component{1}. No driver updates are currently available from Windows Update.{2}"),
        [AppLanguageKeys.ToolsStatusDriverScanFoundFormat] = ("Detected {0} hardware component{1}. Found {2} recommended and {3} optional driver update{4}.{5}{6}", "Detected {0} hardware component{1}. Found {2} recommended and {3} optional driver update{4}.{5}{6}"),
        [AppLanguageKeys.ToolsStatusDriverScanFailedFormat] = ("Driver scan failed: {0}", "Driver scan failed: {0}"),
        [AppLanguageKeys.ToolsStatusHardwareScanStarting] = ("Scanning this PC's hardware details...", "Scanning this PC's hardware details..."),
        [AppLanguageKeys.ToolsStatusHardwareScanWarningsSuffixFormat] = (" Warnings: {0}.", " Warnings: {0}."),
        [AppLanguageKeys.ToolsStatusHardwareScanCompleteFormat] = ("Hardware scan complete. {0} Found {1} graphics adapter{2}, {3} storage drive{4}, {5} partition{6}, {7} PCI/PCIe device{8}, {9} sensor reading{10}, and {11} RAID/storage detail{12}.{13}", "Hardware scan complete. {0} Found {1} graphics adapter{2}, {3} storage drive{4}, {5} partition{6}, {7} PCI/PCIe device{8}, {9} sensor reading{10}, and {11} RAID/storage detail{12}.{13}"),
        [AppLanguageKeys.ToolsStatusHardwareScanFailedFormat] = ("Hardware scan failed: {0}", "Hardware scan failed: {0}"),
        [AppLanguageKeys.ToolsStatusDisplayRefreshScanStarting] = ("Checking display refresh rate recommendations...", "Checking display refresh rate recommendations..."),
        [AppLanguageKeys.ToolsStatusDisplayRefreshNoDisplays] = ("No desktop-attached displays were detected.", "No desktop-attached displays were detected."),
        [AppLanguageKeys.ToolsStatusDisplayRefreshCheckedAllBestFormat] = ("Checked {0} display{1}. All are already at their top refresh rate for the current resolution.", "Checked {0} display{1}. All are already at their top refresh rate for the current resolution."),
        [AppLanguageKeys.ToolsStatusDisplayRefreshCheckedCanRunFasterFormat] = ("Checked {0} display{1}. {2} can be switched to a higher refresh rate.", "Checked {0} display{1}. {2} can be switched to a higher refresh rate."),
        [AppLanguageKeys.ToolsStatusDisplayRefreshScanFailedFormat] = ("Display refresh scan failed: {0}", "Display refresh scan failed: {0}"),
        [AppLanguageKeys.ToolsShortcutHotkeyScanPreparing] = ("Preparing shortcut scan...", "Preparing shortcut scan..."),
        [AppLanguageKeys.ToolsEmptyDirectorySelectionSummaryFormat] = ("{0} selected  |  {1} deletable folder{2} found", "{0} selected  |  {1} deletable folder{2} found"),
        [AppLanguageKeys.ToolsEmptyDirectoryScanProgressSummaryFormat] = ("Scanning {0}/{1} folders...  |  Current: {2}", "Scanning {0}/{1} folders...  |  Current: {2}"),
        [AppLanguageKeys.ToolsShortcutHotkeyScanProgressSummaryFormat] = ("Scanning {0}/{1} folders...  |  .lnk files checked: {2}", "Scanning {0}/{1} folders...  |  .lnk files checked: {2}"),
        [AppLanguageKeys.ToolsMouseSensitivitySummaryCurrentFormat] = ("Current pointer feel: {0}", "Current pointer feel: {0}"),
        [AppLanguageKeys.ToolsMouseSensitivitySummaryPickedFormat] = ("Current pointer feel: {0}  |  Picked: {1}", "Current pointer feel: {0}  |  Picked: {1}"),
        [AppLanguageKeys.ToolsMouseSensitivityGuidanceVerySlow] = ("Very slow. Best if the pointer feels jumpy and you want maximum control.", "Very slow. Best if the pointer feels jumpy and you want maximum control."),
        [AppLanguageKeys.ToolsMouseSensitivityGuidanceSlow] = ("Slow. Good if you want steadier cursor movement.", "Slow. Good if you want steadier cursor movement."),
        [AppLanguageKeys.ToolsMouseSensitivityGuidanceBalanced] = ("Balanced. This is the easiest starting range for most people.", "Balanced. This is the easiest starting range for most people."),
        [AppLanguageKeys.ToolsMouseSensitivityGuidanceFast] = ("Fast. Good if you want to move across the screen with less hand movement.", "Fast. Good if you want to move across the screen with less hand movement."),
        [AppLanguageKeys.ToolsMouseSensitivityGuidanceVeryFast] = ("Very fast. Best only if you like an extremely quick cursor.", "Very fast. Best only if you like an extremely quick cursor."),
        [AppLanguageKeys.ToolsMouseSensitivitySelectionGuidanceFormat] = ("Selected feel: {0}. Tip: move 1-2 steps at a time. {1}", "Selected feel: {0}. Tip: move 1-2 steps at a time. {1}"),
        [AppLanguageKeys.ToolsMouseSensitivityFeelVerySlow] = ("Very Slow", "Very Slow"),
        [AppLanguageKeys.ToolsMouseSensitivityFeelSlow] = ("Slow", "Slow"),
        [AppLanguageKeys.ToolsMouseSensitivityFeelBalanced] = ("Balanced", "Balanced"),
        [AppLanguageKeys.ToolsMouseSensitivityFeelFast] = ("Fast", "Fast"),
        [AppLanguageKeys.ToolsMouseSensitivityFeelVeryFast] = ("Very Fast", "Very Fast"),
        [AppLanguageKeys.ToolsMouseSensitivityLevelTextMiddleFormat] = ("{0} ({1}/20, Windows middle)", "{0} ({1}/20, Windows middle)"),
        [AppLanguageKeys.ToolsMouseSensitivityLevelTextFormat] = ("{0} ({1}/20)", "{0} ({1}/20)"),
        [AppLanguageKeys.ToolsDisplayRefreshSummaryNone] = ("No displays checked yet.", "No displays checked yet."),
        [AppLanguageKeys.ToolsDisplayRefreshSummaryAllBestFormat] = ("{0} display{1} found - all running at their best rate", "{0} display{1} found - all running at their best rate"),
        [AppLanguageKeys.ToolsDisplayRefreshSummaryCanRunFasterFormat] = ("{0} display{1} found - {2} can run faster", "{0} display{1} found - {2} can run faster"),
        [AppLanguageKeys.ToolsHardwareGraphicsSummaryFormat] = ("{0} graphics adapter{1}", "{0} graphics adapter{1}"),
        [AppLanguageKeys.ToolsHardwareStorageSummaryFormat] = ("{0} storage drive{1}", "{0} storage drive{1}"),
        [AppLanguageKeys.ToolsHardwarePartitionSummaryNone] = ("No partitions detected", "No partitions detected"),
        [AppLanguageKeys.ToolsHardwarePartitionSummaryFormat] = ("{0} partition{1}", "{0} partition{1}"),
        [AppLanguageKeys.ToolsHardwareSensorSummaryNone] = ("Sensors / Temps / Fans: Windows did not expose live telemetry", "Sensors / Temps / Fans: Windows did not expose live telemetry"),
        [AppLanguageKeys.ToolsHardwareSensorSummaryFormat] = ("{0} sensor reading{1}", "{0} sensor reading{1}"),
        [AppLanguageKeys.ToolsHardwarePciSummaryNone] = ("No PCI/PCIe devices detected", "No PCI/PCIe devices detected"),
        [AppLanguageKeys.ToolsHardwarePciSummaryFormat] = ("{0} PCI/PCIe device{1}", "{0} PCI/PCIe device{1}"),
        [AppLanguageKeys.ToolsHardwareRaidSummaryNone] = ("RAID / Storage Spaces: none detected", "RAID / Storage Spaces: none detected"),
        [AppLanguageKeys.ToolsHardwareRaidSummaryFormat] = ("{0} RAID/storage detail{1}", "{0} RAID/storage detail{1}"),
        [AppLanguageKeys.ToolsDriverHardwareSummaryFormat] = ("{0} detected component{1}", "{0} detected component{1}"),
        [AppLanguageKeys.ToolsDriverUpdateSelectionSummaryFormat] = ("{0} selected  |  {1} recommended, {2} optional", "{0} selected  |  {1} recommended, {2} optional"),
        [AppLanguageKeys.ToolsHardwareClipboardTitle] = ("MultiTool Hardware Check", "MultiTool Hardware Check"),
        [AppLanguageKeys.ToolsHardwareClipboardCapturedFormat] = ("Captured: {0}", "Captured: {0}"),
        [AppLanguageKeys.ToolsHardwareClipboardSystemSection] = ("System", "System"),
        [AppLanguageKeys.ToolsHardwareClipboardOverviewFormat] = ("- Overview: {0}", "- Overview: {0}"),
        [AppLanguageKeys.ToolsHardwareClipboardHealthSummaryFormat] = ("- Health Summary: {0}", "- Health Summary: {0}"),
        [AppLanguageKeys.ToolsHardwareClipboardOperatingSystemFormat] = ("- Operating System: {0}", "- Operating System: {0}"),
        [AppLanguageKeys.ToolsHardwareClipboardProcessorFormat] = ("- Processor: {0}", "- Processor: {0}"),
        [AppLanguageKeys.ToolsHardwareClipboardMemoryFormat] = ("- Memory: {0}", "- Memory: {0}"),
        [AppLanguageKeys.ToolsHardwareClipboardMotherboardFormat] = ("- Motherboard: {0}", "- Motherboard: {0}"),
        [AppLanguageKeys.ToolsHardwareClipboardBiosFormat] = ("- BIOS: {0}", "- BIOS: {0}"),
        [AppLanguageKeys.ToolsHardwareClipboardGraphicsSection] = ("Graphics", "Graphics"),
        [AppLanguageKeys.ToolsHardwareClipboardSummaryFormat] = ("- Summary: {0}", "- Summary: {0}"),
        [AppLanguageKeys.ToolsHardwareClipboardDriverFormat] = ("  Driver: {0}", "  Driver: {0}"),
        [AppLanguageKeys.ToolsHardwareClipboardMemoryFieldFormat] = ("  Memory: {0}", "  Memory: {0}"),
        [AppLanguageKeys.ToolsHardwareClipboardStorageSection] = ("Storage Drives", "Storage Drives"),
        [AppLanguageKeys.ToolsHardwareClipboardSizeFormat] = ("  Size: {0}", "  Size: {0}"),
        [AppLanguageKeys.ToolsHardwareClipboardInterfaceFormat] = ("  Interface: {0}", "  Interface: {0}"),
        [AppLanguageKeys.ToolsHardwareClipboardMediaFormat] = ("  Media: {0}", "  Media: {0}"),
        [AppLanguageKeys.ToolsHardwareClipboardHealthFormat] = ("  Health: {0}", "  Health: {0}"),
        [AppLanguageKeys.ToolsHardwareClipboardSmartFormat] = ("  SMART: {0}", "  SMART: {0}"),
        [AppLanguageKeys.ToolsHardwareClipboardFirmwareFormat] = ("  Firmware: {0}", "  Firmware: {0}"),
        [AppLanguageKeys.ToolsHardwareClipboardSerialFormat] = ("  Serial: {0}", "  Serial: {0}"),
        [AppLanguageKeys.ToolsHardwareClipboardNotesFormat] = ("  Notes: {0}", "  Notes: {0}"),
        [AppLanguageKeys.ToolsHardwareClipboardPartitionsSection] = ("Partitions", "Partitions"),
        [AppLanguageKeys.ToolsHardwareClipboardDiskFormat] = ("  Disk: {0}", "  Disk: {0}"),
        [AppLanguageKeys.ToolsHardwareClipboardTypeFormat] = ("  Type: {0}", "  Type: {0}"),
        [AppLanguageKeys.ToolsHardwareClipboardVolumeFormat] = ("  Volume: {0}", "  Volume: {0}"),
        [AppLanguageKeys.ToolsHardwareClipboardFileSystemFormat] = ("  File System: {0}", "  File System: {0}"),
        [AppLanguageKeys.ToolsHardwareClipboardFreeSpaceFormat] = ("  Free Space: {0}", "  Free Space: {0}"),
        [AppLanguageKeys.ToolsHardwareClipboardStatusFormat] = ("  Status: {0}", "  Status: {0}"),
        [AppLanguageKeys.ToolsHardwareClipboardSensorsSection] = ("Sensors", "Sensors"),
        [AppLanguageKeys.ToolsHardwareClipboardCategoryFormat] = ("  Category: {0}", "  Category: {0}"),
        [AppLanguageKeys.ToolsHardwareClipboardReadingFormat] = ("  Reading: {0}", "  Reading: {0}"),
        [AppLanguageKeys.ToolsHardwareClipboardSourceFormat] = ("  Source: {0}", "  Source: {0}"),
        [AppLanguageKeys.ToolsHardwareClipboardPciSection] = ("PCI / PCIe Devices", "PCI / PCIe Devices"),
        [AppLanguageKeys.ToolsHardwareClipboardClassFormat] = ("  Class: {0}", "  Class: {0}"),
        [AppLanguageKeys.ToolsHardwareClipboardManufacturerFormat] = ("  Manufacturer: {0}", "  Manufacturer: {0}"),
        [AppLanguageKeys.ToolsHardwareClipboardLocationFormat] = ("  Location: {0}", "  Location: {0}"),
        [AppLanguageKeys.ToolsHardwareClipboardRaidSection] = ("RAID / Storage Spaces", "RAID / Storage Spaces"),
        [AppLanguageKeys.ToolsHardwareClipboardDetailsFormat] = ("  Details: {0}", "  Details: {0}"),
        [AppLanguageKeys.ToolsUsefulSiteFmhyName] = ("FMHY.net", "FMHY.net"),
        [AppLanguageKeys.ToolsUsefulSiteFmhyDescription] = ("The largest collection of free stuff on the internet.", "The largest collection of free stuff on the internet."),
        [AppLanguageKeys.ToolsUsefulSiteSevenSeasName] = ("Guide to sailing the Seven Seas", "Guide to sailing the Seven Seas"),
        [AppLanguageKeys.ToolsUsefulSiteSevenSeasDescription] = ("A comprehensive guide to learn how to get free things!", "A comprehensive guide to learn how to get free things!"),
        [AppLanguageKeys.ToolsUsefulSiteZLibraryName] = ("Z-Library (the true edition)", "Z-Library (the true edition)"),
        [AppLanguageKeys.ToolsUsefulSiteZLibraryDescription] = ("The largest free ebook library on the internet. Tor browser required (and recommended).", "The largest free ebook library on the internet. Tor browser required (and recommended)."),
        [AppLanguageKeys.ToolsUsefulSiteFmhyBackupName] = (":)", ":)"),
        [AppLanguageKeys.ToolsUsefulSiteFmhyBackupDescription] = ("I like free stuff... GenP + adobe creative cloud, anyone?", "I like free stuff... GenP + adobe creative cloud, anyone?"),

        [AppLanguageKeys.ScreenshotOptionsTitle] = ("Screenshot Options", "Screenshot Options"),
        [AppLanguageKeys.ScreenshotFullScreenHeader] = ("Full Screen", "Full Screen"),
        [AppLanguageKeys.ScreenshotFullScreenDescription] = ("Capture the entire screen right away.", "Capture the entire screen right meow."),
        [AppLanguageKeys.ScreenshotAreaHeader] = ("Area", "Area >~<"),
        [AppLanguageKeys.ScreenshotAreaDescription] = ("Select just the part of the screen you want.", "Select just the part of the screen you want._."),
        [AppLanguageKeys.ScreenshotRecordVideo] = ("Record Video", "Record Video!"),
        [AppLanguageKeys.ScreenshotCancel] = ("Cancel", "Cancel -_-"),
        [AppLanguageKeys.ScreenshotWaiting] = ("Waiting...", "Waiting... :3"),

        [AppLanguageKeys.ScreenshotStatusOptionsOpen] = (
            "Screenshot options are open. Choose Full Screen, Area, or arm Record Video first.",
            "Screenshot options are open. Pick Full Screen, Area, or arm Record Video first."),
        [AppLanguageKeys.ScreenshotStatusChooseModeFirst] = (
            "Choose Full Screen or Area first, then press the screenshot hotkey to start recording.",
            "Pick Full Screen or Area first, then press the Screenshot pawkey to start recording."),
        [AppLanguageKeys.ScreenshotStatusRecordingStarted] = (
            "Recording started. Press the screenshot hotkey again to stop and save the video.",
            "Recording started. Press the Screenshot pawkey again to stop and save the Video."),
        [AppLanguageKeys.ScreenshotStatusUnableToStart] = (
            "Unable to start video recording: {0}",
            "Could not start video Recording: {0}"),
        [AppLanguageKeys.ScreenshotStatusVideoTargetFullScreen] = (
            "Video target set to Full Screen. Press the screenshot hotkey to start recording.",
            "Video target set to Full Screen. Press the Screenshot pawkey to start Recording."),
        [AppLanguageKeys.ScreenshotStatusAreaSelectionCanceled] = (
            "Area selection canceled. Choose Area again or switch back to Full Screen.",
            "Area selection canceled. Pick Area again or switch back to Full Screen."),
        [AppLanguageKeys.ScreenshotStatusAreaSelected] = (
            "Area selected ({0} x {1}). Press the screenshot hotkey to start recording.",
            "Area selected ({0} x {1}). Press the Screenshot pawkey to start recording."),
        [AppLanguageKeys.ScreenshotStatusVideoModeArmed] = (
            "Video mode armed. Choose Full Screen or Area, then press the screenshot hotkey to start recording.",
            "Video mode armed. Choose Full Screen or Area, then press the Screenshot pawkey to start recording."),
        [AppLanguageKeys.ScreenshotStatusStopRecordingFirst] = (
            "Press the screenshot hotkey again to stop recording first.",
            "Press the Screenshot pawkey again to stop Recording first."),
        [AppLanguageKeys.ScreenshotStatusVideoModeOff] = ("Video mode turned off.", "Video mode turned off."),
        [AppLanguageKeys.ScreenshotStatusStillRecordingSaveFirst] = (
            "Recording is still running. Press the screenshot hotkey again to stop and save it first.",
            "Recording is still running. Press the Screenshot pawkey again to stop and save it first."),
        [AppLanguageKeys.ScreenshotStatusRecordingStopped] = ("Video recording stopped.", "Video Recording stopped."),
        [AppLanguageKeys.ScreenshotStatusSavedVideoTo] = ("Saved video to {0}.", "Saved Video to {0}."),
        [AppLanguageKeys.ScreenshotStatusUnableToStop] = (
            "Unable to stop video recording: {0}",
            "Could not stop Video Recording: {0}"),
        [AppLanguageKeys.ScreenshotStatusVideoOff] = ("Video recording is off.", "Video Recording is off."),
    };

    public static string Get(string key, AppLanguage language)
    {
        if (!Values.TryGetValue(key, out var value))
        {
            throw new KeyNotFoundException($"Missing language key '{key}'.");
        }

        return language == AppLanguage.CatSpeak
            ? value.CatSpeak
            : value.English;
    }

    public static string Format(string key, AppLanguage language, params object[] args) =>
        string.Format(CultureInfo.CurrentCulture, Get(key, language), args);

    public static AppLanguage ResolveCurrentLanguage()
    {
        if (System.Windows.Application.Current?.MainWindow?.DataContext is MainWindowViewModel vm)
        {
            return vm.IsSillyModeEnabled ? AppLanguage.CatSpeak : AppLanguage.English;
        }

        return AppLanguage.English;
    }

    public static string GetForCurrentLanguage(string key) => Get(key, ResolveCurrentLanguage());

    public static string FormatForCurrentLanguage(string key, params object[] args) =>
        Format(key, ResolveCurrentLanguage(), args);
}
