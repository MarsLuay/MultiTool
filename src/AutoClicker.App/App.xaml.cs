using System.Windows;
using AutoClicker.App.Services;
using AutoClicker.App.ViewModels;
using AutoClicker.App.Views;
using AutoClicker.Core.Services;
using AutoClicker.Core.Validation;
using AutoClicker.Infrastructure.Windows.Hotkeys;
using AutoClicker.Infrastructure.Windows.Installer;
using AutoClicker.Infrastructure.Windows.Input;
using AutoClicker.Infrastructure.Windows.Macro;
using AutoClicker.Infrastructure.Windows.Persistence;
using AutoClicker.Infrastructure.Windows.Screenshot;
using AutoClicker.Infrastructure.Windows.Tools;
using AutoClicker.Infrastructure.Windows.Tray;
using AutoClicker.Infrastructure.Windows.Updates;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AutoClicker.App;

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
            var launchOptions = new AppLaunchOptions
            {
                IsStartupLaunch = e.Args.Any(argument => string.Equals(argument, "--startup-launch", StringComparison.OrdinalIgnoreCase)),
            };

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
                        services.AddSingleton<IMouseSensitivityService, WindowsMouseSensitivityService>();
                        services.AddSingleton<IDisplayRefreshRateService, WindowsDisplayRefreshRateService>();
                        services.AddSingleton<IHardwareInventoryService, WindowsHardwareInventoryService>();
                        services.AddSingleton<IDriverUpdateService, WindowsDriverUpdateService>();
                        services.AddSingleton<IWindows11EeaMediaService, Windows11EeaMediaService>();
                        services.AddSingleton<IWindowsSearchReplacementService, WindowsSearchReplacementService>();
                        services.AddSingleton<IOneDriveRemovalService, WindowsOneDriveRemovalService>();
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
                        services.AddSingleton<IScreenshotOptionsDialogService, ScreenshotOptionsDialogService>();
                        services.AddSingleton<IScreenshotAreaSelectionService, ScreenshotAreaSelectionService>();
                        services.AddSingleton<IAboutWindowService, AboutWindowService>();
                        services.AddSingleton<IShortcutHotkeyDialogService, ShortcutHotkeyDialogService>();
                        services.AddSingleton<IThemeService, ThemeService>();
                        services.AddSingleton<IClipboardTextService, ClipboardTextService>();

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
                $"MultiTool failed to start. Check the Logs folder next to the EXE for details.{Environment.NewLine}{Environment.NewLine}{ex.Message}",
                "MultiTool Startup Error",
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
