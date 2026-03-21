namespace MultiTool.App.Localization;

public static partial class AppLanguageStrings
{
    private static void AddShortcutValues(Dictionary<string, (string English, string CatSpeak)> values)
    {
        values[AppLanguageKeys.ShortcutExplorerTitle] = ("Shortcut Key Explorer", "Shortcut Key Explorer");
        values[AppLanguageKeys.ShortcutExplorerHeading] = ("Shortcut Key Explorer", "Shortcut Key Explorer");
        values[AppLanguageKeys.ShortcutExplorerSearchLabel] = ("Search", "Search");
        values[AppLanguageKeys.ShortcutExplorerConflictsOnly] = ("Show only conflicts", "Show only conflicts");
        values[AppLanguageKeys.ShortcutExplorerColumnHotkey] = ("Hotkey", "Hotkey");
        values[AppLanguageKeys.ShortcutExplorerColumnEnabled] = ("Enabled", "Enabled");
        values[AppLanguageKeys.ShortcutExplorerColumnShortcut] = ("Shortcut", "Shortcut");
        values[AppLanguageKeys.ShortcutExplorerColumnSource] = ("Source", "Source");
        values[AppLanguageKeys.ShortcutExplorerColumnAppliesTo] = ("Applies To", "Applies To");
        values[AppLanguageKeys.ShortcutExplorerColumnConflict] = ("Conflict", "Conflict");
        values[AppLanguageKeys.ShortcutExplorerColumnDetails] = ("Details", "Details");
        values[AppLanguageKeys.ShortcutExplorerColumnFile] = ("Source File", "Source File");
        values[AppLanguageKeys.ShortcutExplorerRescan] = ("Rescan", "Rescan");
        values[AppLanguageKeys.ShortcutExplorerDisableSelected] = ("Apply Changes", "Apply Changes");
        values[AppLanguageKeys.ShortcutExplorerClose] = ("Close", "Close");
        values[AppLanguageKeys.ShortcutExplorerSummaryNoneFormat] = (
            "Scanned {0} Windows shortcut file{1} on fixed drives and checked compatible app shortcut sources that could be discovered on this PC. No shortcut keys were found.",
            "Scanned {0} Windows shortcut file{1} on fixed drives and checked compatible app shortcut sources that could be discovered on this PC. No shortcut keys were found.");
        values[AppLanguageKeys.ShortcutExplorerSummaryFoundFormat] = (
            "Found {0} detected shortcut{1} after scanning {2} Windows shortcut file{3} on fixed drives and checking compatible app shortcut sources that could be discovered on this PC.",
            "Found {0} detected shortcut{1} after scanning {2} Windows shortcut file{3} on fixed drives and checking compatible app shortcut sources that could be discovered on this PC.");
        values[AppLanguageKeys.ShortcutExplorerSummaryNoAssigned] = (
            "No shortcut keys were detected from Windows shortcut files or compatible app shortcut sources that could be discovered on this PC.",
            "No shortcut keys were detected from Windows shortcut files or compatible app shortcut sources that could be discovered on this PC.");
        values[AppLanguageKeys.ShortcutExplorerReferenceIncludedFormat] = (
            " Included {0} built-in Windows and common app shortcut reference entr{1}.",
            " Included {0} built-in Windows and common app shortcut reference entr{1}.");
        values[AppLanguageKeys.ShortcutExplorerWarningSkippedFormat] = (
            "Skipped {0} folder or shortcut read{1} during the scan.",
            "Skipped {0} folder or shortcut read{1} during the scan.");
        values[AppLanguageKeys.ShortcutExplorerReferenceNote] = (
            "Built-in Windows and common app shortcuts are included as a reference catalog. The scanner also pulls real bindings from Windows shortcut files, heuristically discovered compatible app keymap and settings files, plus AutoHotkey scripts when available, but Windows still has no universal API that exposes every private keybind from every program.",
            "Built-in Windows and common app shortcuts are included as a reference catalog. The scanner also pulls real bindings from Windows shortcut files, heuristically discovered compatible app keymap and settings files, plus AutoHotkey scripts when available, but Windows still has no universal API that exposes every private keybind from every program.");
        values[AppLanguageKeys.ShortcutExplorerConflictWarningFormat] = (
            "Warning: {0} shortcut{1} share {2} hotkey{3}. Some overlaps are harmless reference combos, but detected shortcut files, app keymaps, and script bindings can still conflict in real use.",
            "Warning: {0} shortcut{1} share {2} hotkey{3}. Some overlaps are harmless reference combos, but detected shortcut files, app keymaps, and script bindings can still conflict in real use.");
        values[AppLanguageKeys.ShortcutExplorerFilterListedFormat] = ("{0} shortcut{1} listed", "{0} shortcut{1} listed");
        values[AppLanguageKeys.ShortcutExplorerFilterMatchingFormat] = ("{0} matching shortcut{1} shown", "{0} matching shortcut{1} shown");
        values[AppLanguageKeys.ShortcutExplorerFilterConflictSuffixFormat] = (
            ". {0} shortcut{1} are in conflict across {2} shared hotkey{3}.",
            ". {0} shortcut{1} are in conflict across {2} shared hotkey{3}.");
        values[AppLanguageKeys.ShortcutExplorerFilterSourceSuffixFormat] = (
            ". {0} detected from this PC, {1} built-in or common references",
            ". {0} detected from this PC, {1} built-in or common references");
        values[AppLanguageKeys.ShortcutExplorerStatusReady] = (
            "Shortcut results loaded. Use Rescan to refresh them.",
            "Shortcut results loaded. Use Rescan to refresh them.");
        values[AppLanguageKeys.ShortcutExplorerStatusCachedResults] = (
            "Showing the last found shortcut results. Use Rescan to refresh them.",
            "Showing the last found shortcut results. Use Rescan to refresh them.");
        values[AppLanguageKeys.ShortcutExplorerStatusRescanning] = (
            "Rescanning shortcut sources...",
            "Rescanning shortcut sources...");
        values[AppLanguageKeys.ShortcutExplorerStatusRescannedFormat] = (
            "Shortcut list refreshed with {0} detected shortcut{1} and {2} built-in/common reference entr{3}.",
            "Shortcut list refreshed with {0} detected shortcut{1} and {2} built-in/common reference entr{3}.");
        values[AppLanguageKeys.ShortcutExplorerStatusRescanFailedFormat] = (
            "Shortcut rescan failed: {0}",
            "Shortcut rescan failed: {0}");
        values[AppLanguageKeys.ShortcutExplorerStatusDisableNoSelection] = (
            "Clear one or more Enabled checkboxes first.",
            "Clear one or more Enabled checkboxes first.");
        values[AppLanguageKeys.ShortcutExplorerStatusDisabling] = (
            "Disabling selected shortcut hotkeys...",
            "Disabling selected shortcut hotkeys...");
        values[AppLanguageKeys.ShortcutExplorerStatusDisabledFormat] = (
            "Disabled {0} shortcut{1}.{2} The list was refreshed.{3}",
            "Disabled {0} shortcut{1}.{2} The list was refreshed.{3}");
        values[AppLanguageKeys.ShortcutExplorerStatusDisableSkippedUnsupportedSuffixFormat] = (
            " Skipped {0} unsupported entr{1}.",
            " Skipped {0} unsupported entr{1}.");
        values[AppLanguageKeys.ShortcutExplorerStatusDisableNoChangesFormat] = (
            "No shortcut hotkeys were disabled.{0}",
            "No shortcut hotkeys were disabled.{0}");
        values[AppLanguageKeys.ShortcutExplorerStatusDisableUnsupportedFormat] = (
            "The selected entries can't be disabled from here yet. Only detected Windows shortcut files are supported.{0}",
            "The selected entries can't be disabled from here yet. Only detected Windows shortcut files are supported.{0}");
        values[AppLanguageKeys.ShortcutExplorerStatusDisableWarningsSuffixFormat] = (
            " Skipped {0} shortcut change{1}.",
            " Skipped {0} shortcut change{1}.");
        values[AppLanguageKeys.ShortcutExplorerStatusDisableFailedFormat] = (
            "Shortcut disable failed: {0}",
            "Shortcut disable failed: {0}");

        values[AppLanguageKeys.MacroHotkeyNoSelectedSummary] = (
            "Select a saved macro to set a keyboard shortcut.",
            "Select a saved meowcro to set a keyboard shortcut.");
        values[AppLanguageKeys.MacroHotkeyNotSetForSelectedFormat] = ("No keyboard shortcut is set for '{0}' yet.", "No keyboard shortcut is set for '{0}' yet.");
        values[AppLanguageKeys.MacroHotkeySelectedSummaryFormat] = (
            "Shortcut: {0}. Action: {1}. Turned {2}.",
            "Shortcut: {0}. Action: {1}. Turned {2}.");
        values[AppLanguageKeys.MacroHotkeyDefaultAssignmentsSummary] = (
            "No saved macros have keyboard shortcuts yet.",
            "No saved meowcros have keyboard shortcuts yet.");
        values[AppLanguageKeys.MacroHotkeyLogSetupUnavailableNoSaved] = (
            "Shortcut setup is unavailable because there are no saved macros yet.",
            "Shortcut setup is unavailable because there are no saved meowcros yet.");
        values[AppLanguageKeys.MacroHotkeyLogChangesRejectedFormat] = (
            "Shortcut changes were rejected: {0}",
            "Shortcut changes were rejected: {0}");
        values[AppLanguageKeys.MacroHotkeyStatusStoppedActiveBecauseShortcutChanged] = (
            "Stopped the active repeating macro because its shortcut changed.",
            "Stopped the active repeating meowcro because its shortcut changed.");
        values[AppLanguageKeys.MacroHotkeyStatusShortcutsUpdated] = (
            "Keyboard shortcuts updated.",
            "Keyboard shortcuts updated.");
        values[AppLanguageKeys.MacroHotkeyStatusShortcutsUpdatedButSaveFailed] = (
            "The shortcuts were updated on screen, but saving them failed.",
            "The shortcuts were updated on screen, but saving them failed.");
        values[AppLanguageKeys.MacroHotkeyStatusShortcutUpdated] = (
            "Keyboard shortcut updated.",
            "Keyboard shortcut updated.");
        values[AppLanguageKeys.MacroHotkeyLogSetRequestedNoSaved] = (
            "Set shortcut was requested, but no saved macro is selected.",
            "Set shortcut was requested, but no saved meowcro is selected.");
        values[AppLanguageKeys.MacroHotkeyLogChangesRejectedForFormat] = (
            "Shortcut changes for '{0}' were rejected: {1}",
            "Shortcut changes for '{0}' were rejected: {1}");
        values[AppLanguageKeys.MacroHotkeyStatusSaveMacroFirst] = ("Save a macro first, then set a keyboard shortcut for it.", "Save a meowcro first, then set a keyboard shortcut for it.");
        values[AppLanguageKeys.MacroHotkeyStatusChangesCanceled] = ("Shortcut changes were canceled.", "Shortcut changes were canceled.");
        values[AppLanguageKeys.MacroHotkeyStatusNoChanges] = ("No shortcut changes were made.", "No shortcut changes were made.");
        values[AppLanguageKeys.MacroHotkeyStatusChooseSavedMacro] = ("Choose a saved macro first.", "Choose a saved meowcro first.");
        values[AppLanguageKeys.MacroHotkeyStatusChangesCanceledForFormat] = ("Shortcut changes for '{0}' were canceled.", "Shortcut changes for '{0}' were canceled.");
        values[AppLanguageKeys.MacroHotkeyStatusUpdatedForFormat] = ("Updated the keyboard shortcut for '{0}'.", "Updated the keyboard shortcut for '{0}'.");
        values[AppLanguageKeys.MacroHotkeyStatusUpdatedButSaveFailedForFormat] = ("Updated the shortcut for '{0}' on screen, but saving it failed.", "Updated the shortcut for '{0}' on screen, but saving it failed.");
        values[AppLanguageKeys.MacroHotkeyStatusNoLongerSet] = ("That shortcut is no longer set.", "That shortcut is no longer set.");
        values[AppLanguageKeys.MacroHotkeyLogIgnoredAssignmentMissing] = (
            "Ignored a shortcut because its assignment no longer exists.",
            "Ignored a shortcut because its assignment no longer exists.");
        values[AppLanguageKeys.MacroHotkeyStatusRecordingConflict] = ("You can't run a saved macro from a shortcut while recording.", "You can't run a saved meowcro from a shortcut while recording.");
        values[AppLanguageKeys.MacroHotkeyLogIgnoredRecordingActiveFormat] = (
            "Ignored shortcut for '{0}' because a recording is active.",
            "Ignored shortcut for '{0}' because a recording is active.");
        values[AppLanguageKeys.MacroHotkeyStatusAnotherPlaying] = ("Another macro is already playing.", "Another meowcro is already playing.");
        values[AppLanguageKeys.MacroHotkeyLogIgnoredAnotherPlayingFormat] = (
            "Ignored shortcut for '{0}' because another macro is already playing.",
            "Ignored shortcut for '{0}' because another meowcro is already playing.");
        values[AppLanguageKeys.MacroHotkeyStatusStoppedFormat] = ("Stopped '{0}'.", "Stopped '{0}'.");
        values[AppLanguageKeys.MacroHotkeyStatusStoppedAndStartedFormat] = (
            "Stopped '{0}' and started '{1}'.",
            "Stopped '{0}' and started '{1}'.");
        values[AppLanguageKeys.MacroHotkeyStatusStoppedToRunOnceFormat] = (
            "Stopped '{0}' so '{1}' could run once.",
            "Stopped '{0}' so '{1}' could run once.");
        values[AppLanguageKeys.MacroHotkeyStatusRunningOnceFormat] = ("Running '{0}' once.", "Running '{0}' once.");
        values[AppLanguageKeys.MacroHotkeyLogRunningOnceFromFormat] = (
            "Running '{0}' once from {1}.",
            "Running '{0}' once from {1}.");
        values[AppLanguageKeys.MacroHotkeyStatusFinishedFormat] = ("Finished '{0}'.", "Finished '{0}'.");
        values[AppLanguageKeys.MacroHotkeyStatusRunFailedFormat] = ("Couldn't run '{0}': {1}", "Couldn't run '{0}': {1}");
        values[AppLanguageKeys.MacroHotkeyStatusToggleRunningFormat] = ("'{0}' is running. Press {1} again to stop.", "'{0}' is running. Press {1} again to stop.");
        values[AppLanguageKeys.MacroHotkeyLogStartedFromAndPressAgainFormat] = (
            "Started '{0}' from {1}. Press the same key again to stop.",
            "Started '{0}' from {1}. Press the same key again to stop.");
        values[AppLanguageKeys.MacroHotkeyStatusToggleStoppedErrorFormat] = ("'{0}' stopped because of an error: {1}", "'{0}' stopped because of an error: {1}");
        values[AppLanguageKeys.MacroHotkeyStatusMissingFileFormat] = ("'{0}' couldn't be found anymore.", "'{0}' couldn't be found anymore.");
        values[AppLanguageKeys.MacroHotkeyLogMissingFilePathFormat] = (
            "Shortcut for '{0}' points to a missing file: {1}",
            "Shortcut for '{0}' points to a missing file: {1}");
        values[AppLanguageKeys.MacroHotkeyStatusNoEventsFormat] = ("'{0}' has nothing recorded to run.", "'{0}' has nothing recorded to run.");
        values[AppLanguageKeys.MacroHotkeyLogNoEventsFormat] = (
            "Shortcut for '{0}' was skipped because the macro file has no events.",
            "Shortcut for '{0}' was skipped because the meowcro file has no events.");
        values[AppLanguageKeys.MacroHotkeyStatusLoadFailedFormat] = ("Couldn't load '{0}': {1}", "Couldn't load '{0}': {1}");
        values[AppLanguageKeys.MacroHotkeyStatusStoppedActiveFileUnavailable] = (
            "Stopped the active repeating macro because its file is no longer available.",
            "Stopped the active repeating meowcro because its file is no longer available.");
        values[AppLanguageKeys.MacroHotkeyLogUpdatedAfterSavedMacrosChanged] = (
            "Updated macro shortcuts after the saved macros list changed.",
            "Updated meowcro shortcuts after the saved macros list changed.");
        values[AppLanguageKeys.MacroHotkeySummaryFormat] = ("{0} shortcut{1} set. {2} turned on. {3} set to start and stop with the same key.", "{0} shortcut{1} set. {2} turned on. {3} set to start and stop with the same key.");
        values[AppLanguageKeys.MacroHotkeyPlaybackModeRunOnce] = ("Run once", "Run once");
        values[AppLanguageKeys.MacroHotkeyPlaybackModeToggleRepeat] = ("Start and stop with the same key", "Start and stop with the same key");
        values[AppLanguageKeys.MacroHotkeyOnState] = ("On", "On");
        values[AppLanguageKeys.MacroHotkeyOffState] = ("Off", "Off");
        values[AppLanguageKeys.MacroHotkeyActiveFallbackName] = ("the active saved macro", "the active saved macro");

        values[AppLanguageKeys.MacroEventItemPositionNotUsed] = ("Not used", "Not used");
        values[AppLanguageKeys.MacroEventItemDetailsMoveMouseFormat] = ("Move mouse to {0}, {1}", "Move mouse to {0}, {1}");
        values[AppLanguageKeys.MacroEventItemDetailsMouseDownFormat] = ("{0} down at {1}, {2}", "{0} down at {1}, {2}");
        values[AppLanguageKeys.MacroEventItemDetailsMouseUpFormat] = ("{0} up at {1}, {2}", "{0} up at {1}, {2}");
        values[AppLanguageKeys.MacroEventItemDetailsKeyDownFormat] = ("{0} down", "{0} down");
        values[AppLanguageKeys.MacroEventItemDetailsKeyUpFormat] = ("{0} up", "{0} up");
        values[AppLanguageKeys.MacroEventItemDetailsUnavailable] = ("Event details unavailable", "Event details unavailable");
        values[AppLanguageKeys.MacroEventItemVirtualKeyNone] = ("None", "None");
        values[AppLanguageKeys.MacroEventItemVirtualKeyOnlyFormat] = ("VK {0}", "VK {0}");
        values[AppLanguageKeys.MacroEventItemVirtualKeyNamedFormat] = ("{0} (VK {1})", "{0} (VK {1})");

    }
}
