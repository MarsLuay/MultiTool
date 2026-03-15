using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using AutoClicker.App.ViewModels;
using AutoClicker.Core.Enums;

namespace AutoClicker.App.Views;

public partial class HotkeySettingsWindow : Window
{
    private readonly HotkeySettingsViewModel viewModel;
    private bool allowToggleHotkeyFocusFromClick;

    public HotkeySettingsWindow(HotkeySettingsViewModel viewModel)
    {
        this.viewModel = viewModel;
        InitializeComponent();
        DataContext = viewModel;
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
        DisarmCaptureBox(ToggleHotkeyTextBox);
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
        viewModel.CaptureHotkey(action, e.Key);
        ClearCaptureFocus();
        e.Handled = true;
    }

    private void HotkeySettingsWindow_OnPreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (!IsDescendantOf(e.OriginalSource as DependencyObject, ToggleHotkeyTextBox))
        {
            ClearCaptureFocus();
        }
    }

    private void ClearCaptureFocus()
    {
        allowToggleHotkeyFocusFromClick = false;
        DisarmCaptureBox(ToggleHotkeyTextBox);

        if (ToggleHotkeyTextBox.IsKeyboardFocusWithin)
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

            source = VisualTreeHelper.GetParent(source);
        }

        return false;
    }
}
