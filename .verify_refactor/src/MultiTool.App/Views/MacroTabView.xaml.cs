using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using MultiTool.App.ViewModels;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using UserControl = System.Windows.Controls.UserControl;

namespace MultiTool.App.Views;

public partial class MacroTabView : UserControl, IMainWindowCaptureHost
{
    private bool allowMacroHotkeyFocusFromClick;
    private bool allowMacroRecordHotkeyFocusFromClick;

    public MacroTabView()
    {
        InitializeComponent();
    }

    private MainWindowViewModel? Shell => (DataContext as MacroTabViewModel)?.Shell;

    public bool HasCaptureInteraction =>
        MacroHotkeyTextBox.IsChecked == true
        || MacroRecordHotkeyTextBox.IsChecked == true
        || MacroHotkeyTextBox.IsKeyboardFocusWithin
        || MacroRecordHotkeyTextBox.IsKeyboardFocusWithin;

    public bool ContainsCaptureElement(DependencyObject? source) =>
        HotkeyCaptureHelpers.IsDescendantOf(source, MacroHotkeyTextBox)
        || HotkeyCaptureHelpers.IsDescendantOf(source, MacroRecordHotkeyTextBox);

    public bool ShouldIgnoreZoomShortcut(DependencyObject? source) =>
        HotkeyCaptureHelpers.IsDescendantOf(source, MacroHotkeyTextBox)
        || HotkeyCaptureHelpers.IsDescendantOf(source, MacroRecordHotkeyTextBox);

    public void ClearCaptureState(UIElement fallbackFocusTarget)
    {
        allowMacroHotkeyFocusFromClick = false;
        allowMacroRecordHotkeyFocusFromClick = false;
        var hadKeyboardFocus = MacroHotkeyTextBox.IsKeyboardFocusWithin
            || MacroRecordHotkeyTextBox.IsKeyboardFocusWithin;

        HotkeyCaptureHelpers.DisarmCaptureBox(MacroHotkeyTextBox);
        HotkeyCaptureHelpers.DisarmCaptureBox(MacroRecordHotkeyTextBox);

        if (hadKeyboardFocus)
        {
            HotkeyCaptureHelpers.FocusFallback(fallbackFocusTarget);
        }
    }

    private void MacroHotkeyTextBox_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (HotkeyCaptureHelpers.IsModifierKey(key))
        {
            e.Handled = true;
            return;
        }

        Shell?.CaptureMacroHotkey(key);
        ClearCaptureState(HotkeyCaptureHelpers.ResolveFallbackFocusTarget(this));
        e.Handled = true;
    }

    private void MacroHotkeyTextBox_OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        HotkeyCaptureHelpers.ArmCaptureBox(MacroHotkeyTextBox);
        allowMacroHotkeyFocusFromClick = true;
        e.Handled = true;

        if (!MacroHotkeyTextBox.IsKeyboardFocused)
        {
            MacroHotkeyTextBox.Focus();
            Keyboard.Focus(MacroHotkeyTextBox);
        }
    }

    private void MacroHotkeyTextBox_OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (allowMacroHotkeyFocusFromClick)
        {
            allowMacroHotkeyFocusFromClick = false;
            return;
        }

        HotkeyCaptureHelpers.FocusFallback(HotkeyCaptureHelpers.ResolveFallbackFocusTarget(this));
    }

    private void MacroRecordHotkeyTextBox_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (HotkeyCaptureHelpers.IsModifierKey(key))
        {
            e.Handled = true;
            return;
        }

        Shell?.CaptureMacroRecordHotkey(key);
        ClearCaptureState(HotkeyCaptureHelpers.ResolveFallbackFocusTarget(this));
        e.Handled = true;
    }

    private void MacroRecordHotkeyTextBox_OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        HotkeyCaptureHelpers.ArmCaptureBox(MacroRecordHotkeyTextBox);
        allowMacroRecordHotkeyFocusFromClick = true;
        e.Handled = true;

        if (!MacroRecordHotkeyTextBox.IsKeyboardFocused)
        {
            MacroRecordHotkeyTextBox.Focus();
            Keyboard.Focus(MacroRecordHotkeyTextBox);
        }
    }

    private void MacroRecordHotkeyTextBox_OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (allowMacroRecordHotkeyFocusFromClick)
        {
            allowMacroRecordHotkeyFocusFromClick = false;
            return;
        }

        HotkeyCaptureHelpers.FocusFallback(HotkeyCaptureHelpers.ResolveFallbackFocusTarget(this));
    }

    private void CaptureButton_OnLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is ToggleButton button)
        {
            HotkeyCaptureHelpers.DisarmCaptureBox(button);
        }
    }
}
