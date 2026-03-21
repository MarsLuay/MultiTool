using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using MultiTool.App.Helpers;
using MultiTool.App.Localization;
using MultiTool.App.Models;
using MultiTool.App.Services;
using MultiTool.Core.Defaults;
using MultiTool.Core.Enums;
using MultiTool.Core.Models;
using MultiTool.Core.Services;
using MultiTool.Core.Validation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MultiTool.App.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private enum ToolScanResultCacheSlot
    {
        ShortcutHotkeys,
        EmptyDirectories,
        DisplayRefreshRecommendations,
        Ipv4SocketSnapshot,
        DriveSmartHealth,
        StorageBenchmark,
        HardwareInventory,
        DriverUpdates,
    }

    private readonly IAppSettingsStore settingsStore;
    private readonly SettingsValidator settingsValidator;
    private readonly IAutoClickerController autoClickerController;
    private readonly IMacroFileStore macroFileStore;
    private readonly IMacroService macroService;
    private readonly IFolderPickerService folderPickerService;
    private readonly IScreenshotCaptureService screenshotCaptureService;
    private readonly IScreenshotAreaSelectionService screenshotAreaSelectionService;
    private readonly IMacroEditorDialogService macroEditorDialogService;
    private readonly IMacroNamePromptService macroNamePromptService;
    private readonly IMacroFileDialogService macroFileDialogService;
    private readonly ITextFileSaveDialogService textFileSaveDialogService;
    private readonly IMacroHotkeyAssignmentsDialogService macroHotkeyAssignmentsDialogService;
    private readonly ICoordinateCaptureDialogService coordinateCaptureDialogService;
    private readonly IAboutWindowService aboutWindowService;
    private readonly IThemeService themeService;
    private readonly IClipboardTextService clipboardTextService;
    private readonly IRunAtStartupService runAtStartupService;
    private readonly IMacroLibraryService macroLibraryService;
    private readonly IInstallerService installerService;
    private readonly IAppUpdateService appUpdateService;
    private readonly IBrowserLauncherService browserLauncherService;
    private readonly IFirefoxExtensionService firefoxExtensionService;
    private readonly IEmptyDirectoryService emptyDirectoryService;
    private readonly IShortcutHotkeyInventoryService shortcutHotkeyInventoryService;
    private readonly IShortcutHotkeyDisableService shortcutHotkeyDisableService;
    private readonly IIpv4SocketSnapshotService ipv4SocketSnapshotService;
    private readonly IMouseSensitivityService mouseSensitivityService;
    private readonly IDisplayRefreshRateService displayRefreshRateService;
    private readonly IHardwareInventoryService hardwareInventoryService;
    private readonly IDriveSmartHealthService driveSmartHealthService;
    private readonly IStorageBenchmarkService storageBenchmarkService;
    private readonly IDriverUpdateService driverUpdateService;
    private readonly IWindows11EeaMediaService windows11EeaMediaService;
    private readonly IWindowsSearchReplacementService windowsSearchReplacementService;
    private readonly IWindowsSearchReindexService windowsSearchReindexService;
    private readonly IWindowsTelemetryService windowsTelemetryService;
    private readonly IOneDriveRemovalService oneDriveRemovalService;
    private readonly IEdgeRemovalService edgeRemovalService;
    private readonly IFnCtrlSwapService fnCtrlSwapService;
    private readonly IShortcutHotkeyDialogService shortcutHotkeyDialogService;
    private readonly AppLaunchOptions appLaunchOptions;
    private readonly ShortcutHotkeyToolsCoordinator shortcutHotkeyToolsCoordinator;
    private readonly EmptyDirectoryToolsCoordinator emptyDirectoryToolsCoordinator;
    private readonly SemaphoreSlim saveLock = new(1, 1);
    private readonly SynchronizationContext? synchronizationContext;
    private readonly Dispatcher? dispatcher;
    private const int ScreenshotTabIndex = 1;
    private readonly Dictionary<ToolScanResultCacheSlot, CancellationTokenSource> toolScanResultExpirationTokenSources = [];
    private const int InstallerTabIndex = 3;
    private const int ToolsTabIndex = 4;

    private HotkeySettings hotkeySettings = new();
    private CancellationTokenSource? mainMacroPlaybackCancellationTokenSource;
    private Task? mainMacroPlaybackTask;
    private CancellationTokenSource? pendingAutoSaveCancellationTokenSource;
    private bool initialized;
    private bool isInstallerInitializationQueued;
    private bool isInstallerStateInitialized;
    private bool isInstallerInitializationStarted;
    private bool isToolsStateInitialized;
    private bool suppressThemeChange;
    private bool suppressRunAtStartupChangeHandling;
    private string? latestScreenshotFilePath;
    private string? latestVideoFilePath;
    private DateTime latestScreenshotUpdatedAtUtc;
    private DateTime latestVideoUpdatedAtUtc;
    private InstallerSettings deferredInstallerSettings = new();
    private IReadOnlyList<DriverHardwareInfo>? lastDriverHardwareInventory;
    private ShortcutHotkeyScanResult? lastShortcutHotkeyScanResult;
    private int shortcutHotkeyScanMaxFolderCountCache;
    private Dictionary<string, int> emptyDirectoryScanMaxFolderCountCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly object screenshotHotkeySequenceSync = new();
    private CancellationTokenSource? pendingScreenshotHotkeySequenceCancellationTokenSource;
    private int pendingScreenshotHotkeyPressCount;
    private CancellationTokenSource? activeScreenshotAreaSelectionCancellationTokenSource;
    private ScreenshotMode? activeScreenshotAreaSelectionMode;
    private bool promoteActiveAreaSelectionToVideo;
    private DateTime suppressScreenshotHotkeyUntilUtc;
    private bool isMainWindowActive;
    private readonly TimeSpan toolScanResultRetentionWindow;

    private static readonly TimeSpan ScreenshotHotkeySequenceWindow = TimeSpan.FromMilliseconds(350);
    private static readonly TimeSpan ScreenshotHotkeyStopSuppressionWindow = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan DefaultToolScanResultRetentionWindow = TimeSpan.FromMinutes(10);

    public MainWindowViewModel(
        IAppSettingsStore settingsStore,
        SettingsValidator settingsValidator,
        IAutoClickerController autoClickerController,
        IMacroFileStore macroFileStore,
        IMacroService macroService,
        IFolderPickerService folderPickerService,
        IScreenshotCaptureService screenshotCaptureService,
        IScreenshotAreaSelectionService screenshotAreaSelectionService,
        IMacroEditorDialogService macroEditorDialogService,
        IMacroNamePromptService macroNamePromptService,
        IMacroFileDialogService macroFileDialogService,
        ITextFileSaveDialogService textFileSaveDialogService,
        IMacroHotkeyAssignmentsDialogService macroHotkeyAssignmentsDialogService,
        ICoordinateCaptureDialogService coordinateCaptureDialogService,
        IAboutWindowService aboutWindowService,
        IThemeService themeService,
        IClipboardTextService clipboardTextService,
        IRunAtStartupService runAtStartupService,
        IMacroLibraryService macroLibraryService,
        IInstallerService installerService,
        IAppUpdateService appUpdateService,
        IBrowserLauncherService browserLauncherService,
        IFirefoxExtensionService firefoxExtensionService,
        IEmptyDirectoryService emptyDirectoryService,
        IShortcutHotkeyInventoryService shortcutHotkeyInventoryService,
        IShortcutHotkeyDisableService shortcutHotkeyDisableService,
        IIpv4SocketSnapshotService ipv4SocketSnapshotService,
        IMouseSensitivityService mouseSensitivityService,
        IDisplayRefreshRateService displayRefreshRateService,
        IHardwareInventoryService hardwareInventoryService,
        IDriveSmartHealthService driveSmartHealthService,
        IStorageBenchmarkService storageBenchmarkService,
        IDriverUpdateService driverUpdateService,
        IWindows11EeaMediaService windows11EeaMediaService,
        IWindowsSearchReplacementService windowsSearchReplacementService,
        IWindowsSearchReindexService windowsSearchReindexService,
        IWindowsTelemetryService windowsTelemetryService,
        IOneDriveRemovalService oneDriveRemovalService,
        IEdgeRemovalService edgeRemovalService,
        IFnCtrlSwapService fnCtrlSwapService,
        IShortcutHotkeyDialogService shortcutHotkeyDialogService,
        AppLaunchOptions appLaunchOptions,
        TimeSpan? toolScanResultRetentionWindow = null)
    {
        this.settingsStore = settingsStore;
        this.settingsValidator = settingsValidator;
        this.autoClickerController = autoClickerController;
        this.macroFileStore = macroFileStore;
        this.macroService = macroService;
        this.folderPickerService = folderPickerService;
        this.screenshotCaptureService = screenshotCaptureService;
        this.screenshotAreaSelectionService = screenshotAreaSelectionService;
        this.macroEditorDialogService = macroEditorDialogService;
        this.macroNamePromptService = macroNamePromptService;
        this.macroFileDialogService = macroFileDialogService;
        this.textFileSaveDialogService = textFileSaveDialogService;
        this.macroHotkeyAssignmentsDialogService = macroHotkeyAssignmentsDialogService;
        this.coordinateCaptureDialogService = coordinateCaptureDialogService;
        this.aboutWindowService = aboutWindowService;
        this.themeService = themeService;
        this.clipboardTextService = clipboardTextService;
        this.runAtStartupService = runAtStartupService;
        this.macroLibraryService = macroLibraryService;
        this.installerService = installerService;
        this.installerService.OperationProgressChanged += InstallerService_OperationProgressChanged;
        this.appUpdateService = appUpdateService;
        this.browserLauncherService = browserLauncherService;
        this.firefoxExtensionService = firefoxExtensionService;
        this.emptyDirectoryService = emptyDirectoryService;
        this.shortcutHotkeyInventoryService = shortcutHotkeyInventoryService;
        this.shortcutHotkeyDisableService = shortcutHotkeyDisableService;
        this.ipv4SocketSnapshotService = ipv4SocketSnapshotService;
        this.mouseSensitivityService = mouseSensitivityService;
        this.displayRefreshRateService = displayRefreshRateService;
        this.hardwareInventoryService = hardwareInventoryService;
        this.driveSmartHealthService = driveSmartHealthService;
        this.storageBenchmarkService = storageBenchmarkService;
        this.driverUpdateService = driverUpdateService;
        this.windows11EeaMediaService = windows11EeaMediaService;
        this.windowsSearchReplacementService = windowsSearchReplacementService;
        this.windowsSearchReindexService = windowsSearchReindexService;
        this.windowsTelemetryService = windowsTelemetryService;
        this.oneDriveRemovalService = oneDriveRemovalService;
        this.edgeRemovalService = edgeRemovalService;
        this.fnCtrlSwapService = fnCtrlSwapService;
        this.shortcutHotkeyDialogService = shortcutHotkeyDialogService;
        this.appLaunchOptions = appLaunchOptions;
        this.toolScanResultRetentionWindow = toolScanResultRetentionWindow ?? DefaultToolScanResultRetentionWindow;
        synchronizationContext = SynchronizationContext.Current;
        dispatcher = Dispatcher.FromThread(Thread.CurrentThread);
        shortcutHotkeyToolsCoordinator = new ShortcutHotkeyToolsCoordinator(this);
        emptyDirectoryToolsCoordinator = new EmptyDirectoryToolsCoordinator(this);
        ClickerTab = new ClickerTabViewModel(this);
        ScreenshotTab = new ScreenshotTabViewModel(this);
        MacroTab = new MacroTabViewModel(this);
        InstallerTab = new InstallerTabViewModel(this);
        ToolsTab = new ToolsTabViewModel(this);
        SettingsTab = new SettingsTabViewModel(this);
        this.windows11EeaMediaService.StatusChanged += Windows11EeaMediaService_OnStatusChanged;

        MouseButtons =
        [
            ClickMouseButton.Left,
            ClickMouseButton.Right,
            ClickMouseButton.Middle,
            ClickMouseButton.Custom,
        ];
        ClickKinds = Enum.GetValues<ClickKind>();
        RepeatModes = Enum.GetValues<RepeatMode>();
        LocationModes = Enum.GetValues<ClickLocationMode>();

        MacroLogEntries =
        [
            L(AppLanguageKeys.MainMacroLogReady),
            L(AppLanguageKeys.MainMacroLogNoRecordedYet),
        ];

        SavedMacros = [];

        ActivityLogEntries =
        [
            $"{DateTime.Now:HH:mm:ss}  {L(AppLanguageKeys.MainActivityLogReady)}",
        ];

        RefreshAdminModeState();
        RefreshHotkeyLabels();
    }

    public event EventHandler? HotkeysChanged;

    public ClickerTabViewModel ClickerTab { get; }

    public ScreenshotTabViewModel ScreenshotTab { get; }

    public MacroTabViewModel MacroTab { get; }

    public InstallerTabViewModel InstallerTab { get; }

    public ToolsTabViewModel ToolsTab { get; }

    public SettingsTabViewModel SettingsTab { get; }

    public IReadOnlyList<ClickMouseButton> MouseButtons { get; }

    public IReadOnlyList<ClickKind> ClickKinds { get; }

    public IReadOnlyList<RepeatMode> RepeatModes { get; }

    public IReadOnlyList<ClickLocationMode> LocationModes { get; }

    public ObservableCollection<string> MacroLogEntries { get; }

    public ObservableCollection<string> ActivityLogEntries { get; }

    public ObservableCollection<SavedMacroEntry> SavedMacros { get; }

    public bool IsCustomKeySelected => SelectedMouseButton == ClickMouseButton.Custom;

    public bool UsesMousePositionSettings =>
        SelectedMouseButton != ClickMouseButton.Custom || CustomInputKind == MultiTool.Core.Enums.CustomInputKind.MouseButton;

    public bool IsRepeatCountEnabled => SelectedRepeatMode == RepeatMode.Count;

    public bool HasLatestScreenshot => LatestScreenshotPreview is not null;
    public bool HasLatestVideo => !string.IsNullOrWhiteSpace(LatestVideoPath);
    public bool IsLatestMediaVideo => HasLatestVideo && (!HasLatestScreenshot || latestVideoUpdatedAtUtc >= latestScreenshotUpdatedAtUtc);
    public Uri? LatestVideoSource =>
        string.IsNullOrWhiteSpace(LatestVideoPath)
            ? null
            : new Uri(LatestVideoPath, UriKind.Absolute);

    public bool HasSavedMacros => SavedMacros.Count > 0;

    public bool IsWindowPinned => IsTopMost;

    public string ClickerHotkeyDisplay => hotkeySettings.Toggle.DisplayName;

    public string PinWindowHotkeyLabel => GetPinWindowHotkeyDisplay();

    public bool ShouldShowAdminModeBanner => !IsRunningAsAdministrator && !isAdminBannerDismissed;

    private bool isAdminBannerDismissed;

    [RelayCommand]
    private void DismissAdminBanner()
    {
        isAdminBannerDismissed = true;
        OnPropertyChanged(nameof(ShouldShowAdminModeBanner));
    }

    public string AdminModeBannerText =>
        IsRunningAsAdministrator
            ? L(AppLanguageKeys.MainAdminBannerAdmin)
            : L(AppLanguageKeys.MainAdminBannerNotAdmin);

    public string HotkeyEditToolTip => L(AppLanguageKeys.HotkeyEditToolTip);
    public string PinWindowHotkeyFieldLabelText => L(AppLanguageKeys.HotkeySettingsPinWindowLabel);
    public string HotkeyResetButtonText => L(AppLanguageKeys.HotkeySettingsResetButton);
    public string IntervalLabelText => L(AppLanguageKeys.MainIntervalLabel);
    public string HoursLabelText => L(AppLanguageKeys.MainHoursLabel);
    public string MinutesLabelText => L(AppLanguageKeys.MainMinutesLabel);
    public string SecondsLabelText => L(AppLanguageKeys.MainSecondsLabel);
    public string MillisecondsLabelText => L(AppLanguageKeys.MainMillisecondsLabel);
    public string RandomTimingLabelText => L(AppLanguageKeys.MainRandomTimingLabel);
    public string RandomTimingVarianceLabelText => L(AppLanguageKeys.MainRandomTimingVarianceLabel);
    public string RandomTimingHelperText => L(AppLanguageKeys.MainRandomTimingHelperText);
    public string RepeatLabelText => L(AppLanguageKeys.MainRepeatLabel);
    public string PositionLabelText => L(AppLanguageKeys.MainPositionLabel);
    public string XLabelText => L(AppLanguageKeys.MainXLabel);
    public string YLabelText => L(AppLanguageKeys.MainYLabel);
    public string NameLabelText => L(AppLanguageKeys.MainNameLabel);
    public string PlayCountLabelText => L(AppLanguageKeys.MainPlayCountLabel);
    public string MacroInfiniteLabelText => L(AppLanguageKeys.MainMacroInfiniteLabel);
    public string MacroInfiniteHelperText => F(AppLanguageKeys.MainMacroInfiniteHelperFormat, MacroHotkeyDisplay);
    public string RecordMouseMovementLabelText => L(AppLanguageKeys.MainRecordMouseMovementLabel);
    public string PlayHotkeyLabelText => L(AppLanguageKeys.MainPlayHotkeyLabel);
    public string RecordHotkeyLabelText => L(AppLanguageKeys.MainRecordHotkeyLabel);
    public string SavedLabelText => L(AppLanguageKeys.MainSavedLabel);
    public string NoAssignedMacroShortcutsText => L(AppLanguageKeys.MainNoAssignedMacroShortcuts);
    public string MouseSensitivityVerySlowLabelText => L(AppLanguageKeys.MouseSensitivityVerySlow);
    public string MouseSensitivitySlowLabelText => L(AppLanguageKeys.MouseSensitivitySlow);
    public string MouseSensitivityBalancedLabelText => L(AppLanguageKeys.MouseSensitivityBalanced);
    public string MouseSensitivityFastLabelText => L(AppLanguageKeys.MouseSensitivityFast);
    public string MouseSensitivityVeryFastLabelText => L(AppLanguageKeys.MouseSensitivityVeryFast);
    public string InputLabelText => L(AppLanguageKeys.MainInputLabel);
    public string TypeLabelText => L(AppLanguageKeys.MainTypeLabel);
    public string CustomInputLabelText => L(AppLanguageKeys.MainCustomInputLabel);
    public string FolderLabelText => L(AppLanguageKeys.MainFolderLabel);
    public string PrefixLabelText => L(AppLanguageKeys.MainPrefixLabel);
    public string ScreenshotHelperText => L(AppLanguageKeys.MainScreenshotHelperText);
    public string WaitingForKeyText => L(AppLanguageKeys.HotkeySettingsWaitingKey);
    public string LatestScreenshotPlaceholderText => L(AppLanguageKeys.MainLatestScreenshotPlaceholder);
    public string CustomKeyOrMousePromptText => L(AppLanguageKeys.MainCustomKeyOrMousePrompt);

    public string ScreenshotHotkeyLabelText => L(AppLanguageKeys.HotkeyLabel);

    public string CaptureButtonText => L(AppLanguageKeys.CaptureButton);
    public string BrowseButtonText => L(AppLanguageKeys.BrowseButton);
    public string CaptureScreenButtonText => L(AppLanguageKeys.CaptureScreenButton);
    public string OpenFolderButtonText => L(AppLanguageKeys.OpenFolderButton);
    public string NewMacroButtonText => L(AppLanguageKeys.NewMacroButton);
    public string RecordButtonText => L(AppLanguageKeys.RecordButton);
    public string StopButtonText => L(AppLanguageKeys.StopButton);
    public string PlayButtonText => IsMainMacroInfinitePlaybackActive ? StopButtonText : L(AppLanguageKeys.PlayButton);
    public string SaveButtonText => L(AppLanguageKeys.SaveButton);
    public string LoadButtonText => L(AppLanguageKeys.LoadButton);
    public string RefreshButtonText => L(AppLanguageKeys.RefreshButton);
    public string LoadSelectedButtonText => L(AppLanguageKeys.LoadSelectedButton);
    public string EditSelectedButtonText => L(AppLanguageKeys.EditSelectedButton);

    public bool IsRandomTimingVarianceEnabled => IsRandomTimingEnabled;
    public bool IsMacroPlaybackCountEnabled => !IsMacroPlaybackInfinite;

    [ObservableProperty]
    private string windowTitle = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.MainWindowTitleDefault);

    [ObservableProperty]
    private string statusMessage = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.MainStatusLoadingSettings);

    [ObservableProperty]
    private int selectedMainTabIndex;

    [ObservableProperty]
    private int hours;

    [ObservableProperty]
    private int minutes;

    [ObservableProperty]
    private int seconds;

    [ObservableProperty]
    private int milliseconds = 1;

    [ObservableProperty]
    private bool isRandomTimingEnabled;

    [ObservableProperty]
    private int randomTimingVarianceMilliseconds = 25;

    [ObservableProperty]
    private ClickMouseButton selectedMouseButton = ClickMouseButton.Left;

    [ObservableProperty]
    private CustomInputKind customInputKind;

    [ObservableProperty]
    private int customKeyVirtualKey;

    [ObservableProperty]
    private string customKeyDisplayText = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.MainCustomKeyPrompt);

    [ObservableProperty]
    private ClickMouseButton customMouseButton = ClickMouseButton.Left;

    [ObservableProperty]
    private ClickKind selectedClickKind = ClickKind.Single;

    [ObservableProperty]
    private RepeatMode selectedRepeatMode = RepeatMode.Infinite;

    [ObservableProperty]
    private ClickLocationMode selectedLocationMode = ClickLocationMode.CurrentCursor;

    [ObservableProperty]
    private int fixedX;

    [ObservableProperty]
    private int fixedY;

    [ObservableProperty]
    private int repeatCount = 1;

    [ObservableProperty]
    private bool isTopMost;

    [ObservableProperty]
    private bool isRunning;

    [ObservableProperty]
    private string screenshotFolderPath = ScreenshotSettings.GetDefaultSaveFolderPath();

    [ObservableProperty]
    private string screenshotFilePrefix = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.MainScreenshotFilePrefixDefault);

    [ObservableProperty]
    private string screenshotHotkeyDisplay = ScreenshotSettings.DefaultCaptureDisplayName;

    [ObservableProperty]
    private int screenshotHotkeyVirtualKey = ScreenshotSettings.DefaultCaptureVirtualKey;

    [ObservableProperty]
    private HotkeyModifiers screenshotHotkeyModifiers;

    [ObservableProperty]
    private string screenshotStatusMessage = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.MainScreenshotStatusReady);

    [ObservableProperty]
    private ImageSource? latestScreenshotPreview;

    [ObservableProperty]
    private string latestScreenshotCaption = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.MainLatestScreenshotNone);
    [ObservableProperty]
    private string latestVideoCaption = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.MainLatestVideoNone);
    [ObservableProperty]
    private string? latestVideoPath;

    [ObservableProperty]
    private string macroName = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.MainMacroNameDefault);

    [ObservableProperty]
    private string macroHotkeyDisplay = MacroSettings.DefaultPlayDisplayName;

    [ObservableProperty]
    private int macroHotkeyVirtualKey = MacroSettings.DefaultPlayVirtualKey;

    [ObservableProperty]
    private HotkeyModifiers macroHotkeyModifiers;

    [ObservableProperty]
    private string macroRecordHotkeyDisplay = MacroSettings.DefaultRecordDisplayName;

    [ObservableProperty]
    private int macroRecordHotkeyVirtualKey = MacroSettings.DefaultRecordVirtualKey;

    [ObservableProperty]
    private HotkeyModifiers macroRecordHotkeyModifiers;

    [ObservableProperty]
    private int macroPlaybackCount = 1;

    [ObservableProperty]
    private bool isMacroPlaybackInfinite;

    [ObservableProperty]
    private bool recordMacroMouseMovement = true;

    [ObservableProperty]
    private bool hasRecordedMacro;

    [ObservableProperty]
    private bool isMacroRecording;

    [ObservableProperty]
    private bool isMacroPlaying;

    [ObservableProperty]
    private string macroSummaryText = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.MainMacroSummaryNoRecorded);

    [ObservableProperty]
    private string macroStatusMessage = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.MainMacroStatusReady);

    [ObservableProperty]
    private SavedMacroEntry? selectedSavedMacro;

    [ObservableProperty]
    private bool isDarkMode;

    [ObservableProperty]
    private bool isCtrlWheelResizeEnabled = true;

    [ObservableProperty]
    private bool isRunAtStartupEnabled;

    [ObservableProperty]
    private bool isAutoHideOnStartupEnabled;

    [ObservableProperty]
    private bool isShortcutOverrideEnabled;

    [ObservableProperty]
    private bool isSillyModeEnabled;

    [ObservableProperty]
    private string settingsStatusMessage = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.MainSettingsStatusInitial);

    [ObservableProperty]
    private bool isRunningAsAdministrator;

    public bool ShouldAutoHideOnStartup => appLaunchOptions.IsStartupLaunch && IsAutoHideOnStartupEnabled;
    public bool IsTabPerformanceLoggingEnabled => appLaunchOptions.IsTabPerformanceLoggingEnabled;

    private AppLanguage CurrentLanguage => IsSillyModeEnabled ? AppLanguage.CatSpeak : AppLanguage.English;

    private string L(string key) => AppLanguageStrings.Get(key, CurrentLanguage);

    private string F(string key, params object[] args) => AppLanguageStrings.Format(key, CurrentLanguage, args);

    private static string PluralSuffix(int count) => count == 1 ? string.Empty : "s";

    public string AppearanceHelperText => L(AppLanguageKeys.AppearanceHelperText);

    public string ClickerTabHeaderText => L(AppLanguageKeys.ClickerTabHeader);

    public string ClickerHotkeyLabelText => L(AppLanguageKeys.ClickerHotkeyLabel);

    public string ClickerForceStopHelperText => L(AppLanguageKeys.ClickerForceStopHelperText);

    public string ScreenshotTabHeaderText => L(AppLanguageKeys.ScreenshotTabHeader);

    public string MacroTabHeaderText => L(AppLanguageKeys.MacroTabHeader);

    public string MacroSetShortcutButtonText => L(AppLanguageKeys.MacroSetShortcutButton);

    public string MacroEditShortcutsButtonText => L(AppLanguageKeys.MacroEditShortcutsButton);

    public string ToolsTabHeaderText => L(AppLanguageKeys.ToolsTabHeader);

    public string NicheToolsHeaderText => L(AppLanguageKeys.NicheToolsHeader);

    public string ShortcutKeyExplorerHeaderText => L(AppLanguageKeys.ShortcutKeyExplorerHeader);

    public string ShortcutExplorerButtonText => L(AppLanguageKeys.ShortcutExplorerButton);

    public string InstallerTabHeaderText => L(AppLanguageKeys.InstallerTabHeader);

    public string InstallerPackagePickerDescriptionText => L(AppLanguageKeys.InstallerPackagePickerDescription);
    public string InstallerSearchLabelText => L(AppLanguageKeys.InstallerSearchLabel);

    public string SettingsTabHeaderText => L(AppLanguageKeys.SettingsTabHeader);

    public string AppearanceHeaderText => L(AppLanguageKeys.AppearanceHeader);

    public string DarkModeLabelText => L(AppLanguageKeys.DarkModeLabel);

    public string CtrlWheelResizeLabelText => L(AppLanguageKeys.CtrlWheelResizeLabel);

    public string AlwaysOnTopLabelText => L(AppLanguageKeys.AlwaysOnTopLabel);

    public string CatTranslatorLabelText => L(AppLanguageKeys.CatTranslatorLabel);

    public string RunAtStartupLabelText => L(AppLanguageKeys.RunAtStartupLabel);

    public string AutoHideOnStartupLabelText => L(AppLanguageKeys.AutoHideOnStartupLabel);

    public string ShortcutOverrideLabelText => L(AppLanguageKeys.ShortcutOverrideLabel);

    public string ShortcutOverrideHelperText => L(AppLanguageKeys.ShortcutOverrideHelperText);

    public string ResetAllSettingsButtonText => L(AppLanguageKeys.ResetAllSettingsButton);

    public string BugCheckingHeaderText => L(AppLanguageKeys.BugCheckingHeader);

    public string BugCheckingHelperText => L(AppLanguageKeys.BugCheckingHelperText);

    public string CopyLogButtonText => L(AppLanguageKeys.CopyLogButton);

    public bool ShouldSuppressGlobalHotkeys => false;

    private bool IsMainMacroInfinitePlaybackActive =>
        mainMacroPlaybackCancellationTokenSource is not null && mainMacroPlaybackTask is not null;

    public void SetMainWindowActive(bool isActive)
    {
        isMainWindowActive = isActive;
    }

    internal bool ShouldTrackTabPerformance(int tabIndex) =>
        appLaunchOptions.IsTabPerformanceLoggingEnabled
        && tabIndex is InstallerTabIndex or ToolsTabIndex;

    internal string DescribeMainTab(int tabIndex) =>
        tabIndex switch
        {
            0 => "Clicker",
            ScreenshotTabIndex => "Screenshot",
            2 => "Macro",
            InstallerTabIndex => "Install",
            ToolsTabIndex => "Tools",
            5 => "Settings",
            _ => $"Tab {tabIndex}",
        };

    private void LogTabPerformance(int tabIndex, string phase, TimeSpan elapsed, string? details = null)
    {
        if (!ShouldTrackTabPerformance(tabIndex))
        {
            return;
        }

        var detailsSuffix = string.IsNullOrWhiteSpace(details) ? string.Empty : $" | {details}";
        AppLog.Info(
            $"TabPerf | Tab={DescribeMainTab(tabIndex)} ({tabIndex}) | Phase={phase} | Elapsed={elapsed.TotalMilliseconds:0.0} ms{detailsSuffix}");
    }

    partial void OnSelectedMainTabIndexChanged(int value)
    {
        var selectionStopwatch = ShouldTrackTabPerformance(value) ? Stopwatch.StartNew() : null;
        HandleScreenshotTabSelectionChanged(value);

        switch (value)
        {
            case InstallerTabIndex:
                QueueInstallerInitialization();
                break;
            case ToolsTabIndex:
                InitializeToolsState();
                break;
        }

        if (selectionStopwatch is not null)
        {
            selectionStopwatch.Stop();
            LogTabPerformance(
                value,
                "SelectedMainTabIndexChanged",
                selectionStopwatch.Elapsed,
                $"InstallerInitialized={isInstallerStateInitialized}; InstallerQueued={isInstallerInitializationQueued}; ToolsInitialized={isToolsStateInitialized}");
        }
    }

}
