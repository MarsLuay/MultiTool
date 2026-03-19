using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using MultiTool.App.Localization;
using MultiTool.App.ViewModels;
using MultiTool.Core.Enums;

namespace MultiTool.App.Views;

public partial class HotkeySettingsWindow : Window
{
    private readonly HotkeySettingsViewModel viewModel;
    private bool allowToggleHotkeyFocusFromClick;
    private bool allowPinWindowHotkeyFocusFromClick;

    public HotkeySettingsWindow(HotkeySettingsViewModel viewModel)
    {
        this.viewModel = viewModel;
        InitializeComponent();
        DataContext = viewModel;

        Title = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.HotkeySettingsTitle);
        ToggleHotkeyLabelTextBlock.Text = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.HotkeySettingsToggleLabel);
        PinWindowHotkeyLabelTextBlock.Text = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.HotkeySettingsPinWindowLabel);
        ModifierVariantsCheckBox.Content = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.HotkeySettingsModifierVariantsLabel);
        ToggleHotkeyWaitingTextBlock.Text = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.HotkeySettingsWaitingAnyKey);
        PinWindowHotkeyWaitingTextBlock.Text = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.HotkeySettingsWaitingKey);
        ResetButton.Content = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.HotkeySettingsResetButton);
        CancelButton.Content = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.HotkeySettingsCancelButton);
        SaveButton.Content = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.HotkeySettingsSaveButton);

        var captureTooltip = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.HotkeySettingsCaptureTooltip);
        ToggleHotkeyTextBox.ToolTip = captureTooltip;
        PinWindowHotkeyTextBox.ToolTip = captureTooltip;
    }

    private void ToggleHotkeyTextBox_OnPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        CaptureHotkey(HotkeyAction.Toggle, e);
    }

    private void ToggleHotkeyTextBox_OnPreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        ArmCaptureBox(ToggleHotkeyTextBox);
        allowToggleHotkeyFocusFromClick = true;
        e.Handled = true;

        var mouseButton = e.ChangedButton switch
        {
            System.Windows.Input.MouseButton.Right => Core.Enums.ClickMouseButton.Right,
            System.Windows.Input.MouseButton.Middle => Core.Enums.ClickMouseButton.Middle,
            System.Windows.Input.MouseButton.XButton1 => Core.Enums.ClickMouseButton.XButton1,
            System.Windows.Input.MouseButton.XButton2 => Core.Enums.ClickMouseButton.XButton2,
            _ => (Core.Enums.ClickMouseButton?)null,
        };

        if (mouseButton is null)
        {
            if (!ToggleHotkeyTextBox.IsKeyboardFocused)
            {
                ToggleHotkeyTextBox.Focus();
                Keyboard.Focus(ToggleHotkeyTextBox);
            }

            return;
        }

        viewModel.CaptureMouseHotkey(HotkeyAction.Toggle, mouseButton.Value);
        ClearCaptureFocus();
        e.Handled = true;
    }

    private void ToggleHotkeyTextBox_OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (allowToggleHotkeyFocusFromClick)
        {
            allowToggleHotkeyFocusFromClick = false;
            return;
        }

        HotkeySettingsRootGrid.Focus();
        Keyboard.Focus(HotkeySettingsRootGrid);
    }

    private void ToggleHotkeyTextBox_OnLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is ToggleButton button)
        {
            DisarmCaptureBox(button);
        }
    }

    private void PinWindowHotkeyTextBox_OnPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        CaptureHotkey(HotkeyAction.WindowPinToggle, e);
    }

    private void PinWindowHotkeyTextBox_OnPreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        ArmCaptureBox(PinWindowHotkeyTextBox);
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

        HotkeySettingsRootGrid.Focus();
        Keyboard.Focus(HotkeySettingsRootGrid);
    }

    private void SaveButton_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void CancelButton_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void CaptureHotkey(HotkeyAction action, System.Windows.Input.KeyEventArgs e)
    {
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (IsModifierKey(key))
        {
            e.Handled = true;
            return;
        }

        viewModel.CaptureHotkey(action, key);
        ClearCaptureFocus();
        e.Handled = true;
    }

    private void HotkeySettingsWindow_OnPreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (!IsDescendantOf(e.OriginalSource as DependencyObject, ToggleHotkeyTextBox)
            && !IsDescendantOf(e.OriginalSource as DependencyObject, PinWindowHotkeyTextBox))
        {
            ClearCaptureFocus();
        }
    }

    private void ClearCaptureFocus()
    {
        allowToggleHotkeyFocusFromClick = false;
        allowPinWindowHotkeyFocusFromClick = false;
        DisarmCaptureBox(ToggleHotkeyTextBox);
        DisarmCaptureBox(PinWindowHotkeyTextBox);

        if (ToggleHotkeyTextBox.IsKeyboardFocusWithin)
        {
            HotkeySettingsRootGrid.Focus();
            Keyboard.Focus(HotkeySettingsRootGrid);
            return;
        }

        if (PinWindowHotkeyTextBox.IsKeyboardFocusWithin)
        {
            HotkeySettingsRootGrid.Focus();
            Keyboard.Focus(HotkeySettingsRootGrid);
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

    private static bool IsModifierKey(Key key) =>
        key is Key.LeftShift
            or Key.RightShift
            or Key.LeftCtrl
            or Key.RightCtrl
            or Key.LeftAlt
            or Key.RightAlt
            or Key.LWin
            or Key.RWin;
}
