using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using AutoClicker.App.ViewModels;

namespace AutoClicker.App.Views;

public partial class MacroHotkeyAssignmentsWindow : Window
{
    private readonly MacroHotkeyAssignmentsWindowViewModel viewModel;
    private ToggleButton? activeCaptureButton;
    private bool allowCaptureFocusFromClick;

    public MacroHotkeyAssignmentsWindow(MacroHotkeyAssignmentsWindowViewModel viewModel)
    {
        this.viewModel = viewModel;
        InitializeComponent();
        DataContext = viewModel;
    }

    private void HotkeyButton_OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not ToggleButton button)
        {
            return;
        }

        ArmCaptureButton(button);
        allowCaptureFocusFromClick = true;
        e.Handled = true;

        if (!button.IsKeyboardFocused)
        {
            button.Focus();
            Keyboard.Focus(button);
        }
    }

    private void HotkeyButton_OnPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (sender is not ToggleButton { DataContext: MacroHotkeyAssignmentItemViewModel item })
        {
            return;
        }

        viewModel.CaptureHotkey(item, e.Key);
        ClearCaptureFocus();
        e.Handled = true;
    }

    private void HotkeyButton_OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (allowCaptureFocusFromClick)
        {
            allowCaptureFocusFromClick = false;
            return;
        }

        MacroHotkeyAssignmentsRootGrid.Focus();
        Keyboard.Focus(MacroHotkeyAssignmentsRootGrid);
    }

    private void HotkeyButton_OnLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is ToggleButton button)
        {
            DisarmCaptureButton(button);
        }
    }

    private void MacroHotkeyAssignmentsWindow_OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (activeCaptureButton is null)
        {
            return;
        }

        if (!IsDescendantOf(e.OriginalSource as DependencyObject, activeCaptureButton))
        {
            ClearCaptureFocus();
        }
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

    private void ClearCaptureFocus()
    {
        allowCaptureFocusFromClick = false;
        if (activeCaptureButton is not null)
        {
            DisarmCaptureButton(activeCaptureButton);
        }

        MacroHotkeyAssignmentsRootGrid.Focus();
        Keyboard.Focus(MacroHotkeyAssignmentsRootGrid);
    }

    private void ArmCaptureButton(ToggleButton button)
    {
        if (activeCaptureButton is not null && !ReferenceEquals(activeCaptureButton, button))
        {
            DisarmCaptureButton(activeCaptureButton);
        }

        activeCaptureButton = button;
        activeCaptureButton.IsChecked = true;
    }

    private void DisarmCaptureButton(ToggleButton button)
    {
        if (ReferenceEquals(activeCaptureButton, button))
        {
            activeCaptureButton = null;
        }

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
