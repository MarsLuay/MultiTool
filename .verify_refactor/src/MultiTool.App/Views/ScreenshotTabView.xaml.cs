using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using MultiTool.App.ViewModels;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using UserControl = System.Windows.Controls.UserControl;

namespace MultiTool.App.Views;

public partial class ScreenshotTabView : UserControl, IMainWindowCaptureHost
{
    private bool allowScreenshotHotkeyFocusFromClick;

    public ScreenshotTabView()
    {
        InitializeComponent();
    }

    private MainWindowViewModel? Shell => (DataContext as ScreenshotTabViewModel)?.Shell;

    public bool HasCaptureInteraction =>
        ScreenshotHotkeyTextBox.IsChecked == true
        || ScreenshotHotkeyTextBox.IsKeyboardFocusWithin;

    public bool ContainsCaptureElement(DependencyObject? source) =>
        HotkeyCaptureHelpers.IsDescendantOf(source, ScreenshotHotkeyTextBox);

    public bool ShouldIgnoreZoomShortcut(DependencyObject? source) =>
        HotkeyCaptureHelpers.IsDescendantOf(source, ScreenshotHotkeyTextBox);

    public void ClearCaptureState(UIElement fallbackFocusTarget)
    {
        allowScreenshotHotkeyFocusFromClick = false;
        var hadKeyboardFocus = ScreenshotHotkeyTextBox.IsKeyboardFocusWithin;
        HotkeyCaptureHelpers.DisarmCaptureBox(ScreenshotHotkeyTextBox);

        if (hadKeyboardFocus)
        {
            HotkeyCaptureHelpers.FocusFallback(fallbackFocusTarget);
        }
    }

    private void ScreenshotHotkeyTextBox_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (HotkeyCaptureHelpers.IsModifierKey(key))
        {
            e.Handled = true;
            return;
        }

        Shell?.CaptureScreenshotHotkey(key);
        ClearCaptureState(HotkeyCaptureHelpers.ResolveFallbackFocusTarget(this));
        e.Handled = true;
    }

    private void ScreenshotHotkeyTextBox_OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        HotkeyCaptureHelpers.ArmCaptureBox(ScreenshotHotkeyTextBox);
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

        HotkeyCaptureHelpers.FocusFallback(HotkeyCaptureHelpers.ResolveFallbackFocusTarget(this));
    }

    private void CaptureButton_OnLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is ToggleButton button)
        {
            HotkeyCaptureHelpers.DisarmCaptureBox(button);
        }
    }

    private void LatestVideoPlayer_OnMediaOpened(object sender, RoutedEventArgs e)
    {
        if (sender is not MediaElement mediaElement)
        {
            return;
        }

        mediaElement.Position = TimeSpan.Zero;
        mediaElement.Play();
    }

    private void LatestVideoPlayer_OnMediaEnded(object sender, RoutedEventArgs e)
    {
        if (sender is not MediaElement mediaElement)
        {
            return;
        }

        mediaElement.Position = TimeSpan.Zero;
        mediaElement.Play();
    }
}
