namespace MultiTool.App.Localization;

public static partial class AppLanguageStrings
{
    private static void AddMainValues(Dictionary<string, (string English, string CatSpeak)> values)
    {
        values[AppLanguageKeys.AppearanceHelperText] = (
            "On first run, the app starts in the same light or dark mode that Windows is already using. You can also choose whether Ctrl + mouse wheel zoom stays on, keep the window on top, start MultiTool with Windows, or hide startup launches straight to the tray.",
            "Pick whether MultiTool starts with Windows, hides to the tray on startup, keeps zoom on, or stays on top.");
        values[AppLanguageKeys.ClickerTabHeader] = ("Clicker", "Clicker :3");
        values[AppLanguageKeys.ClickerHotkeyLabel] = ("Clicker Start/Stop Hotkey", "Clicker Start/Stop Hotkey");
        values[AppLanguageKeys.ClickerForceStopHelperText] = (
            "Shift + the clicker hotkey force stops the clicker.",
            "Shift + the clicker pawkey force stops the clicker.");
        values[AppLanguageKeys.ScreenshotTabHeader] = ("Screenshot", "Screenshot");
        values[AppLanguageKeys.MacroTabHeader] = ("Macro", "Meowcro");
        values[AppLanguageKeys.MacroSetShortcutButton] = ("Set Shortcut", "Set Shortcut");
        values[AppLanguageKeys.MacroEditShortcutsButton] = ("Edit Shortcuts", "Edit Shortcuts");
        values[AppLanguageKeys.ToolsTabHeader] = ("Tools", "Cat Tools");
        values[AppLanguageKeys.NicheToolsHeader] = ("Niche Tools", "Niche Cat Tools");
        values[AppLanguageKeys.ShortcutKeyExplorerHeader] = ("Shortcut Key Explorer", "Shortcut Key Explorer");
        values[AppLanguageKeys.ShortcutExplorerButton] = ("Open Shortcut Explorer", "Open Shortcut Explorer");
        values[AppLanguageKeys.InstallerTabHeader] = ("Installer", "Instawller");
        values[AppLanguageKeys.SettingsTabHeader] = ("Settings", "Settings");
        values[AppLanguageKeys.AppearanceHeader] = ("Appearance", "Appearance");
        values[AppLanguageKeys.DarkModeLabel] = ("Dark mode", "Tabby Mode");
        values[AppLanguageKeys.CtrlWheelResizeLabel] = ("Ctrl + mouse wheel zoom", "Ctrl + mouse wheel zoom");
        values[AppLanguageKeys.AlwaysOnTopLabel] = ("Keep MultiTool on top", "Keep MultiTool on top");
        values[AppLanguageKeys.CatTranslatorLabel] = ("Cat Translator", "Cat Translator :3");
        values[AppLanguageKeys.RunAtStartupLabel] = ("Run at startup", "Run at startup");
        values[AppLanguageKeys.AutoHideOnStartupLabel] = ("Auto-hide to tray on startup", "Auto-hide to tray on startup");
        values[AppLanguageKeys.ShortcutOverrideLabel] = ("Override app shortcuts with modifier hotkeys", "Override app shortcuts with modifier hotkeys");
        values[AppLanguageKeys.ShortcutOverrideHelperText] = (
            "When this is on, explicit combos like Ctrl + C can run MultiTool instead of Copy in other apps. This can interfere with copy/paste, IDE shortcuts, games, and accessibility tools, so only turn it on if you really want that behavior.",
            "When this is on, explicit combos like Ctrl + C can run MultiTool instead of Copy in other apps. This can interfere with copy/paste, IDE shortcuts, games, and accessibility tools, so only turn it on if you really want that behavior.");
        values[AppLanguageKeys.ResetAllSettingsButton] = ("Reset All Settings", "Reset All Settings");
        values[AppLanguageKeys.BugCheckingHeader] = ("Bug Checking", "Bug Checking :3");
        values[AppLanguageKeys.BugCheckingHelperText] = (
            "If something looks off, use Copy Log and paste it when reporting the bug.",
            "If something looks off, use Copy Log and paste it when repurrting the bug.");
        values[AppLanguageKeys.CopyLogButton] = ("Copy Log", "Copy Log");
        values[AppLanguageKeys.MainAdminBannerAdmin] = ("MultiTool is running as administrator.", "MultiTool is running as administrator.");
        values[AppLanguageKeys.MainAdminBannerNotAdmin] = ("Running without administrator access. Open MultiTool as administrator for installs, hardware sensors, drivers, and Windows changes that need elevated access.", "Running without administrator access. Open MultiTool as administrator for installs, hardware sensors, drivers, and Windows changes that need elevated access.");
        values[AppLanguageKeys.MainAdminActivityNotAdmin] = ("Running without administrator access. Open MultiTool as administrator for full access to elevated features.", "Running without administrator access. Open MultiTool as administrator for full access to elevated features.");
        values[AppLanguageKeys.MainWindowTitleDefault] = ("MultiTool", "MultiTool");
        values[AppLanguageKeys.MainWindowTitleRunning] = ("MultiTool - Running...", "MultiTool - Running...");
        values[AppLanguageKeys.MainWindowTitleNotAdminSuffix] = (" (Not Admin)", " (Not Admin)");
        values[AppLanguageKeys.MainStatusLoadingSettings] = ("Loading settings...", "Loading settings...");
        values[AppLanguageKeys.MainCustomKeyPrompt] = ("Click here and press a key", "Click here and press a key");
        values[AppLanguageKeys.MainCustomKeyOrMousePrompt] = ("Click here and press a key or mouse button", "Click here and press a key or mouse button");
        values[AppLanguageKeys.HotkeyEditToolTip] = (
            "Click here, then press the new hotkey or key combination.",
            "Click here, then press the new hotkey or key combo.");
        values[AppLanguageKeys.HotkeyLabel] = ("Hotkey", "Hotkey");
        values[AppLanguageKeys.CaptureButton] = ("Capture", "Capture");
        values[AppLanguageKeys.BrowseButton] = ("Browse", "Browse");
        values[AppLanguageKeys.CaptureScreenButton] = ("Capture Screen", "Capture Screen");
        values[AppLanguageKeys.OpenFolderButton] = ("Open Folder", "Open Folder");
        values[AppLanguageKeys.NewMacroButton] = ("New Macro", "New Macro");
        values[AppLanguageKeys.RecordButton] = ("Record", "Record");
        values[AppLanguageKeys.StopButton] = ("Stop", "Stop");
        values[AppLanguageKeys.PlayButton] = ("Play", "Play");
        values[AppLanguageKeys.SaveButton] = ("Save", "Save");
        values[AppLanguageKeys.LoadButton] = ("Load", "Load");
        values[AppLanguageKeys.RefreshButton] = ("Refresh", "Refresh");
        values[AppLanguageKeys.LoadSelectedButton] = ("Load Selected", "Load Selected");
        values[AppLanguageKeys.EditSelectedButton] = ("Edit Selected", "Edit Selected");
        values[AppLanguageKeys.MainMouseButtonLeft] = ("Left Mouse Button", "Left Mouse Button");
        values[AppLanguageKeys.MainMouseButtonRight] = ("Right Mouse Button", "Right Mouse Button");
        values[AppLanguageKeys.MainMouseButtonMiddle] = ("Middle Mouse Button", "Middle Mouse Button");
        values[AppLanguageKeys.MainMouseButton4] = ("Mouse Button 4", "Mouse Button 4");
        values[AppLanguageKeys.MainMouseButton5] = ("Mouse Button 5", "Mouse Button 5");
        values[AppLanguageKeys.MainScreenshotFilePrefixDefault] = ("Screenshot", "Screenshot");
        values[AppLanguageKeys.MainIntervalLabel] = ("Interval", "Interval");
        values[AppLanguageKeys.MainHoursLabel] = ("Hours", "Hours");
        values[AppLanguageKeys.MainMinutesLabel] = ("Minutes", "Minutes");
        values[AppLanguageKeys.MainSecondsLabel] = ("Seconds", "Seconds");
        values[AppLanguageKeys.MainMillisecondsLabel] = ("Milliseconds", "Milliseconds");
        values[AppLanguageKeys.MainRandomTimingLabel] = ("Random timing", "Random timing");
        values[AppLanguageKeys.MainRandomTimingVarianceLabel] = ("Variation (+/- ms)", "Variation (+/- ms)");
        values[AppLanguageKeys.MainRandomTimingHelperText] = ("Each click varies around the base interval by up to this many milliseconds.", "Each click varies around the base interval by up to this many milliseconds.");
        values[AppLanguageKeys.MainRepeatLabel] = ("Repeat", "Repeat");
        values[AppLanguageKeys.MainPositionLabel] = ("Position", "Position");
        values[AppLanguageKeys.MainXLabel] = ("X", "X");
        values[AppLanguageKeys.MainYLabel] = ("Y", "Y");
        values[AppLanguageKeys.MainNameLabel] = ("Name", "Name");
        values[AppLanguageKeys.MainPlayCountLabel] = ("Play Count", "Play Count");
        values[AppLanguageKeys.MainMacroInfiniteLabel] = ("Infinite", "Infinite");
        values[AppLanguageKeys.MainMacroInfiniteHelperFormat] = ("With Infinite on, press {0} again to stop the macro.", "With Infinite on, press {0} again to stop the macro.");
        values[AppLanguageKeys.MainRecordMouseMovementLabel] = ("Record mouse movement", "Record mouse movement");
        values[AppLanguageKeys.MainPlayHotkeyLabel] = ("Play Hotkey", "Play Hotkey");
        values[AppLanguageKeys.MainRecordHotkeyLabel] = ("Record Hotkey", "Record Hotkey");
        values[AppLanguageKeys.MainSavedLabel] = ("Saved", "Saved");
        values[AppLanguageKeys.MainNoAssignedMacroShortcuts] = ("No keyboard shortcuts are set yet.", "No keyboard shortcuts are set yet.");
        values[AppLanguageKeys.MainInputLabel] = ("Input", "Input");
        values[AppLanguageKeys.MainTypeLabel] = ("Type", "Type");
        values[AppLanguageKeys.MainCustomInputLabel] = ("Custom Input", "Custom Input");
        values[AppLanguageKeys.MainFolderLabel] = ("Folder", "Folder");
        values[AppLanguageKeys.MainPrefixLabel] = ("Prefix", "Prefix");
        values[AppLanguageKeys.MainScreenshotHelperText] = (
            "Press the screenshot hotkey once for a full-screen PNG, twice quickly for an area capture, three times quickly to choose how to start a video recording, or four times quickly to record the current screen right away. While recording, press that same hotkey once to stop and save the video.",
            "Press the screenshot pawkey once for a full-screen PNG, twice quickly for an area capture, three times quickly to choose how to start a video recording, or four times quickly to record the current screen right away. While recording, press that same pawkey once to stop and save the Video.");
        values[AppLanguageKeys.MainScreenshotStatusReady] = ("Ready to capture the desktop.", "Ready to capture the desktop.");
        values[AppLanguageKeys.MainLatestScreenshotNone] = ("No screenshot captured yet.", "No screenshot captured yet.");
        values[AppLanguageKeys.MainLatestVideoNone] = ("No video recorded yet.", "No video recorded yet.");
        values[AppLanguageKeys.MainLatestScreenshotPlaceholder] = ("Take a screenshot and the latest image will appear here.", "Take a screenshot and the latest image will appear here.");
        values[AppLanguageKeys.MouseSensitivityVerySlow] = ("Very Slow", "Very Slow");
        values[AppLanguageKeys.MouseSensitivitySlow] = ("Slow", "Slow");
        values[AppLanguageKeys.MouseSensitivityBalanced] = ("Balanced", "Balanced");
        values[AppLanguageKeys.MouseSensitivityFast] = ("Fast", "Fast");
        values[AppLanguageKeys.MouseSensitivityVeryFast] = ("Very Fast", "Very Fast");
        values[AppLanguageKeys.MainMacroNameDefault] = ("New Macro", "New Macro");
        values[AppLanguageKeys.MainMacroLogReady] = ("Macro log ready.", "Macro log ready.");
        values[AppLanguageKeys.MainMacroLogNoRecordedYet] = ("No macro recorded yet.", "No macro recorded yet.");
        values[AppLanguageKeys.MainMacroSummaryNoRecorded] = ("No macro recorded yet.", "No macro recorded yet.");
        values[AppLanguageKeys.MainMacroStatusReady] = ("Ready for recording or playback setup.", "Ready for recording or playback setup.");
        values[AppLanguageKeys.MainSettingsStatusInitial] = ("Dark mode will match Windows the first time the app runs.", "Dark mode will match Windows the first time the app runs.");
        values[AppLanguageKeys.MainSettingsStatusDarkModeOn] = ("Dark mode is on.", "Dark mode is on.");
        values[AppLanguageKeys.MainSettingsStatusDarkModeOff] = ("Dark mode is off.", "Dark mode is off.");
        values[AppLanguageKeys.MainActivityDarkModeEnabled] = ("Dark mode enabled.", "Dark mode enabled.");
        values[AppLanguageKeys.MainActivityDarkModeDisabled] = ("Dark mode disabled.", "Dark mode disabled.");
        values[AppLanguageKeys.MainSettingsStatusCtrlWheelZoomOn] = ("Ctrl + mouse wheel UI zoom is on.", "Ctrl + mouse wheel UI zoom is on.");
        values[AppLanguageKeys.MainSettingsStatusCtrlWheelZoomOff] = ("Ctrl + mouse wheel UI zoom is off.", "Ctrl + mouse wheel UI zoom is off.");
        values[AppLanguageKeys.MainActivityCtrlWheelZoomEnabled] = ("Enabled Ctrl + mouse wheel UI zoom.", "Enabled Ctrl + mouse wheel UI zoom.");
        values[AppLanguageKeys.MainActivityCtrlWheelZoomDisabled] = ("Disabled Ctrl + mouse wheel UI zoom.", "Disabled Ctrl + mouse wheel UI zoom.");
        values[AppLanguageKeys.MainSettingsStatusRunAtStartupOn] = ("Run at startup is on.", "Run at startup is on.");
        values[AppLanguageKeys.MainSettingsStatusRunAtStartupOff] = ("Run at startup is off.", "Run at startup is off.");
        values[AppLanguageKeys.MainSettingsStatusRunAtStartupFailedFormat] = ("Unable to change run at startup: {0}", "Could not change run at startup: {0}");
        values[AppLanguageKeys.MainActivityRunAtStartupEnabled] = ("Enabled run at startup.", "Enabled run at startup.");
        values[AppLanguageKeys.MainActivityRunAtStartupDisabled] = ("Disabled run at startup.", "Disabled run at startup.");
        values[AppLanguageKeys.MainSettingsStatusAutoHideOn] = ("Auto-hide on startup is on.", "Auto-hide on startup is on.");
        values[AppLanguageKeys.MainSettingsStatusAutoHideOff] = ("Auto-hide on startup is off.", "Auto-hide on startup is off.");
        values[AppLanguageKeys.MainActivityAutoHideEnabled] = ("Enabled auto-hide on startup.", "Enabled auto-hide on startup.");
        values[AppLanguageKeys.MainActivityAutoHideDisabled] = ("Disabled auto-hide on startup.", "Disabled auto-hide on startup.");
        values[AppLanguageKeys.MainSettingsStatusShortcutOverrideOn] = ("Application shortcut override is on.", "Application shortcut override is on.");
        values[AppLanguageKeys.MainSettingsStatusShortcutOverrideOff] = ("Application shortcut override is off.", "Application shortcut override is off.");
        values[AppLanguageKeys.MainActivityShortcutOverrideEnabled] = ("Enabled application shortcut override for modifier hotkeys.", "Enabled application shortcut override for modifier hotkeys.");
        values[AppLanguageKeys.MainActivityShortcutOverrideDisabled] = ("Disabled application shortcut override for modifier hotkeys.", "Disabled application shortcut override for modifier hotkeys.");
        values[AppLanguageKeys.MainSettingsStatusCatTranslatorOn] = ("Cat Translator is on. Meow.", "Cat Translator is on. Meow.");
        values[AppLanguageKeys.MainActivityCatTranslatorEnabled] = ("Enabled Cat Translator.", "Enabled Cat Translator.");
        values[AppLanguageKeys.MainActivityCatTranslatorDisabled] = ("Disabled Cat Translator.", "Disabled Cat Translator.");
        values[AppLanguageKeys.MainStatusReady] = ("Ready.", "Ready.");
        values[AppLanguageKeys.MainActivityLogReady] = ("Activity log ready.", "Activity log ready.");
        values[AppLanguageKeys.MainActivitySettingsLoaded] = ("Settings loaded.", "Settings loaded.");
        values[AppLanguageKeys.MainStatusClicking] = ("Clicking...", "Clicking...");
        values[AppLanguageKeys.MainStatusClickerHotkeyIgnoredWhileFocused] = ("Clicker start hotkey ignored while MultiTool is focused. Switch to another app, then press it again.", "Clicker start hotkey ignored while MultiTool is focused. Switch to another app, then press it again.");
        values[AppLanguageKeys.MainStatusAutomationStopped] = ("Automation stopped.", "Automation stopped.");
        values[AppLanguageKeys.MainStatusSettingsSaved] = ("Settings saved.", "Settings saved.");
        values[AppLanguageKeys.MainStatusCapturedCoordinatesFormat] = ("Captured coordinates: {0}, {1}.", "Captured coordinates: {0}, {1}.");
        values[AppLanguageKeys.MainScreenshotStatusOpenedFolder] = ("Opened screenshot folder.", "Opened screenshot folder.");
        values[AppLanguageKeys.MainScreenshotStatusOpenFolderFailedFormat] = ("Unable to open screenshot folder: {0}", "Unable to open screenshot folder: {0}");
        values[AppLanguageKeys.MainMacroStatusStartedNew] = ("Started a new macro.", "Started a new macro.");
        values[AppLanguageKeys.MainMacroSummaryRecordingFormat] = ("Recording '{0}'...", "Recording '{0}'...");
        values[AppLanguageKeys.MainMacroStatusRecordingWithMouseFormat] = ("Recording '{0}' with mouse movement. Input inside this window is ignored while it stays focused.", "Recording '{0}' with mouse movement. Input inside this window is ignored while it stays focused.");
        values[AppLanguageKeys.MainMacroStatusRecordingWithoutMouseFormat] = ("Recording '{0}' without mouse movement. Input inside this window is ignored while it stays focused.", "Recording '{0}' without mouse movement. Input inside this window is ignored while it stays focused.");
        values[AppLanguageKeys.MainMacroLogStartedRecordingFormat] = ("Started recording '{0}'.", "Started recording '{0}'.");
        values[AppLanguageKeys.MainMacroStatusStartRecordingFailedFormat] = ("Unable to start recording: {0}", "Unable to start recording: {0}");
        values[AppLanguageKeys.MainMacroStatusCannotRecordWhilePlaying] = ("Cannot start recording while a macro is playing.", "Cannot start recording while a macro is playing.");
        values[AppLanguageKeys.MainMacroLogRecordHotkeyIgnoredPlaying] = ("Record hotkey ignored because a macro is currently playing.", "Record hotkey ignored because a macro is currently playing.");
        values[AppLanguageKeys.MainMacroSummaryRecordedFormat] = ("{0}: {1} events over {2:N0} ms", "{0}: {1} events over {2:N0} ms");
        values[AppLanguageKeys.MainMacroSummaryNoInputCapturedFormat] = ("{0}: no input captured", "{0}: no input captured");
        values[AppLanguageKeys.MainMacroStatusStoppedRecordingFormat] = ("Stopped recording '{0}'.", "Stopped recording '{0}'.");
        values[AppLanguageKeys.MainMacroStatusStoppedRecordingNoInputFormat] = ("Stopped recording '{0}', but no input was captured.", "Stopped recording '{0}', but no input was captured.");
        values[AppLanguageKeys.MainMacroLogStoppedRecordingFormat] = ("Stopped recording '{0}' with {1} events.", "Stopped recording '{0}' with {1} events.");
        values[AppLanguageKeys.MainMacroStatusStopRecordingFailedFormat] = ("Unable to stop recording: {0}", "Unable to stop recording: {0}");
        values[AppLanguageKeys.MainMacroStatusNoRecordedToPlay] = ("There is no recorded macro to play.", "There is no recorded macro to play.");
        values[AppLanguageKeys.MainMacroLogPlaybackRequestedNoMacro] = ("Playback requested, but no recorded macro is available.", "Playback requested, but no recorded macro is available.");
        values[AppLanguageKeys.MainMacroStatusAlreadyPlaying] = ("A macro is already playing.", "A macro is already playing.");
        values[AppLanguageKeys.MainMacroStatusPlayingFormat] = ("Playing '{0}' x{1}.", "Playing '{0}' x{1}.");
        values[AppLanguageKeys.MainMacroStatusPlayingInfiniteFormat] = ("Playing '{0}' infinitely. Press {1} again to stop.", "Playing '{0}' infinitely. Press {1} again to stop.");
        values[AppLanguageKeys.MainMacroStatusStoppedInfiniteFormat] = ("Stopped infinite playback for '{0}'.", "Stopped infinite playback for '{0}'.");
        values[AppLanguageKeys.MainMacroStatusFinishedPlayingFormat] = ("Finished playing '{0}'.", "Finished playing '{0}'.");
        values[AppLanguageKeys.MainMacroStatusPlayFailedFormat] = ("Unable to play macro: {0}", "Unable to play macro: {0}");
        values[AppLanguageKeys.MainMacroStatusNoRecordedToSave] = ("There is no recorded macro to save.", "There is no recorded macro to save.");
        values[AppLanguageKeys.MainMacroLogSaveRequestedNoMacro] = ("Save requested, but no macro is available.", "Save requested, but no macro is available.");
        values[AppLanguageKeys.MainMacroStatusSaveCanceled] = ("Save canceled.", "Save canceled.");
        values[AppLanguageKeys.MainMacroStatusSavedToMacrosFormat] = ("Saved macro to Macros\\{0}.", "Saved macro to Macros\\{0}.");
        values[AppLanguageKeys.MainMacroLogSavedToPathFormat] = ("Saved macro to {0}.", "Saved macro to {0}.");
        values[AppLanguageKeys.MainMacroStatusSaveFailedFormat] = ("Unable to save macro: {0}", "Unable to save macro: {0}");
        values[AppLanguageKeys.MainMacroStatusLoadCanceled] = ("Load canceled.", "Load canceled.");
        values[AppLanguageKeys.MainMacroStatusLoadedFromFileFormat] = ("Loaded macro from {0}.", "Loaded macro from {0}.");
        values[AppLanguageKeys.MainMacroLogLoadedFromPathFormat] = ("Loaded macro from {0}.", "Loaded macro from {0}.");
        values[AppLanguageKeys.MainMacroStatusLoadFailedFormat] = ("Unable to load macro: {0}", "Unable to load macro: {0}");
        values[AppLanguageKeys.MainMacroStatusChooseSavedFirst] = ("Choose a saved macro first.", "Choose a saved macro first.");
        values[AppLanguageKeys.MainMacroLogLoadSelectedNoSaved] = ("Load selected requested, but no saved macro is selected.", "Load selected requested, but no saved macro is selected.");
        values[AppLanguageKeys.MainMacroStatusLoadedSavedFormat] = ("Loaded saved macro '{0}'.", "Loaded saved macro '{0}'.");
        values[AppLanguageKeys.MainMacroLogLoadedSavedPathFormat] = ("Loaded saved macro from {0}.", "Loaded saved macro from {0}.");
        values[AppLanguageKeys.MainMacroStatusLoadSavedFailedFormat] = ("Unable to load saved macro: {0}", "Unable to load saved macro: {0}");
        values[AppLanguageKeys.MainMacroLogEditRequestedNoSaved] = ("Edit requested, but no saved macro is selected.", "Edit requested, but no saved macro is selected.");
        values[AppLanguageKeys.MainMacroStatusEditCanceled] = ("Edit canceled.", "Edit canceled.");
        values[AppLanguageKeys.MainMacroStatusSavedEditsFormat] = ("Saved edits to '{0}'.", "Saved edits to '{0}'.");
        values[AppLanguageKeys.MainMacroLogSavedEditedToPathFormat] = ("Saved edited macro to {0}.", "Saved edited macro to {0}.");
        values[AppLanguageKeys.MainMacroStatusEditSavedFailedFormat] = ("Unable to edit saved macro: {0}", "Unable to edit saved macro: {0}");
        values[AppLanguageKeys.MainMacroStatusFoundSavedFormat] = ("Found {0} saved macro{1}.", "Found {0} saved macro{1}.");
        values[AppLanguageKeys.MainMacroStatusNoSavedInDefaultFolder] = ("No saved macros found in the default macros folder.", "No saved macros found in the default macros folder.");
        values[AppLanguageKeys.MainMacroStatusSavedFolderUnavailableFormat] = ("Saved macros folder is unavailable: {0}", "Saved macros folder is unavailable: {0}");
        values[AppLanguageKeys.MainMacroStatusOpenedSavedFolder] = ("Opened the saved macros folder.", "Opened the saved macros folder.");
        values[AppLanguageKeys.MainMacroStatusOpenSavedFolderFailedFormat] = ("Unable to open the saved macros folder: {0}", "Unable to open the saved macros folder: {0}");
        values[AppLanguageKeys.MainMacroStatusLogCleared] = ("Macro log cleared.", "Macro log cleared.");
        values[AppLanguageKeys.MainStatusSettingsAutoSaved] = ("Settings auto-saved.", "Settings auto-saved.");
        values[AppLanguageKeys.MainStatusCustomKeySetFormat] = ("Custom key set to {0}.", "Custom key set to {0}.");
        values[AppLanguageKeys.MainScreenshotStatusHotkeySetFormat] = ("Screenshot hotkey set to {0}.", "Screenshot hotkey set to {0}.");
        values[AppLanguageKeys.MainStatusClickerHotkeySetFormat] = ("Clicker hotkey set to {0}.", "Clicker hotkey set to {0}.");
        values[AppLanguageKeys.MainMacroStatusHotkeySetFormat] = ("Macro hotkey set to {0}.", "Macro hotkey set to {0}.");
        values[AppLanguageKeys.MainMacroStatusRecordHotkeySetFormat] = ("Macro record hotkey set to {0}.", "Macro record hotkey set to {0}.");
        values[AppLanguageKeys.MainStatusCustomInputSetFormat] = ("Custom input set to {0}.", "Custom input set to {0}.");
        values[AppLanguageKeys.MainScreenshotFolderPickerPrompt] = ("Select the folder to save screenshots in", "Select the folder to save screenshots in");
        values[AppLanguageKeys.MainScreenshotStatusFolderSelectionCanceled] = ("Folder selection canceled.", "Folder selection canceled.");
        values[AppLanguageKeys.MainScreenshotStatusFolderSetFormat] = ("Screenshot folder set to {0}.", "Screenshot folder set to {0}.");
        values[AppLanguageKeys.MainActivityLogEmpty] = ("Activity log is empty.", "Activity log is empty.");
        values[AppLanguageKeys.MainSettingsStatusCopiedActivityLog] = ("Copied activity log to the clipboard.", "Copied activity log to the clipboard.");
        values[AppLanguageKeys.MainSettingsStatusResetRequested] = ("Settings reset to defaults.", "Settings reset to defaults.");
        values[AppLanguageKeys.MainSettingsStatusResetCompleted] = ("All settings were reset to defaults.", "All settings were reset to defaults.");
        values[AppLanguageKeys.MainSettingsStatusResetSaveFailed] = ("Reset applied in the UI, but saving the defaults failed.", "Reset applied in the UI, but saving the defaults failed.");
        values[AppLanguageKeys.MainScreenshotStatusSavedVideoFormat] = ("Saved video: {0}.", "Saved video: {0}.");
        values[AppLanguageKeys.MainScreenshotStatusSavedAndCopiedFormat] = ("Saved {0} and copied it to the clipboard.", "Saved {0} and copied it to the clipboard.");
        values[AppLanguageKeys.MainScreenshotStatusSavedAreaAndArmedVideoFormat] = ("Saved {0} and copied it to the clipboard. Press Shift + the screenshot hotkey again within 3 seconds to record this same area.", "Saved {0} and copied it to the clipboard. Press Shift + the Screenshot pawkey again within 3 seconds to record this same area.");
        values[AppLanguageKeys.MainScreenshotLogSavedFullScreenFormat] = ("Saved full-screen capture to {0} and copied it to the clipboard.", "Saved full-screen capture to {0} and copied it to the clipboard.");
        values[AppLanguageKeys.MainScreenshotStatusAreaCanceled] = ("Area capture canceled.", "Area capture canceled.");
        values[AppLanguageKeys.MainScreenshotStatusVideoCanceled] = ("Video recording selection canceled.", "Video recording selection canceled.");
        values[AppLanguageKeys.MainScreenshotLogSavedAreaFormat] = ("Saved area capture to {0} and copied it to the clipboard.", "Saved area capture to {0} and copied it to the clipboard.");
        values[AppLanguageKeys.MainScreenshotStatusAreaRecordingStarted] = ("Started recording the selected area. Press the screenshot hotkey again to stop and save.", "Started recording the selected area. Press the Screenshot pawkey again to stop and save.");
        values[AppLanguageKeys.MainScreenshotStatusCurrentScreenRecordingStarted] = ("Started recording the current screen. Press the screenshot hotkey again to stop and save.", "Started recording the current screen. Press the Screenshot pawkey again to stop and save.");
        values[AppLanguageKeys.MainScreenshotStatusAllScreensRecordingStarted] = ("Started recording all screens. Press the screenshot hotkey again to stop and save.", "Started recording all screens. Press the Screenshot pawkey again to stop and save.");
        values[AppLanguageKeys.MainScreenshotStatusFailedFormat] = ("Screenshot failed: {0}", "Screenshot failed: {0}");
        values[AppLanguageKeys.MainStatusUnableSaveSettingsFormat] = ("Unable to save settings: {0}", "Unable to save settings: {0}");
        values[AppLanguageKeys.TrayStartupHiddenStatus] = (
            "MultiTool started hidden in the tray.",
            "MultiTool started hiding in the tray :3");
        values[AppLanguageKeys.TrayMinimizedStatus] = (
            "MultiTool was minimized to the tray.",
            "MultiTool was minimized to the tray :3");
        values[AppLanguageKeys.HotkeysRegisterWhenReadyStatus] = (
            "Hotkeys will register once the main window is ready.",
            "Pawkeys will register once the main window is ready.");

    }
}
