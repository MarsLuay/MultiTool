using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using MultiTool.App.ViewModels;
using MultiTool.Core.Enums;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using UserControl = System.Windows.Controls.UserControl;

namespace MultiTool.App.Views;

public partial class ClickerTabView : UserControl, IMainWindowCaptureHost
{
    private bool allowClickerHotkeyFocusFromClick;

    public ClickerTabView()
    {
        InitializeComponent();
    }

    private MainWindowViewModel? Shell => (DataContext as ClickerTabViewModel)?.Shell;

    public bool HasCaptureInteraction =>
        ClickerHotkeyTextBox.IsChecked == true
        || ClickerHotkeyTextBox.IsKeyboardFocusWithin;

    public bool ContainsCaptureElement(DependencyObject? source) =>
        HotkeyCaptureHelpers.IsDescendantOf(source, ClickerHotkeyTextBox);

    public bool ShouldIgnoreZoomShortcut(DependencyObject? source) =>
        HotkeyCaptureHelpers.IsDescendantOf(source, CustomKeyTextBox)
        || HotkeyCaptureHelpers.IsDescendantOf(source, ClickerHotkeyTextBox);

    public void ClearCaptureState(UIElement fallbackFocusTarget)
    {
        allowClickerHotkeyFocusFromClick = false;
        var hadKeyboardFocus = ClickerHotkeyTextBox.IsKeyboardFocusWithin;
        HotkeyCaptureHelpers.DisarmCaptureBox(ClickerHotkeyTextBox);

        if (hadKeyboardFocus)
        {
            HotkeyCaptureHelpers.FocusFallback(fallbackFocusTarget);
        }
    }

    private void CustomKeyTextBox_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        Shell?.CaptureCustomKey(key);
        e.Handled = true;
    }

    private void CustomInputTextBox_OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        var capturedMouseButton = e.ChangedButton switch
        {
            MouseButton.Middle => ClickMouseButton.Middle,
            MouseButton.Right => ClickMouseButton.Right,
            MouseButton.XButton1 => ClickMouseButton.XButton1,
            MouseButton.XButton2 => ClickMouseButton.XButton2,
            _ => (ClickMouseButton?)null,
        };

        if (capturedMouseButton is null)
        {
            return;
        }

        Shell?.CaptureCustomMouseButton(capturedMouseButton.Value);
        e.Handled = true;
    }

    private void ClickerHotkeyTextBox_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (HotkeyCaptureHelpers.IsModifierKey(key))
        {
            e.Handled = true;
            return;
        }

        Shell?.CaptureClickerHotkey(key, Keyboard.Modifiers);
        ClearCaptureState(HotkeyCaptureHelpers.ResolveFallbackFocusTarget(this));
        e.Handled = true;
    }

    private void ClickerHotkeyTextBox_OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        HotkeyCaptureHelpers.ArmCaptureBox(ClickerHotkeyTextBox);
        allowClickerHotkeyFocusFromClick = true;
        e.Handled = true;

        if (!ClickerHotkeyTextBox.IsKeyboardFocused)
        {
            ClickerHotkeyTextBox.Focus();
            Keyboard.Focus(ClickerHotkeyTextBox);
        }
    }

    private void ClickerHotkeyTextBox_OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (allowClickerHotkeyFocusFromClick)
        {
            allowClickerHotkeyFocusFromClick = false;
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
