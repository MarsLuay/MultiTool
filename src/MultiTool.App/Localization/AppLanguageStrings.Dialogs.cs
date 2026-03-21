namespace MultiTool.App.Localization;

public static partial class AppLanguageStrings
{
    private static void AddDialogValues(Dictionary<string, (string English, string CatSpeak)> values)
    {
        values[AppLanguageKeys.AboutWindowTitle] = ("About", "About");
        values[AppLanguageKeys.AboutSubtitle] = (
            "Desktop utility with clicker, screenshot, and macro tools.",
            "Desktop utility with clicker, screenshot, and meowcro tools.");
        values[AppLanguageKeys.AboutCloseButton] = ("Close", "Close");
        values[AppLanguageKeys.AboutVersionFormat] = ("Version {0}", "Version {0}");

        values[AppLanguageKeys.CoordinateCaptureInstruction] = (
            "Click anywhere to capture coordinates",
            "Click anywhere to capture paw-ordinates");
        values[AppLanguageKeys.CoordinateCaptureEscHint] = (
            "Press Esc to cancel.",
            "Press Esc to cancel.");
        values[AppLanguageKeys.CoordinateCapturePositionFormat] = ("X: {0}  Y: {1}", "X: {0}  Y: {1}");

        values[AppLanguageKeys.AreaSelectionInstruction] = (
            "Drag to select an area",
            "Drag to select an area");
        values[AppLanguageKeys.AreaSelectionEscHint] = (
            "Release to capture. Press Esc to cancel.",
            "Release to capture. Press Esc to cancel.");
        values[AppLanguageKeys.VideoSelectionInstruction] = (
            "Choose how to start video recording",
            "Choose how to start video recording");
        values[AppLanguageKeys.VideoSelectionHint] = (
            "Pick an option below. Press Esc to cancel.",
            "Pick an option below. Press Esc to cancel.");
        values[AppLanguageKeys.VideoSelectionChooseAreaButton] = (
            "Choose Area",
            "Choose Area");
        values[AppLanguageKeys.VideoSelectionCurrentScreenButton] = (
            "Full Screen",
            "Full Screen");
        values[AppLanguageKeys.VideoSelectionAllScreensButton] = (
            "All Screens",
            "All Screens");

        values[AppLanguageKeys.HotkeySettingsTitle] = ("Clicker Hotkey Settings", "Clicker Hotkey Settings");
        values[AppLanguageKeys.HotkeySettingsCaptureTooltip] = (
            "Click here, then press the new hotkey or key combination.",
            "Click here, then press the new pawkey or key combo.");
        values[AppLanguageKeys.HotkeySettingsToggleLabel] = ("Clicker Start/Stop Hotkey", "Clicker Start/Stop Hotkey");
        values[AppLanguageKeys.HotkeySettingsPinWindowLabel] = (
            "Pin Window Hotkey (Tools tab)",
            "Pin Window Hotkey (Cat Tools tab)");
        values[AppLanguageKeys.HotkeySettingsModifierVariantsLabel] = (
            "Allow Ctrl / Alt variants for the clicker hotkey",
            "Allow Ctrl / Alt variants for the clicker pawkey");
        values[AppLanguageKeys.HotkeySettingsWaitingAnyKey] = (
            "Waiting for a key or mouse button...",
            "Waiting for a key or mouse button...");
        values[AppLanguageKeys.HotkeySettingsWaitingKey] = (
            "Waiting for a key or key combination...",
            "Waiting for a key or key combo...");
        values[AppLanguageKeys.HotkeySettingsResetButton] = ("Reset", "Reset");
        values[AppLanguageKeys.HotkeySettingsCancelButton] = ("Cancel", "Cancel");
        values[AppLanguageKeys.HotkeySettingsSaveButton] = ("Save", "Save");

    }
}
