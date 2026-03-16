using System.ComponentModel;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using AutoClicker.App.Services;
using AutoClicker.App.ViewModels;
using AutoClicker.Core.Results;
using AutoClicker.Core.Services;

namespace AutoClicker.App.Views;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel viewModel;
    private readonly IHotkeyService hotkeyService;
    private readonly ITrayIconService trayIconService;
    private readonly IAutoClickerController autoClickerController;
    private bool allowScreenshotHotkeyFocusFromClick;
    private bool allowMacroHotkeyFocusFromClick;
    private bool allowMacroRecordHotkeyFocusFromClick;
    private bool isClosingAfterAutoSave;
    private bool isTransitioningToTray;

    public MainWindow(
        MainWindowViewModel viewModel,
        IHotkeyService hotkeyService,
        ITrayIconService trayIconService,
        IAutoClickerController autoClickerController)
    {
        this.viewModel = viewModel;
        this.hotkeyService = hotkeyService;
        this.trayIconService = trayIconService;
        this.autoClickerController = autoClickerController;

        InitializeComponent();
        DataContext = viewModel;

        Loaded += MainWindow_OnLoaded;
        StateChanged += MainWindow_OnStateChanged;
        Deactivated += MainWindow_OnDeactivated;
        Closing += MainWindow_OnClosing;

        viewModel.HotkeysChanged += ViewModel_HotkeysChanged;
        hotkeyService.HotkeyPressed += HotkeyService_HotkeyPressed;
        autoClickerController.RunningStateChanged += AutoClickerController_RunningStateChanged;

        trayIconService.ShowRequested += (_, _) => Dispatcher.Invoke(ShowFromTray);
        trayIconService.HideRequested += (_, _) => Dispatcher.Invoke(HideToTray);
        trayIconService.ExitRequested += (_, _) => Dispatcher.Invoke(Close);
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        EnsureHotkeyServiceAttached();
    }

    private async void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
        trayIconService.Initialize();
        await viewModel.InitializeAsync();
        RegisterHotkeys();
        trayIconService.SetRunningState(viewModel.IsRunning);

        if (viewModel.ShouldAutoHideOnStartup)
        {
            HideToTray();
            viewModel.SetStatus("MultiTool started hidden in the tray.");
        }
    }

    private void MainWindow_OnStateChanged(object? sender, EventArgs e)
    {
        if (!IsLoaded || isTransitioningToTray || WindowState != WindowState.Minimized)
        {
            return;
        }

        HideToTray();
        viewModel.SetStatus("MultiTool was minimized to the tray.");
    }

    private void TitleBar_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            ToggleMaximizeRestore();
            return;
        }

        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void MinimizeButton_OnClick(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void MaximizeRestoreButton_OnClick(object sender, RoutedEventArgs e)
    {
        ToggleMaximizeRestore();
    }

    private void CloseButton_OnClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void RegisterHotkeys()
    {
        if (!EnsureHotkeyServiceAttached())
        {
            viewModel.SetStatus("Hotkeys will register once the main window is ready.");
            return;
        }

        var results = hotkeyService.RegisterHotkeys(
            viewModel.CurrentHotkeys,
            viewModel.CurrentScreenshotSettings,
            viewModel.CurrentMacroSettings);
        var failures = results.Where(result => !result.Succeeded).Select(result => result.Describe()).ToArray();
        if (failures.Length > 0)
        {
            viewModel.SetStatus(string.Join(" ", failures));
        }
    }

    private bool EnsureHotkeyServiceAttached()
    {
        if (hotkeyService.IsAttached)
        {
            return true;
        }

        var handle = new WindowInteropHelper(this).EnsureHandle();
        if (handle == nint.Zero)
        {
            AppLog.Info("Skipped hotkey registration because the main window handle is not available yet.");
            return false;
        }

        hotkeyService.Attach(handle);
        return true;
    }

    private async void HotkeyService_HotkeyPressed(object? sender, HotkeyPressedEventArgs e)
    {
        var operation = Dispatcher.InvokeAsync(() => viewModel.HandleHotkeyAsync(e.Action, e.Payload));
        await await operation;
    }

    private void ViewModel_HotkeysChanged(object? sender, EventArgs e)
    {
        RegisterHotkeys();
    }

    private void AutoClickerController_RunningStateChanged(object? sender, RunningStateChangedEventArgs e)
    {
        Dispatcher.Invoke(
            () =>
            {
                viewModel.UpdateRunningState(e.IsRunning);
                trayIconService.SetRunningState(e.IsRunning);
            });
    }

    private async void MainWindow_OnDeactivated(object? sender, EventArgs e)
    {
        if (isClosingAfterAutoSave || !IsLoaded)
        {
            return;
        }

        await viewModel.AutoSaveAsync();
    }

    private async void MainWindow_OnClosing(object? sender, CancelEventArgs e)
    {
        if (isClosingAfterAutoSave)
        {
            hotkeyService.Dispose();
            trayIconService.Dispose();
            return;
        }

        e.Cancel = true;
        await viewModel.AutoSaveAsync();
        isClosingAfterAutoSave = true;
        Close();
    }

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
            System.Windows.Input.MouseButton.Middle => AutoClicker.Core.Enums.ClickMouseButton.Middle,
            System.Windows.Input.MouseButton.Right => AutoClicker.Core.Enums.ClickMouseButton.Right,
            System.Windows.Input.MouseButton.XButton1 => AutoClicker.Core.Enums.ClickMouseButton.XButton1,
            System.Windows.Input.MouseButton.XButton2 => AutoClicker.Core.Enums.ClickMouseButton.XButton2,
            _ => (AutoClicker.Core.Enums.ClickMouseButton?)null,
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
        viewModel.CaptureScreenshotHotkey(key);
        ClearCaptureFocus();
        e.Handled = true;
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
        if (!IsDescendantOf(e.OriginalSource as DependencyObject, ScreenshotHotkeyTextBox)
            && !IsDescendantOf(e.OriginalSource as DependencyObject, MacroHotkeyTextBox)
            && !IsDescendantOf(e.OriginalSource as DependencyObject, MacroRecordHotkeyTextBox))
        {
            DisarmCaptureBoxes();
            ClearCaptureFocus();
        }
    }

    private void MainWindow_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if ((Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control
            || !viewModel.IsCtrlWheelResizeEnabled
            || WindowState != WindowState.Normal)
        {
            return;
        }

        const double zoomStep = 1.08;
        const double minimumGrowthThreshold = 0.5;
        var stepCount = e.Delta / (double)Mouse.MouseWheelDeltaForOneLine;
        if (Math.Abs(stepCount) < double.Epsilon)
        {
            return;
        }

        var scale = Math.Pow(zoomStep, stepCount);
        var workArea = SystemParameters.WorkArea;
        var currentWidth = ActualWidth > 0 ? ActualWidth : Width;
        var currentHeight = ActualHeight > 0 ? ActualHeight : Height;
        var targetWidth = Clamp(currentWidth * scale, MinWidth, workArea.Width);
        var targetHeight = Clamp(currentHeight * scale, MinHeight, workArea.Height);

        if (Math.Abs(targetWidth - currentWidth) < minimumGrowthThreshold
            && Math.Abs(targetHeight - currentHeight) < minimumGrowthThreshold)
        {
            e.Handled = true;
            return;
        }

        var centerX = Left + (currentWidth / 2d);
        var centerY = Top + (currentHeight / 2d);

        Width = targetWidth;
        Height = targetHeight;
        Left = Clamp(centerX - (targetWidth / 2d), workArea.Left, workArea.Right - targetWidth);
        Top = Clamp(centerY - (targetHeight / 2d), workArea.Top, workArea.Bottom - targetHeight);
        e.Handled = true;
    }

    private void CaptureButton_OnLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is ToggleButton button)
        {
            DisarmCaptureBox(button);
        }
    }

    private void HideToTray()
    {
        isTransitioningToTray = true;

        try
        {
            if (WindowState == WindowState.Minimized)
            {
                WindowState = WindowState.Normal;
            }

            Hide();
        }
        finally
        {
            isTransitioningToTray = false;
        }
    }

    private void ShowFromTray()
    {
        isTransitioningToTray = true;

        try
        {
            if (!IsVisible)
            {
                Show();
            }

            if (WindowState != WindowState.Normal)
            {
                WindowState = WindowState.Normal;
            }

            Show();
            BringIntoView();
            Activate();
            Focus();
        }
        finally
        {
            isTransitioningToTray = false;
        }
    }

    private void ToggleMaximizeRestore()
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private void ClearCaptureFocus()
    {
        allowScreenshotHotkeyFocusFromClick = false;
        allowMacroHotkeyFocusFromClick = false;
        allowMacroRecordHotkeyFocusFromClick = false;

        if (ScreenshotHotkeyTextBox.IsKeyboardFocusWithin
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
        DisarmCaptureBox(ScreenshotHotkeyTextBox);
        DisarmCaptureBox(MacroHotkeyTextBox);
        DisarmCaptureBox(MacroRecordHotkeyTextBox);
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

    private static double Clamp(double value, double minimum, double maximum) =>
        Math.Min(maximum, Math.Max(minimum, value));
}
