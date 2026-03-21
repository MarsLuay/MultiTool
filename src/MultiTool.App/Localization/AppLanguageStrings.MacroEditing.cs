namespace MultiTool.App.Localization;

public static partial class AppLanguageStrings
{
    private static void AddMacroEditingValues(Dictionary<string, (string English, string CatSpeak)> values)
    {
        values[AppLanguageKeys.MacroEditorTitle] = ("Edit Macro", "Edit Meowcro");
        values[AppLanguageKeys.MacroEditorNameLabel] = ("Macro name", "Macro name");
        values[AppLanguageKeys.MacroEditorDescription] = (
            "Choose an event from the top list, then use the details panel below to change what it does. Offsets are in milliseconds from the start of the macro.",
            "Choose an event from the top list, then use the details panel below to change what it does. Offsets are in milliseconds from the start of the meowcro.");
        values[AppLanguageKeys.MacroEditorEventsHeader] = ("Events", "Events");
        values[AppLanguageKeys.MacroEditorPickEventHint] = ("Pick the event you want to edit.", "Pick the event you want to edit.");
        values[AppLanguageKeys.MacroEditorAddEventButton] = ("Add Event", "Add Event");
        values[AppLanguageKeys.MacroEditorRemoveSelectedButton] = ("Remove Selected", "Remove Selected");
        values[AppLanguageKeys.MacroEditorSortByOffsetButton] = ("Sort by Offset", "Sort by Offset");
        values[AppLanguageKeys.MacroEditorColumnNumber] = ("#", "#");
        values[AppLanguageKeys.MacroEditorColumnOffset] = ("Offset", "Offset");
        values[AppLanguageKeys.MacroEditorColumnAction] = ("Action", "Action");
        values[AppLanguageKeys.MacroEditorColumnDetails] = ("Details", "Details");
        values[AppLanguageKeys.MacroEditorSelectedEventHeader] = ("Selected Event", "Selected Event");
        values[AppLanguageKeys.MacroEditorSelectedEventNull] = ("Choose an event from the list first.", "Choose an event from the list first.");
        values[AppLanguageKeys.MacroEditorActionLabel] = ("Action", "Action");
        values[AppLanguageKeys.MacroEditorOffsetLabel] = ("Offset (ms)", "Offset (ms)");
        values[AppLanguageKeys.MacroEditorKeyCodeLabel] = ("Key Code", "Key Code");
        values[AppLanguageKeys.MacroEditorMouseButtonLabel] = ("Mouse Button", "Mouse Button");
        values[AppLanguageKeys.MacroEditorXPositionLabel] = ("X Position", "X Position");
        values[AppLanguageKeys.MacroEditorYPositionLabel] = ("Y Position", "Y Position");
        values[AppLanguageKeys.MacroEditorFieldHint] = (
            "Only the fields that matter for the selected action stay enabled.",
            "Only the fields that matter for the selected action stay enabled.");
        values[AppLanguageKeys.MacroEditorCancelButton] = ("Cancel", "Cancel");
        values[AppLanguageKeys.MacroEditorSaveButton] = ("Save", "Save");
        values[AppLanguageKeys.MacroEditorStatusInitial] = (
            "Pick an event on the left, adjust its details on the right, then press Save.",
            "Pick an event on the left, adjust its details on the right, then press Save.");
        values[AppLanguageKeys.MacroEditorStatusAdded] = (
            "Added a new event. You can edit its details on the right.",
            "Added a new event. You can edit its details on the right.");
        values[AppLanguageKeys.MacroEditorStatusRemoved] = ("Removed the selected event.", "Removed the selected event.");
        values[AppLanguageKeys.MacroEditorStatusSorted] = ("Sorted events by offset.", "Sorted events by offset.");
        values[AppLanguageKeys.MacroEditorErrorEnterName] = ("Enter a macro name before saving.", "Enter a macro name before saving.");
        values[AppLanguageKeys.MacroEditorSummaryEventsFormat] = ("{0} event(s)", "{0} event(s)");
        values[AppLanguageKeys.MacroEditorSelectedHintNone] = (
            "Select an event from the list to start editing it.",
            "Select an event from the list to start editing it.");
        values[AppLanguageKeys.MacroEditorSelectedHintKeyboard] = (
            "Keyboard events only use the Action, Offset, and Key Code fields.",
            "Keyboard events only use the Action, Offset, and Key Code fields.");
        values[AppLanguageKeys.MacroEditorSelectedHintMouseMove] = (
            "Mouse move events only use the Action, Offset, and X / Y position fields.",
            "Mouse move events only use the Action, Offset, and X / Y position fields.");
        values[AppLanguageKeys.MacroEditorSelectedHintMouseButton] = (
            "Mouse button events use the Action, Offset, Mouse Button, and X / Y position fields.",
            "Mouse button events use the Action, Offset, Mouse Button, and X / Y position fields.");

        values[AppLanguageKeys.MacroHotkeyAssignmentsTitle] = ("Macro Keyboard Shortcuts", "Macro Keyboard Shortcuts");
        values[AppLanguageKeys.MacroHotkeyAssignmentsHeading] = ("Macro Keyboard Shortcuts", "Macro Keyboard Shortcuts");
        values[AppLanguageKeys.MacroHotkeyAssignmentsDescription] = (
            "Choose a keyboard shortcut for each saved macro. 'Run once' plays it one time. 'Start/stop' keeps it running until you press the same key again.",
            "Choose a keyboard shortcut for each saved meowcro. 'Run once' plays it one time. 'Start/stop' keeps it running until you press the same key again.");
        values[AppLanguageKeys.MacroHotkeyAssignmentsEmpty] = (
            "No saved macros found yet. Save a macro on the Macro tab first.",
            "No saved meowcros found yet. Save a meowcro on the Macro tab first.");
        values[AppLanguageKeys.InstallerPackagePickerDescription] = (
            "Pick a batch of common apps and MultiTool will install them, update them, or scan the full list for available updates with winget in quiet mode.",
            "Pick a batch of common apps and MultiTool will install them, update them, or scan the full list for available updates with winget in quiet mode.");
        values[AppLanguageKeys.InstallerSearchLabel] = ("Search", "Search");
        values[AppLanguageKeys.MacroHotkeyAssignmentsActive] = ("Active", "Active");
        values[AppLanguageKeys.MacroHotkeyAssignmentsShortcutKey] = ("Shortcut key", "Shortcut key");
        values[AppLanguageKeys.MacroHotkeyAssignmentsBehavior] = ("What this key does", "What this key does");
        values[AppLanguageKeys.MacroHotkeyAssignmentsRemoveKey] = ("Remove key", "Remove key");
        values[AppLanguageKeys.MacroHotkeyAssignmentsClear] = ("Clear", "Clear");
        values[AppLanguageKeys.MacroHotkeyAssignmentsCancel] = ("Cancel", "Cancel");
        values[AppLanguageKeys.MacroHotkeyAssignmentsSave] = ("Save", "Save");
        values[AppLanguageKeys.MacroHotkeyAssignmentsCaptureTooltip] = (
            "Click here, then press the keyboard shortcut or key combination you want to use for this macro.",
            "Click here, then press the keyboard shortcut or key combo you want to use for this meowcro.");
        values[AppLanguageKeys.MacroHotkeyAssignmentsClickToAssign] = ("Click to assign", "Click to assign");
        values[AppLanguageKeys.MacroHotkeyAssignmentsStatusNoSaved] = (
            "No saved macros were found in the Macros folder.",
            "No saved meowcros were found in the Macros folder.");
        values[AppLanguageKeys.MacroHotkeyAssignmentsStatusPick] = (
            "Pick a keyboard shortcut for any saved macro. 'Run once' plays it one time. 'Start/stop' keeps it running until you press the same key again.",
            "Pick a keyboard shortcut for any saved meowcro. 'Run once' plays it one time. 'Start/stop' keeps it running until you press the same key again.");
        values[AppLanguageKeys.MacroHotkeyAssignmentsStatusAssignedFormat] = (
            "'{0}' will now run when you press {1}.",
            "'{0}' will now run when you press {1}.");
        values[AppLanguageKeys.MacroHotkeyAssignmentsStatusRemovedFormat] = (
            "Removed the keyboard shortcut for '{0}'.",
            "Removed the keyboard shortcut for '{0}'.");

        values[AppLanguageKeys.MacroNamePromptTitle] = ("Save Macro", "Save Meowcro");
        values[AppLanguageKeys.MacroNamePromptHeading] = ("Name this macro", "Name this meowcro");
        values[AppLanguageKeys.MacroNamePromptDescription] = (
            "Type the macro name below. Press Save and it will go straight into the Macros folder next to MultiTool.exe.",
            "Type the meowcro name below. Press Save and it will go straight into the Macros folder next to MultiTool.exe.");
        values[AppLanguageKeys.MacroNamePromptNameLabel] = ("Macro name", "Meowcro name");
        values[AppLanguageKeys.MacroNamePromptSaveToLabel] = ("Will save to", "Will save to");
        values[AppLanguageKeys.MacroNamePromptErrorEnterName] = ("Enter a macro name.", "Enter a meowcro name.");
        values[AppLanguageKeys.MacroNamePromptOverwriteHint] = (
            "Saving again with the same name will overwrite the existing macro.",
            "Saving again with the same name will overwrite the existing meowcro.");
        values[AppLanguageKeys.MacroNamePromptCancel] = ("Cancel", "Cancel");
        values[AppLanguageKeys.MacroNamePromptSave] = ("Save", "Save");
        values[AppLanguageKeys.MacroNamePromptNameTooltip] = ("Enter the macro name here.", "Enter the meowcro name here.");
        values[AppLanguageKeys.MacroNamePromptDefaultName] = ("New Macro", "New Meowcro");
        values[AppLanguageKeys.MacroNamePromptSavePreviewFormat] = ("{0}.acmacro.json", "{0}.acmacro.json");

    }
}
