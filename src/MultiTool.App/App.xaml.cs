using System.Windows;
using MultiTool.App.Localization;
using MultiTool.App.Services;
using MultiTool.App.ViewModels;
using MultiTool.App.Views;
using MultiTool.Core.Services;
using MultiTool.Core.Validation;
using MultiTool.Infrastructure.Windows.Hotkeys;
using MultiTool.Infrastructure.Windows.Installer;
using MultiTool.Infrastructure.Windows.Input;
using MultiTool.Infrastructure.Windows.Macro;
using MultiTool.Infrastructure.Windows.Persistence;
using MultiTool.Infrastructure.Windows.Screenshot;
using MultiTool.Infrastructure.Windows.Startup;
using MultiTool.Infrastructure.Windows.Tools;
using MultiTool.Infrastructure.Windows.Tray;
using MultiTool.Infrastructure.Windows.Updates;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MultiTool.App;

public partial class App : System.Windows.Application
{
    private IHost? host;

    public App()
    {
        DispatcherUnhandledException += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        try
        {
            AppLog.Info("Startup begin.");
            base.OnStartup(e);
            var launchOptions = AppLaunchOptions.FromArgs(e.Args);

            host = Host.CreateDefaultBuilder()
                .ConfigureServices(
                    (_, services) =>
                    {
                        services.AddSingleton(launchOptions);
                        services.AddSingleton<SettingsValidator>();
                        services.AddSingleton<IAppSettingsStore, JsonAppSettingsStore>();
                        services.AddSingleton<ICursorService, WindowsCursorService>();
                        services.AddSingleton<IMouseInputService, WindowsMouseInputService>();
                        services.AddSingleton<IMacroService, WindowsMacroService>();
                        services.AddSingleton<IScreenshotCaptureService, WindowsScreenshotCaptureService>();
                        services.AddSingleton<IInstallerService, WindowsWingetInstallerService>();
                        services.AddSingleton<IAppUpdateService, GitHubAppUpdateService>();
                        services.AddSingleton<IBrowserLauncherService, WindowsBrowserLauncherService>();
                        services.AddSingleton<IFirefoxExtensionService, WindowsFirefoxExtensionService>();
                        services.AddSingleton<IEmptyDirectoryService, WindowsEmptyDirectoryService>();
                        services.AddSingleton<IShortcutHotkeyInventoryService, WindowsShortcutHotkeyInventoryService>();
                        services.AddSingleton<IShortcutHotkeyDisableService, WindowsShortcutHotkeyDisableService>();
                        services.AddSingleton<IIpv4SocketSnapshotService, WindowsIpv4SocketSnapshotService>();
                        services.AddSingleton<ISystemTrayMetricsService, WindowsSystemTrayMetricsService>();
                        services.AddSingleton<IMouseSensitivityService, WindowsMouseSensitivityService>();
                        services.AddSingleton<IDisplayRefreshRateService, WindowsDisplayRefreshRateService>();
                        services.AddSingleton<IHardwareInventoryService, WindowsHardwareInventoryService>();
                        services.AddSingleton<IDriverUpdateService, WindowsDriverUpdateService>();
                        services.AddSingleton<IWindows11EeaMediaService, Windows11EeaMediaService>();
                        services.AddSingleton<IWindowsSearchReplacementService, WindowsSearchReplacementService>();
                        services.AddSingleton<IWindowsSearchReindexService, WindowsSearchReindexService>();
                        services.AddSingleton<IWindowsTelemetryService, WindowsTelemetryService>();
                        services.AddSingleton<IOneDriveRemovalService, WindowsOneDriveRemovalService>();
                        services.AddSingleton<IEdgeRemovalService, WindowsEdgeRemovalService>();
                        services.AddSingleton<IFnCtrlSwapService, WindowsFnCtrlSwapService>();
                        services.AddSingleton<IAutoClickerController, AutoClickerController>();
                        services.AddSingleton<IHotkeyService, WindowsHotkeyService>();
                        services.AddSingleton<ITrayIconService, NotifyIconTrayService>();

                        services.AddSingleton<IHotkeySettingsDialogService, HotkeySettingsDialogService>();
                        services.AddSingleton<IMacroHotkeyAssignmentsDialogService, MacroHotkeyAssignmentsDialogService>();
                        services.AddSingleton<ICoordinateCaptureDialogService, CoordinateCaptureDialogService>();
                        services.AddSingleton<IFolderPickerService, FolderPickerService>();
                        services.AddSingleton<IMacroLibraryService, MacroLibraryService>();
                        services.AddSingleton<IMacroEditorDialogService, MacroEditorDialogService>();
                        services.AddSingleton<IMacroNamePromptService, MacroNamePromptService>();
                        services.AddSingleton<IMacroFileDialogService, MacroFileDialogService>();
                        services.AddSingleton<IScreenshotAreaSelectionService, ScreenshotAreaSelectionService>();
                        services.AddSingleton<IAboutWindowService, AboutWindowService>();
                        services.AddSingleton<IShortcutHotkeyDialogService, ShortcutHotkeyDialogService>();
                        services.AddSingleton<IThemeService, ThemeService>();
                        services.AddSingleton<IClipboardTextService, ClipboardTextService>();
                        services.AddSingleton<IVideoRecordingIndicatorService, VideoRecordingIndicatorService>();
                        services.AddSingleton<IRunAtStartupService, WindowsRunAtStartupService>();
                        services.AddHostedService<MemoryDiagnosticsService>();

                        services.AddSingleton<IMacroFileStore, JsonMacroFileStore>();
                        services.AddSingleton<MainWindowViewModel>();
                        services.AddSingleton<MainWindow>();
                    })
                .Build();

            AppLog.Info("Host built.");
            await host.StartAsync();
            AppLog.Info("Host started.");

            var settingsStore = host.Services.GetRequiredService<IAppSettingsStore>();
            var themeService = host.Services.GetRequiredService<IThemeService>();
            var settings = await settingsStore.LoadAsync();
            var isDarkMode = settings.Ui.IsDarkMode ?? themeService.GetSystemPrefersDarkMode();
            themeService.ApplyTheme(isDarkMode);
            AppLog.Info("Theme applied.");

            var mainWindow = host.Services.GetRequiredService<MainWindow>();
            MainWindow = mainWindow;
            mainWindow.Show();
            themeService.ApplyTheme(isDarkMode);
            AppLog.Info("Main window shown.");
        }
        catch (Exception ex)
        {
            AppLog.Error(ex, "Startup failed.");
            System.Windows.MessageBox.Show(
                AppLanguageStrings.FormatForCurrentLanguage(AppLanguageKeys.StartupFailureMessage, Environment.NewLine, ex.Message),
                AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.StartupErrorTitle),
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
            Shutdown(-1);
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (host is not null)
        {
            await host.StopAsync();
            host.Dispose();
        }

        base.OnExit(e);
    }

    private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        AppLog.Error(e.Exception, "Unhandled dispatcher exception.");
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
        {
            AppLog.Error(exception, "Unhandled AppDomain exception.");
        }
        else
        {
            AppLog.Error($"Unhandled AppDomain exception object: {e.ExceptionObject}");
        }
    }

    private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        AppLog.Error(e.Exception, "Unobserved task exception.");
        e.SetObserved();
    }
}
