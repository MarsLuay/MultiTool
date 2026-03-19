using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using System.Windows.Threading;
using MultiTool.App.Localization;
using MultiTool.App.Services;
using MultiTool.App.ViewModels;
using MultiTool.Core.Results;
using MultiTool.Core.Services;

namespace MultiTool.App.Views;

public partial class MainWindow : Window
{
    private void CustomKeyTextBox_OnPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        var key = e.Key == System.Windows.Input.Key.System ? e.SystemKey : e.Key;
        viewModel.CaptureCustomKey(key);
        e.Handled = true;
    }

    private void CustomInputTextBox_OnPreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        var capturedMouseButton = e.ChangedButton switch
        {
            System.Windows.Input.MouseButton.Middle => MultiTool.Core.Enums.ClickMouseButton.Middle,
            System.Windows.Input.MouseButton.Right => MultiTool.Core.Enums.ClickMouseButton.Right,
            System.Windows.Input.MouseButton.XButton1 => MultiTool.Core.Enums.ClickMouseButton.XButton1,
            System.Windows.Input.MouseButton.XButton2 => MultiTool.Core.Enums.ClickMouseButton.XButton2,
            _ => (MultiTool.Core.Enums.ClickMouseButton?)null,
        };

        if (capturedMouseButton is null)
        {
            return;
        }

        viewModel.CaptureCustomMouseButton(capturedMouseButton.Value);
        e.Handled = true;
    }

    private void ScreenshotHotkeyTextBox_OnPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        var key = e.Key == System.Windows.Input.Key.System ? e.SystemKey : e.Key;
        if (IsModifierKey(key))
        {
            e.Handled = true;
            return;
        }

        viewModel.CaptureScreenshotHotkey(key);
        ClearCaptureFocus();
        e.Handled = true;
    }

    private void ClickerHotkeyTextBox_OnPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        var key = e.Key == System.Windows.Input.Key.System ? e.SystemKey : e.Key;
        if (IsModifierKey(key))
        {
            e.Handled = true;
            return;
        }

        viewModel.CaptureClickerHotkey(key);
        ClearCaptureFocus();
        e.Handled = true;
    }

    private void ClickerHotkeyTextBox_OnPreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        ArmCaptureBox(ClickerHotkeyTextBox);
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

        MainRootGrid.Focus();
        Keyboard.Focus(MainRootGrid);
    }

    private void ScreenshotHotkeyTextBox_OnPreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        ArmCaptureBox(ScreenshotHotkeyTextBox);
        allowScreenshotHotkeyFocusFromClick = true;
        e.Handled = true;

        if (!ScreenshotHotkeyTextBox.IsKeyboardFocused)
        {
            ScreenshotHotkeyTextBox.Focus();
            Keyboard.Focus(ScreenshotHotkeyTextBox);
        }
    }

    private void ScreenshotHotkeyTextBox_OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (allowScreenshotHotkeyFocusFromClick)
        {
            allowScreenshotHotkeyFocusFromClick = false;
            return;
        }

        MainRootGrid.Focus();
        Keyboard.Focus(MainRootGrid);
    }

    private void MacroHotkeyTextBox_OnPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        var key = e.Key == System.Windows.Input.Key.System ? e.SystemKey : e.Key;
        if (IsModifierKey(key))
        {
            e.Handled = true;
            return;
        }

        viewModel.CaptureMacroHotkey(key);
        ClearCaptureFocus();
        e.Handled = true;
    }

    private void MacroHotkeyTextBox_OnPreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        ArmCaptureBox(MacroHotkeyTextBox);
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

        MainRootGrid.Focus();
        Keyboard.Focus(MainRootGrid);
    }

    private void MacroRecordHotkeyTextBox_OnPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        var key = e.Key == System.Windows.Input.Key.System ? e.SystemKey : e.Key;
        if (IsModifierKey(key))
        {
            e.Handled = true;
            return;
        }

        viewModel.CaptureMacroRecordHotkey(key);
        ClearCaptureFocus();
        e.Handled = true;
    }

    private void MacroRecordHotkeyTextBox_OnPreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        ArmCaptureBox(MacroRecordHotkeyTextBox);
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

        MainRootGrid.Focus();
        Keyboard.Focus(MainRootGrid);
    }

    private void MainWindow_OnPreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        PauseAutoClickerForInAppInteraction();

        if (!IsDescendantOf(e.OriginalSource as DependencyObject, ClickerHotkeyTextBox)
            && !IsDescendantOf(e.OriginalSource as DependencyObject, ScreenshotHotkeyTextBox)
            && !IsDescendantOf(e.OriginalSource as DependencyObject, MacroHotkeyTextBox)
            && !IsDescendantOf(e.OriginalSource as DependencyObject, MacroRecordHotkeyTextBox))
        {
            DisarmCaptureBoxes();
            ClearCaptureFocus();
        }
    }

    private void MainWindow_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        PauseAutoClickerForInAppInteraction();

        if ((Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control)
        {
            return;
        }

        if (!viewModel.IsCtrlWheelResizeEnabled)
        {
            return;
        }

        var stepCount = e.Delta / (double)Mouse.MouseWheelDeltaForOneLine;
        if (Math.Abs(stepCount) < double.Epsilon)
        {
            return;
        }

        ApplyMainContentScaleStep(stepCount);
        e.Handled = true;
    }

    private void MainWindow_OnPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if ((Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control
            || ShouldIgnoreZoomShortcut(e.OriginalSource as DependencyObject))
        {
            return;
        }

        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        var stepCount = key switch
        {
            Key.OemPlus => 1d,
            Key.Add => 1d,
            Key.OemMinus => -1d,
            Key.Subtract => -1d,
            _ => 0d,
        };

        if (Math.Abs(stepCount) < double.Epsilon)
        {
            return;
        }

        ApplyMainContentScaleStep(stepCount);
        e.Handled = true;
    }

    private bool ShouldIgnoreZoomShortcut(DependencyObject? originalSource) =>
        IsDescendantOf(originalSource, CustomKeyTextBox)
        || IsDescendantOf(originalSource, ClickerHotkeyTextBox)
        || IsDescendantOf(originalSource, ScreenshotHotkeyTextBox)
        || IsDescendantOf(originalSource, MacroHotkeyTextBox)
        || IsDescendantOf(originalSource, MacroRecordHotkeyTextBox);

    private void ApplyMainContentScaleStep(double stepCount)
    {
        var currentScale = MainContentScaleTransform.ScaleX;
        var requestedScale = currentScale * Math.Pow(UiScaleStep, stepCount);
        var targetScale = Clamp(requestedScale, MinimumUiScale, MaximumUiScale);
        if (Math.Abs(targetScale - currentScale) < 0.0001d)
        {
            return;
        }

        MainContentScaleTransform.ScaleX = targetScale;
        MainContentScaleTransform.ScaleY = targetScale;
    }

    private void PauseAutoClickerForInAppInteraction()
    {
        if (!viewModel.IsRunning)
        {
            return;
        }

        autoClickerController.SuspendFor(InAppInteractionClickerPause);
    }

    private void CaptureButton_OnLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is ToggleButton button)
        {
            DisarmCaptureBox(button);
        }
    }

    private void LatestVideoPlayer_OnMediaOpened(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.MediaElement mediaElement)
        {
            return;
        }

        mediaElement.Position = TimeSpan.Zero;
        mediaElement.Play();
    }

    private void LatestVideoPlayer_OnMediaEnded(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.MediaElement mediaElement)
        {
            return;
        }

        mediaElement.Position = TimeSpan.Zero;
        mediaElement.Play();
    }


    private void ClearCaptureFocus()
    {
        allowClickerHotkeyFocusFromClick = false;
        allowScreenshotHotkeyFocusFromClick = false;
        allowMacroHotkeyFocusFromClick = false;
        allowMacroRecordHotkeyFocusFromClick = false;

        if (ClickerHotkeyTextBox.IsKeyboardFocusWithin
            || ScreenshotHotkeyTextBox.IsKeyboardFocusWithin
            || MacroHotkeyTextBox.IsKeyboardFocusWithin
            || MacroRecordHotkeyTextBox.IsKeyboardFocusWithin)
        {
            MainRootGrid.Focus();
            Keyboard.Focus(MainRootGrid);
        }
    }

    private static void ArmCaptureBox(ToggleButton button)
    {
        button.IsChecked = true;
    }

    private static void DisarmCaptureBox(ToggleButton button)
    {
        button.IsChecked = false;
    }

    private void DisarmCaptureBoxes()
    {
        DisarmCaptureBox(ClickerHotkeyTextBox);
        DisarmCaptureBox(ScreenshotHotkeyTextBox);
        DisarmCaptureBox(MacroHotkeyTextBox);
        DisarmCaptureBox(MacroRecordHotkeyTextBox);
    }

    private bool ShouldSuppressHotkeyExecution()
    {
        if (viewModel.ShouldSuppressGlobalHotkeys)
        {
            return true;
        }

        return ScreenshotHotkeyTextBox.IsChecked == true
             || ClickerHotkeyTextBox.IsChecked == true
               || MacroHotkeyTextBox.IsChecked == true
               || MacroRecordHotkeyTextBox.IsChecked == true
             || ClickerHotkeyTextBox.IsKeyboardFocusWithin
               || ScreenshotHotkeyTextBox.IsKeyboardFocusWithin
               || MacroHotkeyTextBox.IsKeyboardFocusWithin
               || MacroRecordHotkeyTextBox.IsKeyboardFocusWithin;
    }

    private static bool IsDescendantOf(DependencyObject? source, DependencyObject target)
    {
        while (source is not null)
        {
            if (ReferenceEquals(source, target))
            {
                return true;
            }

            source = source switch
            {
                Visual or Visual3D => VisualTreeHelper.GetParent(source),
                _ => LogicalTreeHelper.GetParent(source),
            };
        }

        return false;
    }

    private static double Clamp(double value, double minimum, double maximum) =>
        Math.Min(maximum, Math.Max(minimum, value));

    private static bool IsModifierKey(System.Windows.Input.Key key) =>
        key is System.Windows.Input.Key.LeftShift
            or System.Windows.Input.Key.RightShift
            or System.Windows.Input.Key.LeftCtrl
            or System.Windows.Input.Key.RightCtrl
            or System.Windows.Input.Key.LeftAlt
            or System.Windows.Input.Key.RightAlt;
}
