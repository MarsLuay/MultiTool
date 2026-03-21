using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using MultiTool.App.ViewModels;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using UserControl = System.Windows.Controls.UserControl;

namespace MultiTool.App.Views;

public partial class ToolsTabView : UserControl, IMainWindowCaptureHost
{
    private bool allowPinWindowHotkeyFocusFromClick;

    public ToolsTabView()
    {
        InitializeComponent();
    }

    private MainWindowViewModel? Shell => (DataContext as ToolsTabViewModel)?.Shell;

    public bool HasCaptureInteraction =>
        PinWindowHotkeyTextBox.IsChecked == true
        || PinWindowHotkeyTextBox.IsKeyboardFocusWithin;

    public bool ContainsCaptureElement(DependencyObject? source) =>
        HotkeyCaptureHelpers.IsDescendantOf(source, PinWindowHotkeyTextBox);

    public bool ShouldIgnoreZoomShortcut(DependencyObject? source) =>
        HotkeyCaptureHelpers.IsDescendantOf(source, PinWindowHotkeyTextBox);

    public void ClearCaptureState(UIElement fallbackFocusTarget)
    {
        allowPinWindowHotkeyFocusFromClick = false;
        var hadKeyboardFocus = PinWindowHotkeyTextBox.IsKeyboardFocusWithin;
        HotkeyCaptureHelpers.DisarmCaptureBox(PinWindowHotkeyTextBox);

        if (hadKeyboardFocus)
        {
            HotkeyCaptureHelpers.FocusFallback(fallbackFocusTarget);
        }
    }

    private void PinWindowHotkeyTextBox_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (HotkeyCaptureHelpers.IsModifierKey(key))
        {
            e.Handled = true;
            return;
        }

        Shell?.CapturePinWindowHotkey(key, Keyboard.Modifiers);
        ClearCaptureState(HotkeyCaptureHelpers.ResolveFallbackFocusTarget(this));
        e.Handled = true;
    }

    private void PinWindowHotkeyTextBox_OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        HotkeyCaptureHelpers.ArmCaptureBox(PinWindowHotkeyTextBox);
        allowPinWindowHotkeyFocusFromClick = true;
        e.Handled = true;

        if (!PinWindowHotkeyTextBox.IsKeyboardFocused)
        {
            PinWindowHotkeyTextBox.Focus();
            Keyboard.Focus(PinWindowHotkeyTextBox);
        }
    }

    private void PinWindowHotkeyTextBox_OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (allowPinWindowHotkeyFocusFromClick)
        {
            allowPinWindowHotkeyFocusFromClick = false;
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
