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
using System.Text.RegularExpressions;
using System.Windows.Data;
using System.Windows.Input;

namespace MultiTool.App.Tests;


public sealed partial class MainWindowViewModelSettingsFlowTests
{
    private static async Task WaitForConditionAsync(Func<bool> condition, TimeSpan? timeout = null)
    {
        var effectiveTimeout = timeout ?? TimeSpan.FromSeconds(2);
        var startedAt = DateTime.UtcNow;

        while (!condition() && DateTime.UtcNow - startedAt < effectiveTimeout)
        {
            await Task.Delay(25);
        }

        condition().Should().BeTrue();
    }

    private sealed class MainWindowViewModelTestContext
    {
        public MainWindowViewModelTestContext(
            AppSettings loadedSettings,
            TimeSpan? toolScanResultRetentionWindow = null)
        {
            SettingsStore = new FakeAppSettingsStore(loadedSettings);
            ThemeService = new FakeThemeService();
            RunAtStartupService = new FakeRunAtStartupService();
            AutoClickerController = new FakeAutoClickerController();
            ScreenshotCaptureService = new FakeScreenshotCaptureService();
            ScreenshotAreaSelectionService = new FakeScreenshotAreaSelectionService();
            ClipboardTextService = new FakeClipboardTextService();
            Ipv4SocketSnapshotService = new FakeIpv4SocketSnapshotService();
            WindowsTelemetryService = new FakeWindowsTelemetryService();
            InstallerService = new FakeInstallerService();
            ShortcutHotkeyInventoryService = new FakeShortcutHotkeyInventoryService();
            ShortcutHotkeyDialogService = new FakeShortcutHotkeyDialogService();
            HardwareInventoryService = new FakeHardwareInventoryService();
            DriverUpdateService = new FakeDriverUpdateService();

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
                new FakeMacroHotkeyAssignmentsDialogService(),
                new FakeCoordinateCaptureDialogService(),
                new FakeAboutWindowService(),
                ThemeService,
                ClipboardTextService,
                RunAtStartupService,
                new FakeMacroLibraryService(),
                InstallerService,
                new FakeAppUpdateService(),
                new FakeBrowserLauncherService(),
                new FakeFirefoxExtensionService(),
                new FakeEmptyDirectoryService(),
                ShortcutHotkeyInventoryService,
                new FakeShortcutHotkeyDisableService(),
                Ipv4SocketSnapshotService,
                new FakeMouseSensitivityService(),
                new FakeDisplayRefreshRateService(),
                HardwareInventoryService,
                DriverUpdateService,
                new FakeWindows11EeaMediaService(),
                new FakeWindowsSearchReplacementService(),
                new FakeWindowsSearchReindexService(),
                WindowsTelemetryService,
                new FakeOneDriveRemovalService(),
                new FakeEdgeRemovalService(),
                new FakeFnCtrlSwapService(),
                ShortcutHotkeyDialogService,
                new AppLaunchOptions(),
                toolScanResultRetentionWindow);
        }

        public MainWindowViewModel ViewModel { get; }

        public FakeAppSettingsStore SettingsStore { get; }

        public FakeThemeService ThemeService { get; }

        public FakeRunAtStartupService RunAtStartupService { get; }

        public FakeAutoClickerController AutoClickerController { get; }

        public FakeScreenshotCaptureService ScreenshotCaptureService { get; }

        public FakeScreenshotAreaSelectionService ScreenshotAreaSelectionService { get; }

        public FakeClipboardTextService ClipboardTextService { get; }

        public FakeIpv4SocketSnapshotService Ipv4SocketSnapshotService { get; }

        public FakeWindowsTelemetryService WindowsTelemetryService { get; }

        public FakeInstallerService InstallerService { get; }

        public FakeShortcutHotkeyInventoryService ShortcutHotkeyInventoryService { get; }

        public FakeShortcutHotkeyDialogService ShortcutHotkeyDialogService { get; }

        public FakeHardwareInventoryService HardwareInventoryService { get; }

        public FakeDriverUpdateService DriverUpdateService { get; }
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
        private static readonly byte[] TestPngBytes =
        [
            137, 80, 78, 71, 13, 10, 26, 10,
            0, 0, 0, 13, 73, 72, 68, 82,
            0, 0, 0, 1, 0, 0, 0, 1,
            8, 6, 0, 0, 0, 31, 21, 196,
            137, 0, 0, 0, 13, 73, 68, 65,
            84, 120, 156, 99, 248, 255, 255, 63,
            0, 5, 254, 2, 254, 167, 53, 129,
            132, 0, 0, 0, 0, 73, 69, 78,
            68, 174, 66, 96, 130,
        ];

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
            return Task.FromResult(CreateTestImage(outputDirectory, $"{fileNamePrefix}-full.png"));
        }

        public Task<string> CaptureAreaAsync(ScreenRectangle area, string outputDirectory, string fileNamePrefix, CancellationToken cancellationToken = default)
        {
            CaptureAreaCallCount++;
            return Task.FromResult(CreateTestImage(outputDirectory, $"{fileNamePrefix}-area.png"));
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
            LastSavedVideoPath = CreateTestVideoFile();
            VideoCaptureStateChanged?.Invoke(this, new VideoCaptureStateChangedEventArgs(false, null));
            return Task.FromResult<string?>(LastSavedVideoPath);
        }

        public void SetVideoCaptureRunning()
        {
            IsVideoCaptureRunning = true;
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

        private static string CreateTestImage(string outputDirectory, string fileName)
        {
            Directory.CreateDirectory(outputDirectory);
            var filePath = Path.Combine(outputDirectory, fileName);
            File.WriteAllBytes(filePath, TestPngBytes);
            return filePath;
        }

        private static string CreateTestVideoFile()
        {
            var filePath = Path.Combine(Path.GetTempPath(), $"multitool-test-{Guid.NewGuid():N}.mp4");
            File.WriteAllBytes(filePath, []);
            return filePath;
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
        public event EventHandler<InstallerOperationProgressChangedEventArgs>? OperationProgressChanged;

        public int GetCatalogCallCount { get; private set; }

        public int GetCleanupCatalogCallCount { get; private set; }

        public int GetPackageStatusesCallCount { get; private set; }

        public List<bool> PackageStatusIncludeUpdateChecks { get; } = [];

        public IReadOnlyList<InstallerCatalogItem> CatalogItems { get; set; } = [];

        public IReadOnlyList<InstallerCatalogItem> CleanupCatalogItems { get; set; } = [];

        public IReadOnlyList<InstallerPackageStatus> PackageStatuses { get; set; } = [];

        public Func<string, InstallerPackageAction, CancellationToken, Task<IReadOnlyList<InstallerOperationResult>>>? RunPackageOperationAsyncHandler { get; set; }

        public IReadOnlyList<InstallerCatalogItem> GetCatalog()
        {
            GetCatalogCallCount++;
            return CatalogItems;
        }

        public IReadOnlyList<InstallerCatalogItem> GetCleanupCatalog()
        {
            GetCleanupCatalogCallCount++;
            return CleanupCatalogItems;
        }

        public InstallerPackageCapabilities GetPackageCapabilities(string packageId) => new();

        public Task<InstallerEnvironmentInfo> GetEnvironmentInfoAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new InstallerEnvironmentInfo(true, "test", "Installer ready."));

        public Task<IReadOnlyList<InstallerPackageStatus>> GetPackageStatusesAsync(
            IEnumerable<string> packageIds,
            bool includeUpdateCheck = true,
            CancellationToken cancellationToken = default)
        {
            GetPackageStatusesCallCount++;
            PackageStatusIncludeUpdateChecks.Add(includeUpdateCheck);
            var packageIdSet = packageIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var statuses = PackageStatuses
                .Where(status => packageIdSet.Contains(status.PackageId))
                .ToArray();
            return Task.FromResult<IReadOnlyList<InstallerPackageStatus>>(statuses);
        }

        public Task<IReadOnlyList<InstallerOperationResult>> RunPackageOperationAsync(string packageId, InstallerPackageAction action, CancellationToken cancellationToken = default) =>
            RunPackageOperationAsyncHandler?.Invoke(packageId, action, cancellationToken)
            ?? Task.FromResult<IReadOnlyList<InstallerOperationResult>>([]);

        public Task<IReadOnlyList<InstallerOperationResult>> InstallPackagesAsync(IEnumerable<string> packageIds, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<InstallerOperationResult>>([]);

        public Task<IReadOnlyList<InstallerOperationResult>> UpgradePackagesAsync(IEnumerable<string> packageIds, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<InstallerOperationResult>>([]);

        public Task<IReadOnlyList<InstallerOperationResult>> UninstallPackagesAsync(IEnumerable<string> packageIds, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<InstallerOperationResult>>([]);

        public async Task WaitForPackageStatusCallsAsync(int expectedCount, TimeSpan? timeout = null)
        {
            var effectiveTimeout = timeout ?? TimeSpan.FromSeconds(3);
            var startedAt = DateTime.UtcNow;

            while (GetPackageStatusesCallCount < expectedCount && DateTime.UtcNow - startedAt < effectiveTimeout)
            {
                await Task.Delay(25);
            }

            GetPackageStatusesCallCount.Should().BeGreaterOrEqualTo(expectedCount);
        }

        public void RaiseOperationProgress(
            string packageId,
            string displayName,
            InstallerPackageAction action,
            string statusText,
            int? percent = null)
        {
            OperationProgressChanged?.Invoke(
                this,
                new InstallerOperationProgressChangedEventArgs(
                    packageId,
                    displayName,
                    action,
                    statusText,
                    percent));
        }
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
        public ShortcutHotkeyScanResult NextResult { get; set; } = new([], 0, []);

        public int ScanCallCount { get; private set; }

        public Task<ShortcutHotkeyScanResult> ScanAsync(
            IProgress<ShortcutHotkeyScanProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            ScanCallCount++;
            return Task.FromResult(NextResult);
        }
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
        public HardwareInventoryReport NextReport { get; set; } =
            new(
                "Test system",
                "Healthy",
                "Windows 11",
                "Test CPU",
                "16 GB",
                "Test board",
                "Test BIOS",
                [],
                [],
                [],
                [],
                [],
                [],
                [],
                []);

        public int GetReportCallCount { get; private set; }

        public Task<HardwareInventoryReport> GetReportAsync(CancellationToken cancellationToken = default)
        {
            GetReportCallCount++;
            return Task.FromResult(NextReport);
        }
    }

    private sealed class FakeDriverUpdateService : IDriverUpdateService
    {
        public DriverUpdateScanResult NextScanResult { get; set; } = new([], [], []);

        public int ScanCallCount { get; private set; }

        public IReadOnlyList<DriverHardwareInfo>? LastHardwareInventoryArgument { get; private set; }

        public Task<DriverUpdateScanResult> ScanAsync(IReadOnlyList<DriverHardwareInfo>? hardwareInventory = null, CancellationToken cancellationToken = default)
        {
            ScanCallCount++;
            LastHardwareInventoryArgument = hardwareInventory;
            return Task.FromResult(NextScanResult);
        }

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
        public int ShowCallCount { get; private set; }

        public bool LastIsCachedResult { get; private set; }

        public void Show(
            ShortcutHotkeyScanResult result,
            bool isCachedResult,
            Func<Task<ShortcutHotkeyScanResult>> rescanAsync,
            Func<IReadOnlyList<ShortcutHotkeyInfo>, Task<ShortcutHotkeyDisableOperationResult>> disableAsync)
        {
            ShowCallCount++;
            LastIsCachedResult = isCachedResult;
        }
    }
}
