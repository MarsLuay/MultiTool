using FluentAssertions;
using MultiTool.App.Models;
using MultiTool.App.Services;
using MultiTool.App.ViewModels;
using MultiTool.Core.Defaults;
using MultiTool.Core.Models;
using MultiTool.Core.Results;
using MultiTool.Core.Services;
using MultiTool.Core.Validation;
using System.IO;

namespace MultiTool.App.Tests;

public sealed class MainWindowViewModelSettingsFlowTests
{
    [Fact]
    public async Task InitializeAsync_ShouldApplyLoadedUiSettings()
    {
        await StaDispatcherTestRunner.RunAsync(
            async () =>
            {
                var settings = DefaultSettingsFactory.Create();
                settings.Ui.IsDarkMode = true;
                settings.Ui.EnableCtrlWheelResize = false;
                settings.Ui.AutoHideOnStartup = true;
                settings.Screenshot.CaptureHotkey = new HotkeyBinding(0x79, "F10");

                var context = new MainWindowViewModelTestContext(settings);

                await context.ViewModel.InitializeAsync();

                context.ViewModel.IsDarkMode.Should().BeTrue();
                context.ViewModel.IsCtrlWheelResizeEnabled.Should().BeFalse();
                context.ViewModel.IsAutoHideOnStartupEnabled.Should().BeTrue();
                context.ViewModel.ScreenshotHotkeyDisplay.Should().Be("F10");
                context.ThemeService.AppliedModes.Should().ContainSingle().Which.Should().BeTrue();
            });
    }

    [Fact]
    public async Task InitializeAsync_WhenRunAtStartupSettingIsMissing_ShouldMigrateCurrentSystemState()
    {
        await StaDispatcherTestRunner.RunAsync(
            async () =>
            {
                var settings = DefaultSettingsFactory.Create();
                settings.Ui.RunAtStartup = null;

                var context = new MainWindowViewModelTestContext(settings);
                context.RunAtStartupService.CurrentState = true;

                await context.ViewModel.InitializeAsync();
                await context.SettingsStore.WaitForSaveCountAsync(expectedCount: 1);

                context.ViewModel.IsRunAtStartupEnabled.Should().BeTrue();
                context.RunAtStartupService.SetEnabledCalls.Should().Equal(true);
                context.SettingsStore.LastSavedSettings.Should().NotBeNull();
                context.SettingsStore.LastSavedSettings!.Ui.RunAtStartup.Should().BeTrue();
            });
    }

    [Fact]
    public async Task ChangingUiSettingsAfterInitialization_ShouldAutoSaveUpdatedValues()
    {
        await StaDispatcherTestRunner.RunAsync(
            async () =>
            {
                var settings = DefaultSettingsFactory.Create();
                settings.Ui.RunAtStartup = null;

                var context = new MainWindowViewModelTestContext(settings);

                await context.ViewModel.InitializeAsync();

                context.ViewModel.IsCtrlWheelResizeEnabled = false;
                context.ViewModel.IsAutoHideOnStartupEnabled = true;
                context.ViewModel.IsRunAtStartupEnabled = true;

                await context.SettingsStore.WaitForSaveCountAsync(expectedCount: 1);

                context.RunAtStartupService.SetEnabledCalls.Should().Equal(true);
                context.SettingsStore.LastSavedSettings.Should().NotBeNull();
                context.SettingsStore.LastSavedSettings!.Ui.EnableCtrlWheelResize.Should().BeFalse();
                context.SettingsStore.LastSavedSettings!.Ui.AutoHideOnStartup.Should().BeTrue();
                context.SettingsStore.LastSavedSettings!.Ui.RunAtStartup.Should().BeTrue();
            });
    }

    private sealed class MainWindowViewModelTestContext
    {
        public MainWindowViewModelTestContext(AppSettings loadedSettings)
        {
            SettingsStore = new FakeAppSettingsStore(loadedSettings);
            ThemeService = new FakeThemeService();
            RunAtStartupService = new FakeRunAtStartupService();

            ViewModel = new MainWindowViewModel(
                SettingsStore,
                new SettingsValidator(),
                new FakeAutoClickerController(),
                new FakeMacroFileStore(),
                new FakeMacroService(),
                new FakeFolderPickerService(),
                new FakeScreenshotCaptureService(),
                new FakeScreenshotAreaSelectionService(),
                new FakeMacroEditorDialogService(),
                new FakeMacroNamePromptService(),
                new FakeMacroFileDialogService(),
                new FakeHotkeySettingsDialogService(),
                new FakeMacroHotkeyAssignmentsDialogService(),
                new FakeCoordinateCaptureDialogService(),
                new FakeAboutWindowService(),
                ThemeService,
                new FakeClipboardTextService(),
                RunAtStartupService,
                new FakeMacroLibraryService(),
                new FakeInstallerService(),
                new FakeAppUpdateService(),
                new FakeBrowserLauncherService(),
                new FakeFirefoxExtensionService(),
                new FakeEmptyDirectoryService(),
                new FakeShortcutHotkeyInventoryService(),
                new FakeShortcutHotkeyDisableService(),
                new FakeIpv4SocketSnapshotService(),
                new FakeMouseSensitivityService(),
                new FakeDisplayRefreshRateService(),
                new FakeHardwareInventoryService(),
                new FakeDriverUpdateService(),
                new FakeWindows11EeaMediaService(),
                new FakeWindowsSearchReplacementService(),
                new FakeWindowsSearchReindexService(),
                new FakeWindowsTelemetryService(),
                new FakeOneDriveRemovalService(),
                new FakeEdgeRemovalService(),
                new FakeFnCtrlSwapService(),
                new FakeShortcutHotkeyDialogService(),
                new AppLaunchOptions());
        }

        public MainWindowViewModel ViewModel { get; }

        public FakeAppSettingsStore SettingsStore { get; }

        public FakeThemeService ThemeService { get; }

        public FakeRunAtStartupService RunAtStartupService { get; }
    }

    private sealed class FakeAppSettingsStore : IAppSettingsStore
    {
        private readonly AppSettings loadedSettings;

        public FakeAppSettingsStore(AppSettings loadedSettings)
        {
            this.loadedSettings = loadedSettings.Clone();
        }

        public AppSettings? LastSavedSettings { get; private set; }

        public int SaveCount { get; private set; }

        public Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(loadedSettings.Clone());

        public Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
        {
            LastSavedSettings = settings.Clone();
            SaveCount++;
            return Task.CompletedTask;
        }

        public async Task WaitForSaveCountAsync(int expectedCount, TimeSpan? timeout = null)
        {
            var effectiveTimeout = timeout ?? TimeSpan.FromSeconds(3);
            var startedAt = DateTime.UtcNow;

            while (SaveCount < expectedCount && DateTime.UtcNow - startedAt < effectiveTimeout)
            {
                await Task.Delay(25);
            }

            SaveCount.Should().BeGreaterOrEqualTo(expectedCount);
        }
    }

    private sealed class FakeThemeService : IThemeService
    {
        public List<bool> AppliedModes { get; } = [];

        public bool SystemPrefersDarkMode { get; set; }

        public bool GetSystemPrefersDarkMode() => SystemPrefersDarkMode;

        public void ApplyTheme(bool isDarkMode) => AppliedModes.Add(isDarkMode);

        public void ApplyThemeToWindow(System.Windows.Window window)
        {
        }

        public bool TryApplySystemDarkModePreference(out string message)
        {
            message = "System dark mode is unavailable in tests.";
            return false;
        }
    }

    private sealed class FakeRunAtStartupService : IRunAtStartupService
    {
        public List<bool> SetEnabledCalls { get; } = [];

        public bool CurrentState { get; set; }

        public bool IsEnabled() => CurrentState;

        public void SetEnabled(bool enabled)
        {
            CurrentState = enabled;
            SetEnabledCalls.Add(enabled);
        }
    }

    private sealed class FakeAutoClickerController : IAutoClickerController
    {
        public bool IsRunning { get; private set; }

        public event EventHandler<RunningStateChangedEventArgs>? RunningStateChanged;

        public Task StartAsync(ClickSettings settings, CancellationToken cancellationToken = default)
        {
            IsRunning = true;
            RunningStateChanged?.Invoke(this, new RunningStateChangedEventArgs(true));
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken = default)
        {
            IsRunning = false;
            RunningStateChanged?.Invoke(this, new RunningStateChangedEventArgs(false));
            return Task.CompletedTask;
        }

        public Task ToggleAsync(ClickSettings settings, CancellationToken cancellationToken = default) =>
            IsRunning ? StopAsync(cancellationToken) : StartAsync(settings, cancellationToken);

        public void SuspendFor(TimeSpan duration)
        {
        }
    }

    private sealed class FakeMacroFileStore : IMacroFileStore
    {
        public Task SaveAsync(string filePath, RecordedMacro macro, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<RecordedMacro> LoadAsync(string filePath, CancellationToken cancellationToken = default) =>
            Task.FromResult(new RecordedMacro("Test", [], TimeSpan.Zero, DateTimeOffset.UtcNow));
    }

    private sealed class FakeMacroService : IMacroService
    {
        public bool IsRecording { get; private set; }

        public bool IsPlaying { get; private set; }

        public RecordedMacro? CurrentMacro { get; private set; }

        public void StartRecording(string? name = null, bool recordMouseMovement = true)
        {
            IsRecording = true;
            CurrentMacro = new RecordedMacro(name ?? "Test", [], TimeSpan.Zero, DateTimeOffset.UtcNow);
        }

        public RecordedMacro StopRecording()
        {
            IsRecording = false;
            CurrentMacro ??= new RecordedMacro("Test", [], TimeSpan.Zero, DateTimeOffset.UtcNow);
            return CurrentMacro;
        }

        public void SetCurrentMacro(RecordedMacro macro) => CurrentMacro = macro;

        public Task PlayAsync(int repeatCount, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task PlayAsync(RecordedMacro macro, int repeatCount, CancellationToken cancellationToken = default)
        {
            CurrentMacro = macro;
            return Task.CompletedTask;
        }

        public void Clear() => CurrentMacro = null;

        public void Dispose()
        {
        }
    }

    private sealed class FakeFolderPickerService : IFolderPickerService
    {
        public string? PickFolder(string currentPath, string description) => currentPath;
    }

    private sealed class FakeScreenshotCaptureService : IScreenshotCaptureService
    {
        public event EventHandler<VideoCaptureStateChangedEventArgs>? VideoCaptureStateChanged;

        public bool IsVideoCaptureRunning { get; private set; }

        public string? LastSavedVideoPath { get; private set; }

        public Task<string> CaptureDesktopAsync(string outputDirectory, string fileNamePrefix, CancellationToken cancellationToken = default) =>
            Task.FromResult(Path.Combine(outputDirectory, $"{fileNamePrefix}-full.png"));

        public Task<string> CaptureAreaAsync(ScreenRectangle area, string outputDirectory, string fileNamePrefix, CancellationToken cancellationToken = default) =>
            Task.FromResult(Path.Combine(outputDirectory, $"{fileNamePrefix}-area.png"));

        public Task StartVideoCaptureAsync(string outputDirectory, string fileNamePrefix, ScreenRectangle? area = null, CancellationToken cancellationToken = default)
        {
            IsVideoCaptureRunning = true;
            VideoCaptureStateChanged?.Invoke(this, new VideoCaptureStateChangedEventArgs(true, area));
            return Task.CompletedTask;
        }

        public Task<string?> StopVideoCaptureAsync(CancellationToken cancellationToken = default)
        {
            IsVideoCaptureRunning = false;
            LastSavedVideoPath = Path.Combine(Path.GetTempPath(), "multitool-test.mp4");
            VideoCaptureStateChanged?.Invoke(this, new VideoCaptureStateChangedEventArgs(false, null));
            return Task.FromResult<string?>(LastSavedVideoPath);
        }
    }

    private sealed class FakeScreenshotAreaSelectionService : IScreenshotAreaSelectionService
    {
        public ScreenRectangle? SelectArea() => null;
    }

    private sealed class FakeMacroEditorDialogService : IMacroEditorDialogService
    {
        public RecordedMacro? Edit(RecordedMacro macro) => macro;
    }

    private sealed class FakeMacroNamePromptService : IMacroNamePromptService
    {
        public string? PromptForName(string suggestedName) => suggestedName;
    }

    private sealed class FakeMacroFileDialogService : IMacroFileDialogService
    {
        public string? PickOpenPath() => null;
    }

    private sealed class FakeHotkeySettingsDialogService : IHotkeySettingsDialogService
    {
        public HotkeySettings? Edit(HotkeySettings currentSettings) => currentSettings.Clone();
    }

    private sealed class FakeMacroHotkeyAssignmentsDialogService : IMacroHotkeyAssignmentsDialogService
    {
        public IReadOnlyList<MacroHotkeyAssignment>? Edit(
            IReadOnlyList<SavedMacroEntry> savedMacros,
            IReadOnlyList<MacroHotkeyAssignment> currentAssignments) =>
            [.. currentAssignments.Select(static assignment => assignment.Clone())];
    }

    private sealed class FakeCoordinateCaptureDialogService : ICoordinateCaptureDialogService
    {
        public ScreenPoint? Capture() => null;
    }

    private sealed class FakeAboutWindowService : IAboutWindowService
    {
        public void Show()
        {
        }
    }

    private sealed class FakeClipboardTextService : IClipboardTextService
    {
        public void SetText(string text)
        {
        }
    }

    private sealed class FakeMacroLibraryService : IMacroLibraryService
    {
        public string DefaultDirectory => Path.Combine(Path.GetTempPath(), "MultiToolAppTests");

        public IReadOnlyList<SavedMacroEntry> GetSavedMacros() => [];

        public string GetSavePath(string macroName) => Path.Combine(DefaultDirectory, $"{macroName}.json");
    }

    private sealed class FakeInstallerService : IInstallerService
    {
        public IReadOnlyList<InstallerCatalogItem> GetCatalog() => [];

        public IReadOnlyList<InstallerCatalogItem> GetCleanupCatalog() => [];

        public InstallerPackageCapabilities GetPackageCapabilities(string packageId) => new();

        public Task<InstallerEnvironmentInfo> GetEnvironmentInfoAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new InstallerEnvironmentInfo(true, "test", "Installer ready."));

        public Task<IReadOnlyList<InstallerPackageStatus>> GetPackageStatusesAsync(IEnumerable<string> packageIds, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<InstallerPackageStatus>>([]);

        public Task<IReadOnlyList<InstallerOperationResult>> RunPackageOperationAsync(string packageId, InstallerPackageAction action, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<InstallerOperationResult>>([]);

        public Task<IReadOnlyList<InstallerOperationResult>> InstallPackagesAsync(IEnumerable<string> packageIds, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<InstallerOperationResult>>([]);

        public Task<IReadOnlyList<InstallerOperationResult>> UpgradePackagesAsync(IEnumerable<string> packageIds, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<InstallerOperationResult>>([]);

        public Task<IReadOnlyList<InstallerOperationResult>> UninstallPackagesAsync(IEnumerable<string> packageIds, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<InstallerOperationResult>>([]);
    }

    private sealed class FakeAppUpdateService : IAppUpdateService
    {
        public Task<AppUpdateInfo> CheckForUpdatesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new AppUpdateInfo(true, false, "1.0.0", "1.0.0", "MultiTool is up to date."));
    }

    private sealed class FakeBrowserLauncherService : IBrowserLauncherService
    {
        public BrowserLaunchResult OpenUrl(string url) => new("Test Browser");
    }

    private sealed class FakeFirefoxExtensionService : IFirefoxExtensionService
    {
        public IReadOnlyList<InstallerOptionDefinition> GetCatalog() => [];

        public Task<IReadOnlyList<InstallerOperationResult>> SyncExtensionSelectionsAsync(
            IEnumerable<string> selectedOptionIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<InstallerOperationResult>>([]);
    }

    private sealed class FakeEmptyDirectoryService : IEmptyDirectoryService
    {
        public Task<EmptyDirectoryScanResult> FindEmptyDirectoriesAsync(
            string rootPath,
            IProgress<EmptyDirectoryScanProgress>? progress = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<EmptyDirectoryDeleteResult>> DeleteDirectoriesAsync(IEnumerable<string> directoryPaths, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class FakeShortcutHotkeyInventoryService : IShortcutHotkeyInventoryService
    {
        public Task<ShortcutHotkeyScanResult> ScanAsync(
            IProgress<ShortcutHotkeyScanProgress>? progress = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class FakeShortcutHotkeyDisableService : IShortcutHotkeyDisableService
    {
        public Task<ShortcutHotkeyDisableResult> DisableAsync(IReadOnlyList<ShortcutHotkeyInfo> shortcuts, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class FakeIpv4SocketSnapshotService : IIpv4SocketSnapshotService
    {
        public Task<Ipv4SocketSnapshotResult> CaptureAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class FakeMouseSensitivityService : IMouseSensitivityService
    {
        public IReadOnlyList<int> GetSupportedLevels() => [10];

        public MouseSensitivityStatus GetStatus() => new(10, "Mouse speed ready.");

        public Task<MouseSensitivityApplyResult> ApplyAsync(int level, CancellationToken cancellationToken = default) =>
            Task.FromResult(new MouseSensitivityApplyResult(true, true, level, "Applied."));
    }

    private sealed class FakeDisplayRefreshRateService : IDisplayRefreshRateService
    {
        public Task<IReadOnlyList<DisplayRefreshRecommendation>> GetRecommendationsAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<DisplayRefreshApplyResult>> ApplyRecommendedAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class FakeHardwareInventoryService : IHardwareInventoryService
    {
        public Task<HardwareInventoryReport> GetReportAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class FakeDriverUpdateService : IDriverUpdateService
    {
        public Task<DriverUpdateScanResult> ScanAsync(IReadOnlyList<DriverHardwareInfo>? hardwareInventory = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<DriverUpdateInstallResult>> InstallAsync(IEnumerable<string> updateIds, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class FakeWindows11EeaMediaService : IWindows11EeaMediaService
    {
        public event EventHandler<string>? StatusChanged;

        public Task<Windows11EeaMediaPreparationResult> PrepareAsync(CancellationToken cancellationToken = default)
        {
            StatusChanged?.Invoke(this, "Preparing media.");
            return Task.FromResult(new Windows11EeaMediaPreparationResult(true, false, string.Empty, string.Empty, string.Empty, "Ready."));
        }
    }

    private sealed class FakeWindowsSearchReplacementService : IWindowsSearchReplacementService
    {
        public WindowsSearchReplacementStatus GetStatus() => new(false, false, false, false, false, false, "Search replacement ready.");

        public Task<WindowsSearchReplacementResult> ApplyAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new WindowsSearchReplacementResult(true, true, "Applied."));

        public Task<WindowsSearchReplacementResult> RestoreAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new WindowsSearchReplacementResult(true, true, "Restored."));
    }

    private sealed class FakeWindowsSearchReindexService : IWindowsSearchReindexService
    {
        public WindowsSearchReindexStatus GetStatus() => new(true, false, "Search reindex ready.");

        public Task<WindowsSearchReindexResult> ReindexAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new WindowsSearchReindexResult(true, true, "Requested."));
    }

    private sealed class FakeWindowsTelemetryService : IWindowsTelemetryService
    {
        public WindowsTelemetryStatus GetStatus() => new(false, "Telemetry status ready.");

        public Task<WindowsTelemetryResult> ApplyAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new WindowsTelemetryResult(true, true, "Applied."));

        public Task<WindowsTelemetryResult> RestoreAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new WindowsTelemetryResult(true, true, "Restored."));
    }

    private sealed class FakeOneDriveRemovalService : IOneDriveRemovalService
    {
        public OneDriveRemovalStatus GetStatus() => new(false, "OneDrive status ready.");

        public Task<OneDriveRemovalResult> RemoveAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new OneDriveRemovalResult(true, true, "Removed."));
    }

    private sealed class FakeEdgeRemovalService : IEdgeRemovalService
    {
        public EdgeRemovalStatus GetStatus() => new(false, "Edge status ready.");

        public Task<EdgeRemovalResult> RemoveAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new EdgeRemovalResult(true, true, "Removed."));
    }

    private sealed class FakeFnCtrlSwapService : IFnCtrlSwapService
    {
        public FnCtrlSwapStatus GetStatus() => new(true, false, "Fn/Ctrl status ready.");

        public Task<FnCtrlSwapResult> ToggleAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new FnCtrlSwapResult(true, true, "Toggled."));
    }

    private sealed class FakeShortcutHotkeyDialogService : IShortcutHotkeyDialogService
    {
        public void Show(
            ShortcutHotkeyScanResult result,
            bool isCachedResult,
            Func<Task<ShortcutHotkeyScanResult>> rescanAsync,
            Func<IReadOnlyList<ShortcutHotkeyInfo>, Task<ShortcutHotkeyDisableOperationResult>> disableAsync)
        {
        }
    }
}
