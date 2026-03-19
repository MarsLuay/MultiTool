using FluentAssertions;
using MultiTool.App.Localization;
using MultiTool.App.Models;
using MultiTool.App.Services;
using MultiTool.App.ViewModels;
using MultiTool.Core.Defaults;
using MultiTool.Core.Enums;
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

    [Fact]
    public async Task ChangingPinStateAfterInitialization_ShouldRefreshPinWindowToolPresentation()
    {
        await StaDispatcherTestRunner.RunAsync(
            async () =>
            {
                var settings = DefaultSettingsFactory.Create();
                settings.Hotkeys.PinWindow = new HotkeyBinding(0x78, "F9");

                var context = new MainWindowViewModelTestContext(settings);

                await context.ViewModel.InitializeAsync();

                context.ViewModel.IsTopMost = true;

                context.ViewModel.PinWindowStateText.Should().Be("pinned on top");
                context.ViewModel.PinWindowActionButtonText.Should().Be("Unpin Window");
                context.ViewModel.PinWindowToolStatusMessage.Should().Contain("pinned");
                context.ViewModel.PinWindowToolStatusMessage.Should().Contain("F9");
            });
    }

    [Fact]
    public async Task OpenPinWindowHotkeySettingsAsync_ShouldUpdateHotkeyAndSave()
    {
        await StaDispatcherTestRunner.RunAsync(
            async () =>
            {
                var settings = DefaultSettingsFactory.Create();

                var context = new MainWindowViewModelTestContext(settings);

                await context.ViewModel.InitializeAsync();

                var updatedHotkeys = settings.Hotkeys.Clone();
                updatedHotkeys.PinWindow = new HotkeyBinding(0x79, "F10");
                context.HotkeySettingsDialogService.NextResult = updatedHotkeys;

                await context.ViewModel.OpenPinWindowHotkeySettingsCommand.ExecuteAsync(null);
                await context.SettingsStore.WaitForSaveCountAsync(expectedCount: 1);

                context.HotkeySettingsDialogService.EditCalls.Should().Be(1);
                context.ViewModel.PinWindowHotkeyLabel.Should().Be("F10");
                context.ViewModel.PinWindowHotkeySummary.Should().Contain("F10");
                context.ViewModel.PinWindowToolStatusMessage.Should().Contain("F10");
                context.SettingsStore.LastSavedSettings.Should().NotBeNull();
                context.SettingsStore.LastSavedSettings!.Hotkeys.PinWindow.VirtualKey.Should().Be(0x79);
                context.SettingsStore.LastSavedSettings!.Hotkeys.PinWindow.DisplayName.Should().Be("F10");
            });
    }

    [Fact]
    public async Task ScreenshotHotkey_WhenPressedAgainDuringAreaSelection_ShouldPromoteToVideoCapture()
    {
        await StaDispatcherTestRunner.RunAsync(
            async () =>
            {
                var context = new MainWindowViewModelTestContext(DefaultSettingsFactory.Create());
                await context.ViewModel.InitializeAsync();
                var promotedVideoArea = new ScreenRectangle(10, 20, 300, 200);

                context.ScreenshotAreaSelectionService.EnqueueBehavior(
                    async cancellationToken =>
                    {
                        try
                        {
                            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                        }
                        catch (OperationCanceledException)
                        {
                        }

                        return null;
                    });
                context.ScreenshotAreaSelectionService.EnqueueVideoSelectionResult(
                    new VideoCaptureSelection(VideoCaptureSelectionKind.CurrentScreen, promotedVideoArea));

                var firstPressTask = context.ViewModel.HandleHotkeyAsync(HotkeyAction.ScreenshotCapture);
                await Task.Delay(50);
                var secondPressTask = context.ViewModel.HandleHotkeyAsync(HotkeyAction.ScreenshotCapture);

                await context.ScreenshotAreaSelectionService.WaitForCallCountAsync(expectedCount: 1);

                var thirdPressTask = context.ViewModel.HandleHotkeyAsync(HotkeyAction.ScreenshotCapture);

                await Task.WhenAll(firstPressTask, secondPressTask, thirdPressTask);
                await context.ScreenshotCaptureService.WaitForVideoStartCountAsync(expectedCount: 1);

                context.ScreenshotAreaSelectionService.CallCount.Should().Be(1);
                context.ScreenshotAreaSelectionService.VideoSelectionCallCount.Should().Be(1);
                context.ScreenshotCaptureService.StartVideoCaptureCallCount.Should().Be(1);
                context.ScreenshotCaptureService.CaptureAreaCallCount.Should().Be(0);
                context.ScreenshotCaptureService.LastStartVideoCaptureArea.Should().Be(promotedVideoArea);
                context.ViewModel.ScreenshotStatusMessage.Should().Be(
                    AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.MainScreenshotStatusCurrentScreenRecordingStarted));
            });
    }

    [Fact]
    public async Task ScreenshotHotkey_WhenPressedThreeTimes_ShouldOpenVideoPickerAndStartChosenCapture()
    {
        await StaDispatcherTestRunner.RunAsync(
            async () =>
            {
                var context = new MainWindowViewModelTestContext(DefaultSettingsFactory.Create());
                await context.ViewModel.InitializeAsync();

                context.ScreenshotAreaSelectionService.EnqueueVideoSelectionResult(
                    new VideoCaptureSelection(VideoCaptureSelectionKind.AllScreens, null));

                var firstPressTask = context.ViewModel.HandleHotkeyAsync(HotkeyAction.ScreenshotCapture);
                await Task.Delay(50);
                var secondPressTask = context.ViewModel.HandleHotkeyAsync(HotkeyAction.ScreenshotCapture);
                await Task.Delay(50);
                var thirdPressTask = context.ViewModel.HandleHotkeyAsync(HotkeyAction.ScreenshotCapture);

                await Task.WhenAll(firstPressTask, secondPressTask, thirdPressTask);
                await context.ScreenshotCaptureService.WaitForVideoStartCountAsync(expectedCount: 1);

                context.ScreenshotAreaSelectionService.CallCount.Should().Be(0);
                context.ScreenshotAreaSelectionService.VideoSelectionCallCount.Should().Be(1);
                context.ScreenshotCaptureService.StartVideoCaptureCallCount.Should().Be(1);
                context.ScreenshotCaptureService.LastStartVideoCaptureArea.Should().BeNull();
                context.ViewModel.ScreenshotStatusMessage.Should().Be(
                    AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.MainScreenshotStatusAllScreensRecordingStarted));
            });
    }

    [Fact]
    public async Task ScreenshotHotkey_WhenPressedFourTimes_ShouldStartCurrentScreenRecordingWithoutPicker()
    {
        await StaDispatcherTestRunner.RunAsync(
            async () =>
            {
                var context = new MainWindowViewModelTestContext(DefaultSettingsFactory.Create());
                await context.ViewModel.InitializeAsync();

                var firstPressTask = context.ViewModel.HandleHotkeyAsync(HotkeyAction.ScreenshotCapture);
                await Task.Delay(50);
                var secondPressTask = context.ViewModel.HandleHotkeyAsync(HotkeyAction.ScreenshotCapture);
                await Task.Delay(50);
                var thirdPressTask = context.ViewModel.HandleHotkeyAsync(HotkeyAction.ScreenshotCapture);
                await Task.Delay(50);
                var fourthPressTask = context.ViewModel.HandleHotkeyAsync(HotkeyAction.ScreenshotCapture);

                await Task.WhenAll(firstPressTask, secondPressTask, thirdPressTask, fourthPressTask);
                await context.ScreenshotCaptureService.WaitForVideoStartCountAsync(expectedCount: 1);

                context.ScreenshotAreaSelectionService.CallCount.Should().Be(0);
                context.ScreenshotAreaSelectionService.VideoSelectionCallCount.Should().Be(0);
                context.ScreenshotCaptureService.StartVideoCaptureCallCount.Should().Be(1);
                context.ScreenshotCaptureService.LastStartVideoCaptureArea.Should().NotBeNull();
                context.ScreenshotCaptureService.LastStartVideoCaptureArea!.Value.Width.Should().BeGreaterThan(0);
                context.ScreenshotCaptureService.LastStartVideoCaptureArea!.Value.Height.Should().BeGreaterThan(0);
                context.ViewModel.ScreenshotStatusMessage.Should().Be(
                    AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.MainScreenshotStatusCurrentScreenRecordingStarted));
            });
    }

    [Fact]
    public async Task ToggleHotkey_WhenMainWindowIsActive_ShouldNotStartAutoClicker()
    {
        await StaDispatcherTestRunner.RunAsync(
            async () =>
            {
                var context = new MainWindowViewModelTestContext(DefaultSettingsFactory.Create());

                await context.ViewModel.InitializeAsync();
                context.ViewModel.SetMainWindowActive(true);

                await context.ViewModel.HandleHotkeyAsync(HotkeyAction.Toggle);

                context.AutoClickerController.StartAsyncCallCount.Should().Be(0);
                context.AutoClickerController.IsRunning.Should().BeFalse();
                context.ViewModel.IsRunning.Should().BeFalse();
                context.ViewModel.StatusMessage.Should().Be(
                    AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.MainStatusClickerHotkeyIgnoredWhileFocused));
            });
    }

    [Fact]
    public async Task ToggleHotkey_WhenMainWindowIsActiveAndAutoClickerIsAlreadyRunning_ShouldStillStopAutoClicker()
    {
        await StaDispatcherTestRunner.RunAsync(
            async () =>
            {
                var context = new MainWindowViewModelTestContext(DefaultSettingsFactory.Create());

                await context.ViewModel.InitializeAsync();
                context.ViewModel.SetMainWindowActive(false);
                await context.ViewModel.HandleHotkeyAsync(HotkeyAction.Toggle);

                context.ViewModel.SetMainWindowActive(true);
                await context.ViewModel.HandleHotkeyAsync(HotkeyAction.Toggle);

                context.AutoClickerController.StartAsyncCallCount.Should().Be(1);
                context.AutoClickerController.StopAsyncCallCount.Should().Be(1);
                context.AutoClickerController.IsRunning.Should().BeFalse();
                context.ViewModel.IsRunning.Should().BeFalse();
                context.ViewModel.StatusMessage.Should().Be(
                    AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.MainStatusAutomationStopped));
            });
    }

    [Fact]
    public async Task CaptureIpv4SocketSnapshotAsync_ShouldShowProgramsAndFriendlyClipboardText()
    {
        await StaDispatcherTestRunner.RunAsync(
            async () =>
            {
                var context = new MainWindowViewModelTestContext(DefaultSettingsFactory.Create());
                context.Ipv4SocketSnapshotService.NextResult = new Ipv4SocketSnapshotResult(
                    new DateTimeOffset(2026, 3, 19, 18, 30, 0, TimeSpan.Zero),
                    [
                        new Ipv4SocketEntry("tcp4", "ESTAB", "10.0.0.25:52344", "93.184.216.34:443", "chrome.exe", 7420),
                        new Ipv4SocketEntry("tcp4", "LISTEN", "0.0.0.0:3000", "*:*", "node.exe", 9216),
                        new Ipv4SocketEntry("udp4", "UNCONN", "127.0.0.1:5353", "*:*", string.Empty, 1104),
                    ],
                    1,
                    1,
                    1);

                await context.ViewModel.InitializeAsync();
                await context.ViewModel.CaptureIpv4SocketSnapshotCommand.ExecuteAsync(null);

                context.ViewModel.Ipv4SocketSummary.Should().Contain("across 3 apps");
                context.ViewModel.Ipv4SocketStatusMessage.Should().Contain("Found 3 IPv4 entries across 3 apps");
                context.ViewModel.Ipv4SocketEntries.Select(static entry => entry.ProgramSummary).Should().Contain(
                    "chrome.exe (PID 7420)",
                    "node.exe (PID 9216)",
                    "Unknown app (PID 1104)");

                context.ViewModel.CopyIpv4SocketSnapshotCommand.Execute(null);

                context.ClipboardTextService.LastText.Should().NotBeNull();
                context.ClipboardTextService.LastText.Should().Contain("IPv4 App Activity");
                context.ClipboardTextService.LastText.Should().Contain("chrome.exe (PID 7420)");
                context.ClipboardTextService.LastText.Should().Contain("TCP connection  |  tcp4  ESTAB");
                context.ClipboardTextService.LastText.Should().Contain("Unknown app (PID 1104)");
            });
    }

    [Fact]
    public async Task RestoreTelemetryDefaultsAsync_ShouldKeepRestoreSuccessMessage()
    {
        await StaDispatcherTestRunner.RunAsync(
            async () =>
            {
                var context = new MainWindowViewModelTestContext(DefaultSettingsFactory.Create());
                context.WindowsTelemetryService.Status = new WindowsTelemetryStatus(
                    false,
                    "Telemetry hardening not fully applied. AllowTelemetry policy is not set to 0.");
                context.WindowsTelemetryService.RestoreResult = new WindowsTelemetryResult(
                    true,
                    true,
                    "Restored telemetry defaults by removing the policy override, re-enabling telemetry tasks, and restoring telemetry service startup defaults.");

                await context.ViewModel.InitializeAsync();
                await context.ViewModel.RestoreTelemetryDefaultsCommand.ExecuteAsync(null);

                context.ViewModel.TelemetryToolStatusMessage.Should().Be(context.WindowsTelemetryService.RestoreResult.Message);
            });
    }

    private sealed class MainWindowViewModelTestContext
    {
        public MainWindowViewModelTestContext(AppSettings loadedSettings)
        {
            SettingsStore = new FakeAppSettingsStore(loadedSettings);
            ThemeService = new FakeThemeService();
            RunAtStartupService = new FakeRunAtStartupService();
            AutoClickerController = new FakeAutoClickerController();
            ScreenshotCaptureService = new FakeScreenshotCaptureService();
            ScreenshotAreaSelectionService = new FakeScreenshotAreaSelectionService();
            HotkeySettingsDialogService = new FakeHotkeySettingsDialogService();
            ClipboardTextService = new FakeClipboardTextService();
            Ipv4SocketSnapshotService = new FakeIpv4SocketSnapshotService();
            WindowsTelemetryService = new FakeWindowsTelemetryService();

            ViewModel = new MainWindowViewModel(
                SettingsStore,
                new SettingsValidator(),
                AutoClickerController,
                new FakeMacroFileStore(),
                new FakeMacroService(),
                new FakeFolderPickerService(),
                ScreenshotCaptureService,
                ScreenshotAreaSelectionService,
                new FakeMacroEditorDialogService(),
                new FakeMacroNamePromptService(),
                new FakeMacroFileDialogService(),
                HotkeySettingsDialogService,
                new FakeMacroHotkeyAssignmentsDialogService(),
                new FakeCoordinateCaptureDialogService(),
                new FakeAboutWindowService(),
                ThemeService,
                ClipboardTextService,
                RunAtStartupService,
                new FakeMacroLibraryService(),
                new FakeInstallerService(),
                new FakeAppUpdateService(),
                new FakeBrowserLauncherService(),
                new FakeFirefoxExtensionService(),
                new FakeEmptyDirectoryService(),
                new FakeShortcutHotkeyInventoryService(),
                new FakeShortcutHotkeyDisableService(),
                Ipv4SocketSnapshotService,
                new FakeMouseSensitivityService(),
                new FakeDisplayRefreshRateService(),
                new FakeHardwareInventoryService(),
                new FakeDriverUpdateService(),
                new FakeWindows11EeaMediaService(),
                new FakeWindowsSearchReplacementService(),
                new FakeWindowsSearchReindexService(),
                WindowsTelemetryService,
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

        public FakeAutoClickerController AutoClickerController { get; }

        public FakeScreenshotCaptureService ScreenshotCaptureService { get; }

        public FakeScreenshotAreaSelectionService ScreenshotAreaSelectionService { get; }

        public FakeHotkeySettingsDialogService HotkeySettingsDialogService { get; }

        public FakeClipboardTextService ClipboardTextService { get; }

        public FakeIpv4SocketSnapshotService Ipv4SocketSnapshotService { get; }

        public FakeWindowsTelemetryService WindowsTelemetryService { get; }
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

        public int StartAsyncCallCount { get; private set; }

        public int StopAsyncCallCount { get; private set; }

        public event EventHandler<RunningStateChangedEventArgs>? RunningStateChanged;

        public Task StartAsync(ClickSettings settings, CancellationToken cancellationToken = default)
        {
            StartAsyncCallCount++;
            IsRunning = true;
            RunningStateChanged?.Invoke(this, new RunningStateChangedEventArgs(true));
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken = default)
        {
            StopAsyncCallCount++;
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

        public ScreenRectangle? LastStartVideoCaptureArea { get; private set; }

        public int CaptureDesktopCallCount { get; private set; }

        public int CaptureAreaCallCount { get; private set; }

        public int StartVideoCaptureCallCount { get; private set; }

        public Task<string> CaptureDesktopAsync(string outputDirectory, string fileNamePrefix, CancellationToken cancellationToken = default)
        {
            CaptureDesktopCallCount++;
            return Task.FromResult(Path.Combine(outputDirectory, $"{fileNamePrefix}-full.png"));
        }

        public Task<string> CaptureAreaAsync(ScreenRectangle area, string outputDirectory, string fileNamePrefix, CancellationToken cancellationToken = default)
        {
            CaptureAreaCallCount++;
            return Task.FromResult(Path.Combine(outputDirectory, $"{fileNamePrefix}-area.png"));
        }

        public Task StartVideoCaptureAsync(string outputDirectory, string fileNamePrefix, ScreenRectangle? area = null, CancellationToken cancellationToken = default)
        {
            StartVideoCaptureCallCount++;
            LastStartVideoCaptureArea = area;
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

        public async Task WaitForVideoStartCountAsync(int expectedCount, TimeSpan? timeout = null)
        {
            var effectiveTimeout = timeout ?? TimeSpan.FromSeconds(3);
            var startedAt = DateTime.UtcNow;

            while (StartVideoCaptureCallCount < expectedCount && DateTime.UtcNow - startedAt < effectiveTimeout)
            {
                await Task.Delay(25);
            }

            StartVideoCaptureCallCount.Should().BeGreaterOrEqualTo(expectedCount);
        }
    }

    private sealed class FakeScreenshotAreaSelectionService : IScreenshotAreaSelectionService
    {
        private readonly Queue<Func<CancellationToken, Task<ScreenRectangle?>>> areaSelectionBehaviors = new();
        private readonly Queue<Func<CancellationToken, Task<VideoCaptureSelection?>>> videoSelectionBehaviors = new();

        public int CallCount { get; private set; }

        public int VideoSelectionCallCount { get; private set; }

        public void EnqueueBehavior(Func<CancellationToken, Task<ScreenRectangle?>> behavior) => areaSelectionBehaviors.Enqueue(behavior);

        public void EnqueueResult(ScreenRectangle? result) => areaSelectionBehaviors.Enqueue(_ => Task.FromResult(result));

        public void EnqueueVideoSelectionBehavior(Func<CancellationToken, Task<VideoCaptureSelection?>> behavior) => videoSelectionBehaviors.Enqueue(behavior);

        public void EnqueueVideoSelectionResult(VideoCaptureSelection? result) => videoSelectionBehaviors.Enqueue(_ => Task.FromResult(result));

        public Task<ScreenRectangle?> SelectAreaAsync(CancellationToken cancellationToken = default)
        {
            CallCount++;
            return areaSelectionBehaviors.Count > 0
                ? areaSelectionBehaviors.Dequeue()(cancellationToken)
                : Task.FromResult<ScreenRectangle?>(null);
        }

        public Task<VideoCaptureSelection?> SelectVideoCaptureAsync(CancellationToken cancellationToken = default)
        {
            VideoSelectionCallCount++;
            return videoSelectionBehaviors.Count > 0
                ? videoSelectionBehaviors.Dequeue()(cancellationToken)
                : Task.FromResult<VideoCaptureSelection?>(null);
        }

        public async Task WaitForCallCountAsync(int expectedCount, TimeSpan? timeout = null)
        {
            var effectiveTimeout = timeout ?? TimeSpan.FromSeconds(3);
            var startedAt = DateTime.UtcNow;

            while (CallCount < expectedCount && DateTime.UtcNow - startedAt < effectiveTimeout)
            {
                await Task.Delay(25);
            }

            CallCount.Should().BeGreaterOrEqualTo(expectedCount);
        }

        public async Task WaitForVideoSelectionCallCountAsync(int expectedCount, TimeSpan? timeout = null)
        {
            var effectiveTimeout = timeout ?? TimeSpan.FromSeconds(3);
            var startedAt = DateTime.UtcNow;

            while (VideoSelectionCallCount < expectedCount && DateTime.UtcNow - startedAt < effectiveTimeout)
            {
                await Task.Delay(25);
            }

            VideoSelectionCallCount.Should().BeGreaterOrEqualTo(expectedCount);
        }
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
        public HotkeySettings? NextResult { get; set; }

        public int EditCalls { get; private set; }

        public HotkeySettings? LastInput { get; private set; }

        public HotkeySettings? Edit(HotkeySettings currentSettings)
        {
            EditCalls++;
            LastInput = currentSettings.Clone();
            return NextResult?.Clone() ?? currentSettings.Clone();
        }
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
        public string? LastText { get; private set; }

        public void SetText(string text)
        {
            LastText = text;
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
        public Ipv4SocketSnapshotResult NextResult { get; set; } =
            new(DateTimeOffset.UnixEpoch, [], 0, 0, 0);

        public Task<Ipv4SocketSnapshotResult> CaptureAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(NextResult);
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
        public WindowsTelemetryStatus Status { get; set; } = new(false, "Telemetry status ready.");

        public WindowsTelemetryResult ApplyResult { get; set; } = new(true, true, "Applied.");

        public WindowsTelemetryResult RestoreResult { get; set; } = new(true, true, "Restored.");

        public WindowsTelemetryStatus GetStatus() => Status;

        public Task<WindowsTelemetryResult> ApplyAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(ApplyResult);

        public Task<WindowsTelemetryResult> RestoreAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(RestoreResult);
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
