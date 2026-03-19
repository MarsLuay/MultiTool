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
    private const double MinimumUiScale = 0.8;
    private const double MaximumUiScale = 1.8;
    private const double UiScaleStep = 1.08;
    private const double DefaultTitleBarHeight = 34d;
    private static readonly TimeSpan InAppInteractionClickerPause = TimeSpan.FromMilliseconds(180);
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
    private readonly IScreenshotCaptureService screenshotCaptureService;
    private readonly IVideoRecordingIndicatorService videoRecordingIndicatorService;
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
        IAutoClickerController autoClickerController,
        IScreenshotCaptureService screenshotCaptureService,
        IVideoRecordingIndicatorService videoRecordingIndicatorService)
    {
        this.viewModel = viewModel;
        this.hotkeyService = hotkeyService;
        this.trayIconService = trayIconService;
        this.autoClickerController = autoClickerController;
        this.screenshotCaptureService = screenshotCaptureService;
        this.videoRecordingIndicatorService = videoRecordingIndicatorService;

        InitializeComponent();
        DataContext = viewModel;

        Loaded += MainWindow_OnLoaded;
        Activated += MainWindow_OnActivated;
        StateChanged += MainWindow_OnStateChanged;
        Deactivated += MainWindow_OnDeactivated;
        Closing += MainWindow_OnClosing;

        viewModel.HotkeysChanged += ViewModel_HotkeysChanged;
        viewModel.PropertyChanged += ViewModel_PropertyChanged;
        hotkeyService.HotkeyPressed += HotkeyService_HotkeyPressed;
        autoClickerController.RunningStateChanged += AutoClickerController_RunningStateChanged;
        screenshotCaptureService.VideoCaptureStateChanged += ScreenshotCaptureService_VideoCaptureStateChanged;

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
        viewModel.SetMainWindowActive(IsActive);
        RegisterHotkeys();
        trayIconService.SetRunningState(viewModel.IsRunning);

        if (viewModel.ShouldAutoHideOnStartup)
        {
            HideToTray();
            viewModel.SetStatus(AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.TrayStartupHiddenStatus));
        }

        ApplySillyModeState();
    }

    private void MainWindow_OnActivated(object? sender, EventArgs e)
    {
        viewModel.SetMainWindowActive(true);
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

    private void ScreenshotCaptureService_VideoCaptureStateChanged(object? sender, VideoCaptureStateChangedEventArgs e)
    {
        Dispatcher.Invoke(
            () =>
            {
                if (e.IsRecording)
                {
                    videoRecordingIndicatorService.ShowForRecordingArea(e.CaptureArea);
                }
                else
                {
                    videoRecordingIndicatorService.Hide();
                }
            });
    }

    private async void MainWindow_OnDeactivated(object? sender, EventArgs e)
    {
        viewModel.SetMainWindowActive(false);

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
            screenshotCaptureService.VideoCaptureStateChanged -= ScreenshotCaptureService_VideoCaptureStateChanged;
            videoRecordingIndicatorService.Dispose();
            trayIconService.Dispose();
            return;
        }

        e.Cancel = true;
        await viewModel.AutoSaveAsync();
        isClosingAfterAutoSave = true;
        Close();
    }
}
