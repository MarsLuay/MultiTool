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
using AutoClicker.App.Localization;
using AutoClicker.App.Services;
using AutoClicker.App.ViewModels;
using AutoClicker.Core.Results;
using AutoClicker.Core.Services;

namespace AutoClicker.App.Views;

public partial class MainWindow : Window
{
    private const double MinimumUiScale = 0.8;
    private const double MaximumUiScale = 1.8;
    private const double UiScaleStep = 1.08;
    private const double DefaultTitleBarHeight = 34d;
    private static readonly System.Windows.Media.Brush[] ConfettiBrushes =
    [
        System.Windows.Media.Brushes.HotPink,
        System.Windows.Media.Brushes.Orange,
        System.Windows.Media.Brushes.Gold,
        System.Windows.Media.Brushes.DeepSkyBlue,
        System.Windows.Media.Brushes.LimeGreen,
        System.Windows.Media.Brushes.Tomato,
    ];
    private static readonly TimeSpan ConfettiDuration = TimeSpan.FromSeconds(3);

    private readonly MainWindowViewModel viewModel;
    private readonly IHotkeyService hotkeyService;
    private readonly ITrayIconService trayIconService;
    private readonly IAutoClickerController autoClickerController;
    private readonly Random random = new();
    private bool allowClickerHotkeyFocusFromClick;
    private bool allowScreenshotHotkeyFocusFromClick;
    private bool allowMacroHotkeyFocusFromClick;
    private bool allowMacroRecordHotkeyFocusFromClick;
    private bool isClosingAfterAutoSave;
    private bool isTransitioningToTray;
    private System.Windows.Point? pendingMaximizedTitleBarDragPoint;
    private DispatcherTimer? confettiTimer;
    private DateTime confettiStartedAtUtc;

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
        viewModel.PropertyChanged += ViewModel_PropertyChanged;
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
            viewModel.SetStatus(AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.TrayStartupHiddenStatus));
        }

        ApplySillyModeState();
    }

    private void MainWindow_OnStateChanged(object? sender, EventArgs e)
    {
        if (!IsLoaded || isTransitioningToTray || WindowState != WindowState.Minimized)
        {
            return;
        }

        HideToTray();
        viewModel.SetStatus(AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.TrayMinimizedStatus));
    }

    private void TitleBar_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            ClearPendingTitleBarDrag(sender as UIElement);
            ToggleMaximizeRestore();
            return;
        }

        if (e.ButtonState == MouseButtonState.Pressed)
        {
            if (WindowState == WindowState.Maximized)
            {
                pendingMaximizedTitleBarDragPoint = e.GetPosition(this);
                (sender as UIElement)?.CaptureMouse();
                return;
            }

            DragMove();
        }
    }

    private void TitleBar_OnMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (WindowState != WindowState.Maximized
            || pendingMaximizedTitleBarDragPoint is null
            || e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        var currentPoint = e.GetPosition(this);
        var dragStartPoint = pendingMaximizedTitleBarDragPoint.Value;
        if (Math.Abs(currentPoint.X - dragStartPoint.X) < SystemParameters.MinimumHorizontalDragDistance
            && Math.Abs(currentPoint.Y - dragStartPoint.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        RestoreFromMaximizedForDrag(sender as FrameworkElement, dragStartPoint);
        ClearPendingTitleBarDrag(sender as UIElement);

        try
        {
            DragMove();
        }
        catch (InvalidOperationException)
        {
        }

        e.Handled = true;
    }

    private void TitleBar_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        ClearPendingTitleBarDrag(sender as UIElement);
    }

    private void TitleBar_OnLostMouseCapture(object sender, System.Windows.Input.MouseEventArgs e)
    {
        ClearPendingTitleBarDrag(sender as UIElement, releaseCapture: false);
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
            viewModel.SetStatus(AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.HotkeysRegisterWhenReadyStatus));
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
        if (ShouldSuppressHotkeyExecution())
        {
            return;
        }

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

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.IsSillyModeEnabled))
        {
            ApplySillyModeState();
        }
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
            StopSillyModeEffects();
            viewModel.PropertyChanged -= ViewModel_PropertyChanged;
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
        if ((Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control)
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

    private void ApplySillyModeState()
    {
        if (!IsLoaded)
        {
            return;
        }

        if (viewModel.IsSillyModeEnabled)
        {
            StartConfetti();
            return;
        }

        StopSillyModeEffects();
    }

    private void StopSillyModeEffects()
    {
        if (confettiTimer is not null)
        {
            confettiTimer.Stop();
        }

        SillyConfettiCanvas.Children.Clear();
        SillyConfettiCanvas.Visibility = Visibility.Collapsed;
    }

    private void StartConfetti()
    {
        if (confettiTimer is not null)
        {
            confettiTimer.Stop();
        }

        SillyConfettiCanvas.Children.Clear();
        SillyConfettiCanvas.Visibility = Visibility.Visible;
        confettiStartedAtUtc = DateTime.UtcNow;
        SpawnConfettiBurst(12, ConfettiDuration);

        confettiTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(120),
        };
        confettiTimer.Tick += ConfettiTimer_OnTick;
        confettiTimer.Start();
    }

    private void ConfettiTimer_OnTick(object? sender, EventArgs e)
    {
        if (!viewModel.IsSillyModeEnabled)
        {
            confettiTimer?.Stop();
            confettiTimer = null;
            return;
        }

        var elapsed = DateTime.UtcNow - confettiStartedAtUtc;
        if (elapsed >= ConfettiDuration)
        {
            confettiTimer?.Stop();
            confettiTimer = null;
            return;
        }

        // Spawn denser waves near the middle for a fuller cascade feel.
        var progress = elapsed.TotalMilliseconds / ConfettiDuration.TotalMilliseconds;
        var waveSize = progress is >= 0.25 and <= 0.75 ? 10 : 6;
        SpawnConfettiBurst(waveSize, ConfettiDuration - elapsed + TimeSpan.FromMilliseconds(300));
    }

    private void SpawnConfettiBurst(int pieceCount, TimeSpan duration)
    {
        var width = Math.Max(SillyConfettiCanvas.ActualWidth, MainRootGrid.ActualWidth);
        var height = Math.Max(SillyConfettiCanvas.ActualHeight, MainRootGrid.ActualHeight);
        if (width <= 0 || height <= 0)
        {
            return;
        }

        for (var index = 0; index < pieceCount; index++)
        {
            var piece = new System.Windows.Shapes.Rectangle
            {
                Width = random.Next(6, 14),
                Height = random.Next(6, 14),
                RadiusX = 1.5,
                RadiusY = 1.5,
                Fill = ConfettiBrushes[random.Next(ConfettiBrushes.Length)],
                Opacity = 0.95,
                IsHitTestVisible = false,
                RenderTransformOrigin = new System.Windows.Point(0.5, 0.5),
                RenderTransform = new RotateTransform(random.Next(0, 360)),
            };

            SillyConfettiCanvas.Children.Add(piece);

            var startX = random.NextDouble() * Math.Max(1, width - 20);
            var endX = Math.Clamp(startX + random.Next(-120, 121), 0, Math.Max(0, width - piece.Width));

            Canvas.SetLeft(piece, startX);
            Canvas.SetTop(piece, -20);

            var fallAnimation = new DoubleAnimation
            {
                From = -20,
                To = height + 30,
                Duration = duration,
            };

            var driftAnimation = new DoubleAnimation
            {
                From = startX,
                To = endX,
                Duration = duration,
            };

            fallAnimation.Completed += (_, _) => SillyConfettiCanvas.Children.Remove(piece);

            piece.BeginAnimation(Canvas.TopProperty, fallAnimation);
            piece.BeginAnimation(Canvas.LeftProperty, driftAnimation);
        }
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

    private void RestoreFromMaximizedForDrag(FrameworkElement? titleBar, System.Windows.Point dragStartPoint)
    {
        var restoreBounds = RestoreBounds;
        if (restoreBounds.Width <= 0 || restoreBounds.Height <= 0)
        {
            WindowState = WindowState.Normal;
            return;
        }

        var screenPoint = PointToScreen(dragStartPoint);
        var restorePoint = ConvertFromDevicePoint(screenPoint);
        var horizontalRatio = ActualWidth <= 0
            ? 0.5d
            : Clamp(dragStartPoint.X / ActualWidth, 0d, 1d);
        var restoredLeft = restorePoint.X - (restoreBounds.Width * horizontalRatio);
        var titleBarHeight = titleBar is { ActualHeight: > 0 } ? titleBar.ActualHeight : DefaultTitleBarHeight;
        var restoredTop = restorePoint.Y - Math.Min(dragStartPoint.Y, titleBarHeight);

        WindowState = WindowState.Normal;
        Left = Clamp(
            restoredLeft,
            SystemParameters.VirtualScreenLeft,
            SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - Width);
        Top = Clamp(
            restoredTop,
            SystemParameters.VirtualScreenTop,
            SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight - titleBarHeight);
    }

    private System.Windows.Point ConvertFromDevicePoint(System.Windows.Point devicePoint)
    {
        var source = PresentationSource.FromVisual(this);
        if (source?.CompositionTarget is null)
        {
            return devicePoint;
        }

        return source.CompositionTarget.TransformFromDevice.Transform(devicePoint);
    }

    private void ClearPendingTitleBarDrag(UIElement? titleBar, bool releaseCapture = true)
    {
        pendingMaximizedTitleBarDragPoint = null;
        if (releaseCapture && titleBar?.IsMouseCaptured == true)
        {
            titleBar.ReleaseMouseCapture();
        }
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
            or System.Windows.Input.Key.RightAlt
            or System.Windows.Input.Key.LWin
            or System.Windows.Input.Key.RWin;
}
