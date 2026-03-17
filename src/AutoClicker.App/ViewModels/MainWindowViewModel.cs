using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AutoClicker.App.Helpers;
using AutoClicker.App.Localization;
using AutoClicker.App.Models;
using AutoClicker.App.Services;
using AutoClicker.Core.Defaults;
using AutoClicker.Core.Enums;
using AutoClicker.Core.Models;
using AutoClicker.Core.Services;
using AutoClicker.Core.Validation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AutoClicker.App.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly IAppSettingsStore settingsStore;
    private readonly SettingsValidator settingsValidator;
    private readonly IAutoClickerController autoClickerController;
    private readonly IMacroFileStore macroFileStore;
    private readonly IMacroService macroService;
    private readonly IFolderPickerService folderPickerService;
    private readonly IScreenshotCaptureService screenshotCaptureService;
    private readonly IScreenshotOptionsDialogService screenshotOptionsDialogService;
    private readonly IScreenshotAreaSelectionService screenshotAreaSelectionService;
    private readonly IMacroEditorDialogService macroEditorDialogService;
    private readonly IMacroNamePromptService macroNamePromptService;
    private readonly IMacroFileDialogService macroFileDialogService;
    private readonly IHotkeySettingsDialogService hotkeySettingsDialogService;
    private readonly IMacroHotkeyAssignmentsDialogService macroHotkeyAssignmentsDialogService;
    private readonly ICoordinateCaptureDialogService coordinateCaptureDialogService;
    private readonly IAboutWindowService aboutWindowService;
    private readonly IThemeService themeService;
    private readonly IClipboardTextService clipboardTextService;
    private readonly IMacroLibraryService macroLibraryService;
    private readonly IInstallerService installerService;
    private readonly IAppUpdateService appUpdateService;
    private readonly IBrowserLauncherService browserLauncherService;
    private readonly IFirefoxExtensionService firefoxExtensionService;
    private readonly IEmptyDirectoryService emptyDirectoryService;
    private readonly IShortcutHotkeyInventoryService shortcutHotkeyInventoryService;
    private readonly IMouseSensitivityService mouseSensitivityService;
    private readonly IDisplayRefreshRateService displayRefreshRateService;
    private readonly IHardwareInventoryService hardwareInventoryService;
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
    private readonly SemaphoreSlim saveLock = new(1, 1);
    private readonly SynchronizationContext? synchronizationContext;

    private HotkeySettings hotkeySettings = new();
    private CancellationTokenSource? pendingAutoSaveCancellationTokenSource;
    private bool initialized;
    private bool suppressThemeChange;
    private DateTime latestScreenshotUpdatedAtUtc;
    private DateTime latestVideoUpdatedAtUtc;
    private int shortcutHotkeyScanMaxFolderCountCache;
    private Dictionary<string, int> emptyDirectoryScanMaxFolderCountCache = new(StringComparer.OrdinalIgnoreCase);

    public MainWindowViewModel(
        IAppSettingsStore settingsStore,
        SettingsValidator settingsValidator,
        IAutoClickerController autoClickerController,
        IMacroFileStore macroFileStore,
        IMacroService macroService,
        IFolderPickerService folderPickerService,
        IScreenshotCaptureService screenshotCaptureService,
        IScreenshotOptionsDialogService screenshotOptionsDialogService,
        IScreenshotAreaSelectionService screenshotAreaSelectionService,
        IMacroEditorDialogService macroEditorDialogService,
        IMacroNamePromptService macroNamePromptService,
        IMacroFileDialogService macroFileDialogService,
        IHotkeySettingsDialogService hotkeySettingsDialogService,
        IMacroHotkeyAssignmentsDialogService macroHotkeyAssignmentsDialogService,
        ICoordinateCaptureDialogService coordinateCaptureDialogService,
        IAboutWindowService aboutWindowService,
        IThemeService themeService,
        IClipboardTextService clipboardTextService,
        IMacroLibraryService macroLibraryService,
        IInstallerService installerService,
        IAppUpdateService appUpdateService,
        IBrowserLauncherService browserLauncherService,
        IFirefoxExtensionService firefoxExtensionService,
        IEmptyDirectoryService emptyDirectoryService,
        IShortcutHotkeyInventoryService shortcutHotkeyInventoryService,
        IMouseSensitivityService mouseSensitivityService,
        IDisplayRefreshRateService displayRefreshRateService,
        IHardwareInventoryService hardwareInventoryService,
        IDriverUpdateService driverUpdateService,
        IWindows11EeaMediaService windows11EeaMediaService,
        IWindowsSearchReplacementService windowsSearchReplacementService,
        IWindowsSearchReindexService windowsSearchReindexService,
        IWindowsTelemetryService windowsTelemetryService,
        IOneDriveRemovalService oneDriveRemovalService,
        IEdgeRemovalService edgeRemovalService,
        IFnCtrlSwapService fnCtrlSwapService,
        IShortcutHotkeyDialogService shortcutHotkeyDialogService,
        AppLaunchOptions appLaunchOptions)
    {
        this.settingsStore = settingsStore;
        this.settingsValidator = settingsValidator;
        this.autoClickerController = autoClickerController;
        this.macroFileStore = macroFileStore;
        this.macroService = macroService;
        this.folderPickerService = folderPickerService;
        this.screenshotCaptureService = screenshotCaptureService;
        this.screenshotOptionsDialogService = screenshotOptionsDialogService;
        this.screenshotAreaSelectionService = screenshotAreaSelectionService;
        this.macroEditorDialogService = macroEditorDialogService;
        this.macroNamePromptService = macroNamePromptService;
        this.macroFileDialogService = macroFileDialogService;
        this.hotkeySettingsDialogService = hotkeySettingsDialogService;
        this.macroHotkeyAssignmentsDialogService = macroHotkeyAssignmentsDialogService;
        this.coordinateCaptureDialogService = coordinateCaptureDialogService;
        this.aboutWindowService = aboutWindowService;
        this.themeService = themeService;
        this.clipboardTextService = clipboardTextService;
        this.macroLibraryService = macroLibraryService;
        this.installerService = installerService;
        this.appUpdateService = appUpdateService;
        this.browserLauncherService = browserLauncherService;
        this.firefoxExtensionService = firefoxExtensionService;
        this.emptyDirectoryService = emptyDirectoryService;
        this.shortcutHotkeyInventoryService = shortcutHotkeyInventoryService;
        this.mouseSensitivityService = mouseSensitivityService;
        this.displayRefreshRateService = displayRefreshRateService;
        this.hardwareInventoryService = hardwareInventoryService;
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
        synchronizationContext = SynchronizationContext.Current;
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
        InitializeInstallerState();
        InitializeToolsState();

        ActivityLogEntries =
        [
            $"{DateTime.Now:HH:mm:ss}  {L(AppLanguageKeys.MainActivityLogReady)}",
        ];

        RefreshAdminModeState();
        RefreshHotkeyLabels();
    }

    public event EventHandler? HotkeysChanged;

    public IReadOnlyList<ClickMouseButton> MouseButtons { get; }

    public IReadOnlyList<ClickKind> ClickKinds { get; }

    public IReadOnlyList<RepeatMode> RepeatModes { get; }

    public IReadOnlyList<ClickLocationMode> LocationModes { get; }

    public ObservableCollection<string> MacroLogEntries { get; }

    public ObservableCollection<string> ActivityLogEntries { get; }

    public ObservableCollection<SavedMacroEntry> SavedMacros { get; }

    public bool IsCustomKeySelected => SelectedMouseButton == ClickMouseButton.Custom;

    public bool UsesMousePositionSettings =>
        SelectedMouseButton != ClickMouseButton.Custom || CustomInputKind == AutoClicker.Core.Enums.CustomInputKind.MouseButton;

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
    public string IntervalLabelText => L(AppLanguageKeys.MainIntervalLabel);
    public string HoursLabelText => L(AppLanguageKeys.MainHoursLabel);
    public string MinutesLabelText => L(AppLanguageKeys.MainMinutesLabel);
    public string SecondsLabelText => L(AppLanguageKeys.MainSecondsLabel);
    public string MillisecondsLabelText => L(AppLanguageKeys.MainMillisecondsLabel);
    public string RepeatLabelText => L(AppLanguageKeys.MainRepeatLabel);
    public string PositionLabelText => L(AppLanguageKeys.MainPositionLabel);
    public string XLabelText => L(AppLanguageKeys.MainXLabel);
    public string YLabelText => L(AppLanguageKeys.MainYLabel);
    public string NameLabelText => L(AppLanguageKeys.MainNameLabel);
    public string PlayCountLabelText => L(AppLanguageKeys.MainPlayCountLabel);
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
    public string PlayButtonText => L(AppLanguageKeys.PlayButton);
    public string SaveButtonText => L(AppLanguageKeys.SaveButton);
    public string LoadButtonText => L(AppLanguageKeys.LoadButton);
    public string RefreshButtonText => L(AppLanguageKeys.RefreshButton);
    public string LoadSelectedButtonText => L(AppLanguageKeys.LoadSelectedButton);
    public string EditSelectedButtonText => L(AppLanguageKeys.EditSelectedButton);

    [ObservableProperty]
    private string windowTitle = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.MainWindowTitleDefault);

    [ObservableProperty]
    private string statusMessage = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.MainStatusLoadingSettings);

    [ObservableProperty]
    private int hours;

    [ObservableProperty]
    private int minutes;

    [ObservableProperty]
    private int seconds;

    [ObservableProperty]
    private int milliseconds = 1;

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
    private string macroRecordHotkeyDisplay = MacroSettings.DefaultRecordDisplayName;

    [ObservableProperty]
    private int macroRecordHotkeyVirtualKey = MacroSettings.DefaultRecordVirtualKey;

    [ObservableProperty]
    private int macroPlaybackCount = 1;

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
    private bool isAutoHideOnStartupEnabled;

    [ObservableProperty]
    private bool isSillyModeEnabled;

    [ObservableProperty]
    private string settingsStatusMessage = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.MainSettingsStatusInitial);

    [ObservableProperty]
    private bool isRunningAsAdministrator;

    public bool ShouldAutoHideOnStartup => appLaunchOptions.IsStartupLaunch && IsAutoHideOnStartupEnabled;

    private AppLanguage CurrentLanguage => IsSillyModeEnabled ? AppLanguage.CatSpeak : AppLanguage.English;

    private string L(string key) => AppLanguageStrings.Get(key, CurrentLanguage);

    private string F(string key, params object[] args) => AppLanguageStrings.Format(key, CurrentLanguage, args);

    private static string PluralSuffix(int count) => count == 1 ? string.Empty : "s";

    public string AppearanceHelperText => L(AppLanguageKeys.AppearanceHelperText);

    public string ClickerTabHeaderText => L(AppLanguageKeys.ClickerTabHeader);

    public string ClickerHotkeyLabelText => L(AppLanguageKeys.ClickerHotkeyLabel);

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

    public string AlwaysOnTopLabelText => L(AppLanguageKeys.AlwaysOnTopLabel);

    public string CatTranslatorLabelText => L(AppLanguageKeys.CatTranslatorLabel);

    public string AutoHideOnStartupLabelText => L(AppLanguageKeys.AutoHideOnStartupLabel);

    public string ResetAllSettingsButtonText => L(AppLanguageKeys.ResetAllSettingsButton);

    public string BugCheckingHeaderText => L(AppLanguageKeys.BugCheckingHeader);

    public string BugCheckingHelperText => L(AppLanguageKeys.BugCheckingHelperText);

    public string CopyLogButtonText => L(AppLanguageKeys.CopyLogButton);

    public bool ShouldSuppressGlobalHotkeys => false;

    public async Task InitializeAsync()
    {
        if (initialized)
        {
            return;
        }

        var settings = await settingsStore.LoadAsync();
        ApplySettings(settings);
        RefreshSavedMacrosInternal();
        StatusMessage = L(AppLanguageKeys.MainStatusReady);
        AddActivityLog(L(AppLanguageKeys.MainActivitySettingsLoaded));
        initialized = true;
        StartInstallerInitialization();
    }

    [RelayCommand]
    private async Task ToggleAsync()
    {
        if (!autoClickerController.IsRunning)
        {
            var validation = settingsValidator.ValidateClickSettings(BuildClickSettings());
            if (!validation.IsValid)
            {
                StatusMessage = validation.Summary;
                return;
            }
        }

        await autoClickerController.ToggleAsync(BuildClickSettings());
        UpdateRunningState(autoClickerController.IsRunning);
        StatusMessage = autoClickerController.IsRunning
            ? L(AppLanguageKeys.MainStatusClicking)
            : L(AppLanguageKeys.MainStatusAutomationStopped);
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        await SaveSettingsAsync(L(AppLanguageKeys.MainStatusSettingsSaved), updateStatusOnSuccess: true);
    }

    [RelayCommand]
    private void CaptureCoordinates()
    {
        var point = coordinateCaptureDialogService.Capture();
        if (point is null)
        {
            return;
        }

        FixedX = point.Value.X;
        FixedY = point.Value.Y;
        SelectedLocationMode = ClickLocationMode.FixedPoint;
        StatusMessage = F(AppLanguageKeys.MainStatusCapturedCoordinatesFormat, FixedX, FixedY);
    }

    [RelayCommand]
    private void OpenAbout()
    {
        aboutWindowService.Show();
    }

    [RelayCommand]
    private async Task CaptureScreenshotAsync()
        => await PerformScreenshotAsync(ScreenshotMode.FullScreen);

    [RelayCommand]
    private void OpenScreenshotFolder()
    {
        try
        {
            Directory.CreateDirectory(ScreenshotFolderPath);
            Process.Start(
                new ProcessStartInfo
                {
                    FileName = ScreenshotFolderPath,
                    UseShellExecute = true,
                });

            ScreenshotStatusMessage = L(AppLanguageKeys.MainScreenshotStatusOpenedFolder);
            AddScreenshotLog(ScreenshotStatusMessage);
        }
        catch (Exception ex)
        {
            ScreenshotStatusMessage = F(AppLanguageKeys.MainScreenshotStatusOpenFolderFailedFormat, ex.Message);
            AddScreenshotLog(ScreenshotStatusMessage);
        }
    }

    private bool CanNewMacro => HasRecordedMacro && !IsMacroRecording && !IsMacroPlaying;

    [RelayCommand(CanExecute = nameof(CanNewMacro))]
    private void NewMacro()
    {
        macroService.Clear();
        MacroName = L(AppLanguageKeys.MainMacroNameDefault);
        HasRecordedMacro = false;
        MacroSummaryText = L(AppLanguageKeys.MainMacroSummaryNoRecorded);
        MacroStatusMessage = L(AppLanguageKeys.MainMacroStatusStartedNew);
        AddMacroLog(MacroStatusMessage);
    }

    private bool CanStartMacroRecording => !IsMacroRecording && !IsMacroPlaying;

    [RelayCommand(CanExecute = nameof(CanStartMacroRecording))]
    private void StartMacroRecording()
    {
        try
        {
            var name = string.IsNullOrWhiteSpace(MacroName) ? L(AppLanguageKeys.MainMacroNameDefault) : MacroName.Trim();
            macroService.StartRecording(name, RecordMacroMouseMovement);
            MacroName = name;
            IsMacroRecording = true;
            HasRecordedMacro = false;
            MacroSummaryText = F(AppLanguageKeys.MainMacroSummaryRecordingFormat, name);
            MacroStatusMessage = RecordMacroMouseMovement
                ? F(AppLanguageKeys.MainMacroStatusRecordingWithMouseFormat, name)
                : F(AppLanguageKeys.MainMacroStatusRecordingWithoutMouseFormat, name);
            AddMacroLog(F(AppLanguageKeys.MainMacroLogStartedRecordingFormat, name));
        }
        catch (Exception ex)
        {
            MacroStatusMessage = F(AppLanguageKeys.MainMacroStatusStartRecordingFailedFormat, ex.Message);
            AddMacroLog(MacroStatusMessage);
        }
    }

    private async Task ToggleMacroRecordingAsync()
    {
        if (IsMacroRecording)
        {
            StopMacroRecording();
            return;
        }

        if (IsMacroPlaying)
        {
            MacroStatusMessage = L(AppLanguageKeys.MainMacroStatusCannotRecordWhilePlaying);
            AddMacroLog(L(AppLanguageKeys.MainMacroLogRecordHotkeyIgnoredPlaying));
            return;
        }

        StartMacroRecording();
        await Task.CompletedTask;
    }

    private bool CanStopMacroRecording => IsMacroRecording;

    [RelayCommand(CanExecute = nameof(CanStopMacroRecording))]
    private void StopMacroRecording()
    {
        try
        {
            var macro = macroService.StopRecording();
            IsMacroRecording = false;
            HasRecordedMacro = macro.Events.Count > 0;
            MacroName = macro.Name;
            MacroSummaryText = HasRecordedMacro
                ? F(AppLanguageKeys.MainMacroSummaryRecordedFormat, macro.Name, macro.Events.Count, macro.Duration.TotalMilliseconds)
                : F(AppLanguageKeys.MainMacroSummaryNoInputCapturedFormat, macro.Name);
            MacroStatusMessage = HasRecordedMacro
                ? F(AppLanguageKeys.MainMacroStatusStoppedRecordingFormat, macro.Name)
                : F(AppLanguageKeys.MainMacroStatusStoppedRecordingNoInputFormat, macro.Name);
            AddMacroLog(F(AppLanguageKeys.MainMacroLogStoppedRecordingFormat, macro.Name, macro.Events.Count));
        }
        catch (Exception ex)
        {
            MacroStatusMessage = F(AppLanguageKeys.MainMacroStatusStopRecordingFailedFormat, ex.Message);
            AddMacroLog(MacroStatusMessage);
        }
    }

    private bool CanPlayMacro => HasRecordedMacro && !IsMacroRecording && !IsMacroPlaying;

    [RelayCommand(CanExecute = nameof(CanPlayMacro))]
    private async Task PlayMacroAsync()
    {
        try
        {
            var macro = macroService.CurrentMacro;
            if (macro is null || macro.Events.Count == 0)
            {
                MacroStatusMessage = L(AppLanguageKeys.MainMacroStatusNoRecordedToPlay);
                AddMacroLog(L(AppLanguageKeys.MainMacroLogPlaybackRequestedNoMacro));
                return;
            }

            var count = Math.Max(1, MacroPlaybackCount);
            MacroPlaybackCount = count;
            IsMacroPlaying = true;
            MacroStatusMessage = F(AppLanguageKeys.MainMacroStatusPlayingFormat, macro.Name, count);
            AddMacroLog(MacroStatusMessage);

            await macroService.PlayAsync(count);

            MacroStatusMessage = F(AppLanguageKeys.MainMacroStatusFinishedPlayingFormat, macro.Name);
            AddMacroLog(MacroStatusMessage);
        }
        catch (Exception ex)
        {
            MacroStatusMessage = F(AppLanguageKeys.MainMacroStatusPlayFailedFormat, ex.Message);
            AddMacroLog(MacroStatusMessage);
        }
        finally
        {
            IsMacroPlaying = false;
        }
    }

    private bool CanSaveMacro => HasRecordedMacro && !IsMacroRecording && !IsMacroPlaying;

    [RelayCommand(CanExecute = nameof(CanSaveMacro))]
    private async Task SaveMacroAsync()
    {
        try
        {
            var macro = macroService.CurrentMacro;
            if (macro is null)
            {
                MacroStatusMessage = L(AppLanguageKeys.MainMacroStatusNoRecordedToSave);
                AddMacroLog(L(AppLanguageKeys.MainMacroLogSaveRequestedNoMacro));
                return;
            }

            var suggestedName = string.IsNullOrWhiteSpace(MacroName) ? macro.Name : MacroName.Trim();
            var chosenName = macroNamePromptService.PromptForName(suggestedName);
            if (string.IsNullOrWhiteSpace(chosenName))
            {
                MacroStatusMessage = L(AppLanguageKeys.MainMacroStatusSaveCanceled);
                AddMacroLog(MacroStatusMessage);
                return;
            }

            var macroToSave = macro with { Name = chosenName.Trim() };
            var filePath = macroLibraryService.GetSavePath(macroToSave.Name);
            await macroFileStore.SaveAsync(filePath, macroToSave);
            macroService.SetCurrentMacro(macroToSave);
            ApplyLoadedMacro(macroToSave);
            RefreshSavedMacrosInternal(filePath);
            MacroStatusMessage = F(AppLanguageKeys.MainMacroStatusSavedToMacrosFormat, Path.GetFileName(filePath));
            AddMacroLog(F(AppLanguageKeys.MainMacroLogSavedToPathFormat, filePath));
        }
        catch (Exception ex)
        {
            MacroStatusMessage = F(AppLanguageKeys.MainMacroStatusSaveFailedFormat, ex.Message);
            AddMacroLog(MacroStatusMessage);
        }
    }

    private bool CanLoadMacro => !IsMacroRecording && !IsMacroPlaying;

    [RelayCommand(CanExecute = nameof(CanLoadMacro))]
    private async Task LoadMacroAsync()
    {
        try
        {
            var filePath = macroFileDialogService.PickOpenPath();
            if (string.IsNullOrWhiteSpace(filePath))
            {
                MacroStatusMessage = L(AppLanguageKeys.MainMacroStatusLoadCanceled);
                AddMacroLog(MacroStatusMessage);
                return;
            }

            var macro = await macroFileStore.LoadAsync(filePath);
            macroService.SetCurrentMacro(macro);
            ApplyLoadedMacro(macro);
            MacroStatusMessage = F(AppLanguageKeys.MainMacroStatusLoadedFromFileFormat, Path.GetFileName(filePath));
            AddMacroLog(F(AppLanguageKeys.MainMacroLogLoadedFromPathFormat, filePath));
        }
        catch (Exception ex)
        {
            MacroStatusMessage = F(AppLanguageKeys.MainMacroStatusLoadFailedFormat, ex.Message);
            AddMacroLog(MacroStatusMessage);
        }
    }

    private bool CanLoadSelectedSavedMacro => SelectedSavedMacro is not null && !IsMacroRecording && !IsMacroPlaying;

    [RelayCommand(CanExecute = nameof(CanLoadSelectedSavedMacro))]
    private async Task LoadSelectedSavedMacroAsync()
    {
        if (SelectedSavedMacro is null)
        {
            MacroStatusMessage = L(AppLanguageKeys.MainMacroStatusChooseSavedFirst);
            AddMacroLog(L(AppLanguageKeys.MainMacroLogLoadSelectedNoSaved));
            return;
        }

        try
        {
            var macro = await macroFileStore.LoadAsync(SelectedSavedMacro.FilePath);
            macroService.SetCurrentMacro(macro);
            ApplyLoadedMacro(macro);
            MacroStatusMessage = F(AppLanguageKeys.MainMacroStatusLoadedSavedFormat, SelectedSavedMacro.DisplayName);
            AddMacroLog(F(AppLanguageKeys.MainMacroLogLoadedSavedPathFormat, SelectedSavedMacro.FilePath));
        }
        catch (Exception ex)
        {
            MacroStatusMessage = F(AppLanguageKeys.MainMacroStatusLoadSavedFailedFormat, ex.Message);
            AddMacroLog(MacroStatusMessage);
        }
    }

    private bool CanEditSelectedSavedMacro => SelectedSavedMacro is not null && !IsMacroRecording && !IsMacroPlaying;

    [RelayCommand(CanExecute = nameof(CanEditSelectedSavedMacro))]
    private async Task EditSelectedSavedMacroAsync()
    {
        if (SelectedSavedMacro is null)
        {
            MacroStatusMessage = L(AppLanguageKeys.MainMacroStatusChooseSavedFirst);
            AddMacroLog(L(AppLanguageKeys.MainMacroLogEditRequestedNoSaved));
            return;
        }

        try
        {
            var originalPath = SelectedSavedMacro.FilePath;
            var macro = await macroFileStore.LoadAsync(originalPath);
            var editedMacro = macroEditorDialogService.Edit(macro);
            if (editedMacro is null)
            {
                MacroStatusMessage = L(AppLanguageKeys.MainMacroStatusEditCanceled);
                AddMacroLog(MacroStatusMessage);
                return;
            }

            var updatedPath = macroLibraryService.GetSavePath(editedMacro.Name);
            await macroFileStore.SaveAsync(updatedPath, editedMacro);

            if (!string.Equals(originalPath, updatedPath, StringComparison.OrdinalIgnoreCase) && File.Exists(originalPath))
            {
                File.Delete(originalPath);
            }

            macroService.SetCurrentMacro(editedMacro);
            ApplyLoadedMacro(editedMacro);
            UpdateAssignedMacroHotkeyPath(originalPath, updatedPath, editedMacro.Name);
            RefreshSavedMacrosInternal(updatedPath);
            MacroStatusMessage = F(AppLanguageKeys.MainMacroStatusSavedEditsFormat, editedMacro.Name);
            AddMacroLog(F(AppLanguageKeys.MainMacroLogSavedEditedToPathFormat, updatedPath));
        }
        catch (Exception ex)
        {
            MacroStatusMessage = F(AppLanguageKeys.MainMacroStatusEditSavedFailedFormat, ex.Message);
            AddMacroLog(MacroStatusMessage);
        }
    }

    [RelayCommand]
    private void RefreshSavedMacros()
    {
        RefreshSavedMacrosInternal();
        MacroStatusMessage = HasSavedMacros
            ? F(AppLanguageKeys.MainMacroStatusFoundSavedFormat, SavedMacros.Count, PluralSuffix(SavedMacros.Count))
            : L(AppLanguageKeys.MainMacroStatusNoSavedInDefaultFolder);
        AddMacroLog(MacroStatusMessage);
    }

    [RelayCommand]
    private void OpenSavedMacrosFolder()
    {
        try
        {
            Directory.CreateDirectory(macroLibraryService.DefaultDirectory);
            Process.Start(
                new ProcessStartInfo
                {
                    FileName = macroLibraryService.DefaultDirectory,
                    UseShellExecute = true,
                });

            MacroStatusMessage = L(AppLanguageKeys.MainMacroStatusOpenedSavedFolder);
            AddMacroLog(MacroStatusMessage);
        }
        catch (Exception ex)
        {
            MacroStatusMessage = F(AppLanguageKeys.MainMacroStatusOpenSavedFolderFailedFormat, ex.Message);
            AddMacroLog(MacroStatusMessage);
        }
    }

    [RelayCommand]
    private void ClearMacroLog()
    {
        MacroLogEntries.Clear();
        AddMacroLog(L(AppLanguageKeys.MainMacroStatusLogCleared));
        MacroStatusMessage = L(AppLanguageKeys.MainMacroStatusLogCleared);
    }

    public HotkeySettings CurrentHotkeys => hotkeySettings.Clone();

    public ScreenshotSettings CurrentScreenshotSettings => BuildScreenshotSettings();

    public MacroSettings CurrentMacroSettings => BuildMacroSettings();

    public Task<bool> AutoSaveAsync() => SaveSettingsAsync(L(AppLanguageKeys.MainStatusSettingsAutoSaved), updateStatusOnSuccess: false, addActivityLogOnSuccess: false);

    public void CaptureCustomKey(Key key)
    {
        var virtualKey = HotkeyDisplayNameFormatter.ToVirtualKey(key);
        var displayName = HotkeyDisplayNameFormatter.FormatVirtualKey(virtualKey);

        CustomInputKind = AutoClicker.Core.Enums.CustomInputKind.Keyboard;
        CustomKeyVirtualKey = virtualKey;
        CustomMouseButton = ClickMouseButton.Left;
        CustomKeyDisplayText = displayName;
        StatusMessage = F(AppLanguageKeys.MainStatusCustomKeySetFormat, displayName);
    }

    public void CaptureScreenshotHotkey(Key key)
    {
        var capturedKey = key == Key.System ? Key.None : key;
        var virtualKey = HotkeyDisplayNameFormatter.ToVirtualKey(capturedKey);
        if (virtualKey <= 0)
        {
            return;
        }

        ScreenshotHotkeyVirtualKey = virtualKey;
        ScreenshotHotkeyDisplay = HotkeyDisplayNameFormatter.FormatVirtualKey(virtualKey);
        ScreenshotStatusMessage = F(AppLanguageKeys.MainScreenshotStatusHotkeySetFormat, ScreenshotHotkeyDisplay);
        AddScreenshotLog(ScreenshotStatusMessage);
        HotkeysChanged?.Invoke(this, EventArgs.Empty);
    }

    public void CaptureClickerHotkey(Key key)
    {
        var capturedKey = key == Key.System ? Key.None : key;
        var virtualKey = HotkeyDisplayNameFormatter.ToVirtualKey(capturedKey);
        if (virtualKey <= 0)
        {
            return;
        }

        hotkeySettings.Toggle = new HotkeyBinding(
            virtualKey,
            HotkeyDisplayNameFormatter.FormatVirtualKey(virtualKey));
        OnPropertyChanged(nameof(ClickerHotkeyDisplay));
        HotkeysChanged?.Invoke(this, EventArgs.Empty);
        ScheduleSettingsAutoSave();
        StatusMessage = F(AppLanguageKeys.MainStatusClickerHotkeySetFormat, ClickerHotkeyDisplay);
    }

    public void CaptureMacroHotkey(Key key)
    {
        var capturedKey = key == Key.System ? Key.None : key;
        var virtualKey = HotkeyDisplayNameFormatter.ToVirtualKey(capturedKey);
        if (virtualKey <= 0)
        {
            return;
        }

        MacroHotkeyVirtualKey = virtualKey;
        MacroHotkeyDisplay = HotkeyDisplayNameFormatter.FormatVirtualKey(virtualKey);
        MacroStatusMessage = F(AppLanguageKeys.MainMacroStatusHotkeySetFormat, MacroHotkeyDisplay);
        AddMacroLog(MacroStatusMessage);
        HotkeysChanged?.Invoke(this, EventArgs.Empty);
    }

    public void CaptureMacroRecordHotkey(Key key)
    {
        var capturedKey = key == Key.System ? Key.None : key;
        var virtualKey = HotkeyDisplayNameFormatter.ToVirtualKey(capturedKey);
        if (virtualKey <= 0)
        {
            return;
        }

        MacroRecordHotkeyVirtualKey = virtualKey;
        MacroRecordHotkeyDisplay = HotkeyDisplayNameFormatter.FormatVirtualKey(virtualKey);
        MacroStatusMessage = F(AppLanguageKeys.MainMacroStatusRecordHotkeySetFormat, MacroRecordHotkeyDisplay);
        AddMacroLog(MacroStatusMessage);
        HotkeysChanged?.Invoke(this, EventArgs.Empty);
    }

    public void CaptureCustomMouseButton(ClickMouseButton mouseButton)
    {
        CustomInputKind = AutoClicker.Core.Enums.CustomInputKind.MouseButton;
        CustomKeyVirtualKey = 0;
        CustomMouseButton = mouseButton;
        CustomKeyDisplayText = FormatMouseButtonDisplay(mouseButton);
        StatusMessage = F(AppLanguageKeys.MainStatusCustomInputSetFormat, CustomKeyDisplayText);
    }

    public void UpdateRunningState(bool running)
    {
        IsRunning = running;
        RefreshWindowTitle();
        RefreshHotkeyLabels();
    }

    public void SetStatus(string message)
    {
        StatusMessage = message;
    }

    public async Task HandleHotkeyAsync(HotkeyAction action, string? payload = null)
    {
        switch (action)
        {
            case HotkeyAction.Toggle:
                await ToggleAsync();
                return;
            case HotkeyAction.ScreenshotCapture:
                if (await screenshotOptionsDialogService.TryHandleCaptureHotkeyAsync())
                {
                    var videoWasSaved = RefreshLatestVideoFromCaptureService();
                    if (videoWasSaved)
                    {
                        ScreenshotStatusMessage = F(AppLanguageKeys.MainScreenshotStatusSavedVideoFormat, LatestVideoCaption);
                        AddScreenshotLog(ScreenshotStatusMessage);
                    }

                    return;
                }

                await PerformScreenshotAsync(ScreenshotMode.FullScreen);
                return;
            case HotkeyAction.ScreenshotOptions:
                await ShowScreenshotOptionsAsync();
                return;
            case HotkeyAction.MacroPlay:
                await PlayMacroAsync();
                return;
            case HotkeyAction.MacroRecordToggle:
                await ToggleMacroRecordingAsync();
                return;
            case HotkeyAction.MacroAssigned:
                await HandleAssignedMacroHotkeyAsync(payload);
                return;
            case HotkeyAction.WindowPinToggle:
                ToggleWindowPinFromHotkey();
                return;
            default:
                throw new NotSupportedException($"Hotkey action {action} is not supported.");
        }
    }

    private void ApplySettings(AppSettings settings)
    {
        Hours = settings.Clicker.Hours;
        Minutes = settings.Clicker.Minutes;
        Seconds = settings.Clicker.Seconds;
        Milliseconds = settings.Clicker.Milliseconds;
        SelectedMouseButton = settings.Clicker.MouseButton;
        CustomInputKind = settings.Clicker.CustomInputKind;
        CustomKeyVirtualKey = settings.Clicker.CustomKeyVirtualKey;
        CustomMouseButton = settings.Clicker.CustomMouseButton;
        CustomKeyDisplayText = GetCustomKeyDisplayText(settings.Clicker);
        SelectedClickKind = settings.Clicker.ClickType;
        SelectedRepeatMode = settings.Clicker.RepeatMode;
        SelectedLocationMode = settings.Clicker.LocationMode;
        FixedX = settings.Clicker.FixedX;
        FixedY = settings.Clicker.FixedY;
        RepeatCount = settings.Clicker.RepeatCount;
        IsTopMost = settings.Clicker.AlwaysOnTop;
        hotkeySettings = settings.Hotkeys.Clone();
        ScreenshotFolderPath = settings.Screenshot.SaveFolderPath;
        ScreenshotFilePrefix = settings.Screenshot.FilePrefix;
        ScreenshotHotkeyVirtualKey = settings.Screenshot.CaptureHotkey.VirtualKey;
        ScreenshotHotkeyDisplay = settings.Screenshot.CaptureHotkey.DisplayName;
        MacroHotkeyVirtualKey = settings.Macro.PlayHotkey.VirtualKey;
        MacroHotkeyDisplay = settings.Macro.PlayHotkey.DisplayName;
        MacroRecordHotkeyVirtualKey = settings.Macro.RecordHotkey.VirtualKey;
        MacroRecordHotkeyDisplay = settings.Macro.RecordHotkey.DisplayName;
        RecordMacroMouseMovement = settings.Macro.RecordMouseMovement;
        SetMacroHotkeyAssignments(settings.Macro.AssignedHotkeys);
        ApplyInstallerSettings(settings.Installer);
        shortcutHotkeyScanMaxFolderCountCache = Math.Max(settings.Tools.ShortcutHotkeyScanMaxFolderCount, 0);
        emptyDirectoryScanMaxFolderCountCache = new Dictionary<string, int>(
            settings.Tools.EmptyDirectoryScanMaxFolderCounts,
            StringComparer.OrdinalIgnoreCase);
        suppressThemeChange = true;
        IsDarkMode = settings.Ui.IsDarkMode ?? themeService.GetSystemPrefersDarkMode();
        IsCtrlWheelResizeEnabled = true;
        IsAutoHideOnStartupEnabled = settings.Ui.AutoHideOnStartup;
        IsSillyModeEnabled = settings.Ui.SillyMode;
        OnPropertyChanged(nameof(AppearanceHelperText));
        OnPropertyChanged(nameof(ClickerTabHeaderText));
        OnPropertyChanged(nameof(ClickerHotkeyLabelText));
        OnPropertyChanged(nameof(ScreenshotTabHeaderText));
        OnPropertyChanged(nameof(MacroTabHeaderText));
        OnPropertyChanged(nameof(MacroSetShortcutButtonText));
        OnPropertyChanged(nameof(MacroEditShortcutsButtonText));
        OnPropertyChanged(nameof(ToolsTabHeaderText));
        OnPropertyChanged(nameof(NicheToolsHeaderText));
        OnPropertyChanged(nameof(ShortcutKeyExplorerHeaderText));
        OnPropertyChanged(nameof(ShortcutExplorerButtonText));
        OnPropertyChanged(nameof(InstallerTabHeaderText));
        OnPropertyChanged(nameof(SettingsTabHeaderText));
        OnPropertyChanged(nameof(AppearanceHeaderText));
        OnPropertyChanged(nameof(DarkModeLabelText));
        OnPropertyChanged(nameof(AlwaysOnTopLabelText));
        OnPropertyChanged(nameof(CatTranslatorLabelText));
        OnPropertyChanged(nameof(AutoHideOnStartupLabelText));
        OnPropertyChanged(nameof(ResetAllSettingsButtonText));
        OnPropertyChanged(nameof(BugCheckingHeaderText));
        OnPropertyChanged(nameof(BugCheckingHelperText));
        OnPropertyChanged(nameof(CopyLogButtonText));
        suppressThemeChange = false;
        themeService.ApplyTheme(IsDarkMode);
        SettingsStatusMessage = IsDarkMode
            ? L(AppLanguageKeys.MainSettingsStatusDarkModeOn)
            : L(AppLanguageKeys.MainSettingsStatusDarkModeOff);
        RefreshHotkeyLabels();
    }

    private ClickSettings BuildClickSettings() =>
        new()
        {
            Hours = Hours,
            Minutes = Minutes,
            Seconds = Seconds,
            Milliseconds = Milliseconds,
            MouseButton = SelectedMouseButton,
            CustomInputKind = CustomInputKind,
            CustomKeyVirtualKey = CustomKeyVirtualKey,
            CustomKeyDisplayName = GetCustomKeyDisplayName(),
            CustomMouseButton = CustomMouseButton,
            ClickType = SelectedClickKind,
            RepeatMode = SelectedRepeatMode,
            LocationMode = SelectedLocationMode,
            FixedX = FixedX,
            FixedY = FixedY,
            RepeatCount = RepeatCount,
            AlwaysOnTop = IsTopMost,
        };

    private AppSettings BuildAppSettings() =>
        new()
        {
            Clicker = BuildClickSettings(),
            Hotkeys = hotkeySettings.Clone(),
            Screenshot = BuildScreenshotSettings(),
            Macro = BuildMacroSettings(),
            Installer = BuildInstallerSettings(),
            Tools = BuildToolSettings(),
            Ui = new UiSettings
            {
                IsDarkMode = IsDarkMode,
                EnableCtrlWheelResize = true,
                AutoHideOnStartup = IsAutoHideOnStartupEnabled,
                SillyMode = IsSillyModeEnabled,
            },
        };

    private ScreenshotSettings BuildScreenshotSettings() =>
        new()
        {
            CaptureHotkey = new HotkeyBinding(
                virtualKey: ScreenshotHotkeyVirtualKey <= 0 ? ScreenshotSettings.DefaultCaptureVirtualKey : ScreenshotHotkeyVirtualKey,
                displayName: string.IsNullOrWhiteSpace(ScreenshotHotkeyDisplay) ? ScreenshotSettings.DefaultCaptureDisplayName : ScreenshotHotkeyDisplay),
            SaveFolderPath = ScreenshotFolderPath,
            FilePrefix = ScreenshotFilePrefix,
        };

    private MacroSettings BuildMacroSettings() =>
        new()
        {
            PlayHotkey = new HotkeyBinding(
                virtualKey: MacroHotkeyVirtualKey <= 0 ? MacroSettings.DefaultPlayVirtualKey : MacroHotkeyVirtualKey,
                displayName: string.IsNullOrWhiteSpace(MacroHotkeyDisplay) ? MacroSettings.DefaultPlayDisplayName : MacroHotkeyDisplay),
            RecordHotkey = new HotkeyBinding(
                virtualKey: MacroRecordHotkeyVirtualKey <= 0 ? MacroSettings.DefaultRecordVirtualKey : MacroRecordHotkeyVirtualKey,
                displayName: string.IsNullOrWhiteSpace(MacroRecordHotkeyDisplay) ? MacroSettings.DefaultRecordDisplayName : MacroRecordHotkeyDisplay),
            RecordMouseMovement = RecordMacroMouseMovement,
            AssignedHotkeys = macroHotkeyAssignments.Select(assignment => assignment.Clone()).ToList(),
        };

    private ToolSettings BuildToolSettings() =>
        new()
        {
            ShortcutHotkeyScanMaxFolderCount = Math.Max(shortcutHotkeyScanMaxFolderCountCache, 0),
            EmptyDirectoryScanMaxFolderCounts = new Dictionary<string, int>(
                emptyDirectoryScanMaxFolderCountCache,
                StringComparer.OrdinalIgnoreCase),
        };

    private void RefreshHotkeyLabels()
    {
        OnPropertyChanged(nameof(ClickerHotkeyDisplay));
        OnPropertyChanged(nameof(PinWindowHotkeyLabel));
    }

    private string GetPinWindowHotkeyDisplay() =>
        hotkeySettings.PinWindow.VirtualKey > 0 && !string.IsNullOrWhiteSpace(hotkeySettings.PinWindow.DisplayName)
            ? hotkeySettings.PinWindow.DisplayName
            : HotkeySettings.UnassignedDisplayName;

    partial void OnSelectedMouseButtonChanged(ClickMouseButton value)
    {
        OnPropertyChanged(nameof(IsCustomKeySelected));
        OnPropertyChanged(nameof(UsesMousePositionSettings));
        ScheduleSettingsAutoSave();
    }

    partial void OnCustomInputKindChanged(CustomInputKind value)
    {
        OnPropertyChanged(nameof(UsesMousePositionSettings));
        ScheduleSettingsAutoSave();
    }

    partial void OnHoursChanged(int value)
    {
        ScheduleSettingsAutoSave();
    }

    partial void OnMinutesChanged(int value)
    {
        ScheduleSettingsAutoSave();
    }

    partial void OnSecondsChanged(int value)
    {
        ScheduleSettingsAutoSave();
    }

    partial void OnMillisecondsChanged(int value)
    {
        ScheduleSettingsAutoSave();
    }

    partial void OnCustomKeyVirtualKeyChanged(int value)
    {
        ScheduleSettingsAutoSave();
    }

    partial void OnCustomKeyDisplayTextChanged(string value)
    {
        ScheduleSettingsAutoSave();
    }

    partial void OnCustomMouseButtonChanged(ClickMouseButton value)
    {
        ScheduleSettingsAutoSave();
    }

    partial void OnSelectedClickKindChanged(ClickKind value)
    {
        ScheduleSettingsAutoSave();
    }

    partial void OnSelectedRepeatModeChanged(RepeatMode value)
    {
        OnPropertyChanged(nameof(IsRepeatCountEnabled));
        ScheduleSettingsAutoSave();
    }

    partial void OnSelectedLocationModeChanged(ClickLocationMode value)
    {
        ScheduleSettingsAutoSave();
    }

    partial void OnFixedXChanged(int value)
    {
        ScheduleSettingsAutoSave();
    }

    partial void OnFixedYChanged(int value)
    {
        ScheduleSettingsAutoSave();
    }

    partial void OnRepeatCountChanged(int value)
    {
        ScheduleSettingsAutoSave();
    }

    partial void OnIsTopMostChanged(bool value)
    {
        OnPropertyChanged(nameof(IsWindowPinned));
        ScheduleSettingsAutoSave();
    }

    partial void OnScreenshotFolderPathChanged(string value)
    {
        ScheduleSettingsAutoSave();
    }

    partial void OnScreenshotFilePrefixChanged(string value)
    {
        ScheduleSettingsAutoSave();
    }

    partial void OnScreenshotHotkeyDisplayChanged(string value)
    {
        ScheduleSettingsAutoSave();
    }

    partial void OnScreenshotHotkeyVirtualKeyChanged(int value)
    {
        ScheduleSettingsAutoSave();
    }

    partial void OnMacroHotkeyDisplayChanged(string value)
    {
        ScheduleSettingsAutoSave();
    }

    partial void OnMacroHotkeyVirtualKeyChanged(int value)
    {
        ScheduleSettingsAutoSave();
    }

    partial void OnMacroRecordHotkeyDisplayChanged(string value)
    {
        ScheduleSettingsAutoSave();
    }

    partial void OnMacroRecordHotkeyVirtualKeyChanged(int value)
    {
        ScheduleSettingsAutoSave();
    }

    partial void OnRecordMacroMouseMovementChanged(bool value)
    {
        ScheduleSettingsAutoSave();
    }

    partial void OnSelectedSavedMacroChanged(SavedMacroEntry? value)
    {
        LoadSelectedSavedMacroCommand.NotifyCanExecuteChanged();
        EditSelectedSavedMacroCommand.NotifyCanExecuteChanged();
        AssignSelectedMacroHotkeyCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(HasSelectedSavedMacroHotkey));
        OnPropertyChanged(nameof(SelectedSavedMacroHotkeySummary));
    }

    partial void OnLatestScreenshotPreviewChanged(ImageSource? value)
    {
        OnPropertyChanged(nameof(HasLatestScreenshot));
        OnPropertyChanged(nameof(IsLatestMediaVideo));
    }

    partial void OnLatestVideoPathChanged(string? value)
    {
        OnPropertyChanged(nameof(HasLatestVideo));
        OnPropertyChanged(nameof(IsLatestMediaVideo));
        OnPropertyChanged(nameof(LatestVideoSource));
    }

    partial void OnIsMacroRecordingChanged(bool value)
    {
        RefreshMacroCommandStates();
    }

    partial void OnHasRecordedMacroChanged(bool value)
    {
        RefreshMacroCommandStates();
    }

    partial void OnIsMacroPlayingChanged(bool value)
    {
        RefreshMacroCommandStates();
    }

    partial void OnIsDarkModeChanged(bool value)
    {
        if (suppressThemeChange)
        {
            return;
        }

        themeService.ApplyTheme(value);
        SettingsStatusMessage = value
            ? L(AppLanguageKeys.MainSettingsStatusDarkModeOn)
            : L(AppLanguageKeys.MainSettingsStatusDarkModeOff);
        ScheduleSettingsAutoSave();

        if (initialized)
        {
            AddActivityLog(value
                ? L(AppLanguageKeys.MainActivityDarkModeEnabled)
                : L(AppLanguageKeys.MainActivityDarkModeDisabled));
        }
    }

    partial void OnIsCtrlWheelResizeEnabledChanged(bool value)
    {
        if (suppressThemeChange)
        {
            return;
        }

        SettingsStatusMessage = value
            ? L(AppLanguageKeys.MainSettingsStatusCtrlWheelZoomOn)
            : L(AppLanguageKeys.MainSettingsStatusCtrlWheelZoomOff);
        ScheduleSettingsAutoSave();

        if (initialized)
        {
            AddActivityLog(value
                ? L(AppLanguageKeys.MainActivityCtrlWheelZoomEnabled)
                : L(AppLanguageKeys.MainActivityCtrlWheelZoomDisabled));
        }
    }

    partial void OnIsAutoHideOnStartupEnabledChanged(bool value)
    {
        if (suppressThemeChange)
        {
            return;
        }

        SettingsStatusMessage = value
            ? L(AppLanguageKeys.MainSettingsStatusAutoHideOn)
            : L(AppLanguageKeys.MainSettingsStatusAutoHideOff);
        ScheduleSettingsAutoSave();

        if (initialized)
        {
            AddActivityLog(value
                ? L(AppLanguageKeys.MainActivityAutoHideEnabled)
                : L(AppLanguageKeys.MainActivityAutoHideDisabled));
        }
    }

    partial void OnIsSillyModeEnabledChanged(bool value)
    {
        if (suppressThemeChange)
        {
            return;
        }

        OnPropertyChanged(nameof(AppearanceHelperText));
        OnPropertyChanged(nameof(ClickerTabHeaderText));
        OnPropertyChanged(nameof(ClickerHotkeyLabelText));
        OnPropertyChanged(nameof(ScreenshotTabHeaderText));
        OnPropertyChanged(nameof(MacroTabHeaderText));
        OnPropertyChanged(nameof(MacroSetShortcutButtonText));
        OnPropertyChanged(nameof(MacroEditShortcutsButtonText));
        OnPropertyChanged(nameof(ToolsTabHeaderText));
        OnPropertyChanged(nameof(NicheToolsHeaderText));
        OnPropertyChanged(nameof(ShortcutKeyExplorerHeaderText));
        OnPropertyChanged(nameof(ShortcutExplorerButtonText));
        OnPropertyChanged(nameof(InstallerTabHeaderText));
        OnPropertyChanged(nameof(SettingsTabHeaderText));
        OnPropertyChanged(nameof(AppearanceHeaderText));
        OnPropertyChanged(nameof(DarkModeLabelText));
        OnPropertyChanged(nameof(AlwaysOnTopLabelText));
        OnPropertyChanged(nameof(CatTranslatorLabelText));
        OnPropertyChanged(nameof(AutoHideOnStartupLabelText));
        OnPropertyChanged(nameof(ResetAllSettingsButtonText));
        OnPropertyChanged(nameof(BugCheckingHeaderText));
        OnPropertyChanged(nameof(BugCheckingHelperText));
        OnPropertyChanged(nameof(CopyLogButtonText));

        SettingsStatusMessage = value
            ? L(AppLanguageKeys.MainSettingsStatusCatTranslatorOn)
            : (IsDarkMode
                ? L(AppLanguageKeys.MainSettingsStatusDarkModeOn)
                : L(AppLanguageKeys.MainSettingsStatusDarkModeOff));
        ScheduleSettingsAutoSave();

        if (initialized)
        {
            AddActivityLog(value
                ? L(AppLanguageKeys.MainActivityCatTranslatorEnabled)
                : L(AppLanguageKeys.MainActivityCatTranslatorDisabled));
        }
    }

    partial void OnIsRunningAsAdministratorChanged(bool value)
    {
        OnPropertyChanged(nameof(ShouldShowAdminModeBanner));
        OnPropertyChanged(nameof(AdminModeBannerText));
        RefreshWindowTitle();
    }

    private void RefreshMacroCommandStates()
    {
        NewMacroCommand.NotifyCanExecuteChanged();
        StartMacroRecordingCommand.NotifyCanExecuteChanged();
        StopMacroRecordingCommand.NotifyCanExecuteChanged();
        PlayMacroCommand.NotifyCanExecuteChanged();
        SaveMacroCommand.NotifyCanExecuteChanged();
        LoadMacroCommand.NotifyCanExecuteChanged();
        LoadSelectedSavedMacroCommand.NotifyCanExecuteChanged();
        EditSelectedSavedMacroCommand.NotifyCanExecuteChanged();
        ManageMacroHotkeysCommand.NotifyCanExecuteChanged();
        AssignSelectedMacroHotkeyCommand.NotifyCanExecuteChanged();
    }

    private string GetCustomKeyDisplayName() =>
        !string.IsNullOrWhiteSpace(CustomKeyDisplayText) && CustomInputKind != AutoClicker.Core.Enums.CustomInputKind.None
            ? CustomKeyDisplayText
            : string.Empty;

    private static string GetCustomKeyDisplayText(ClickSettings settings) =>
        !string.IsNullOrWhiteSpace(settings.CustomKeyDisplayName)
            ? settings.CustomKeyDisplayName
            : AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.MainCustomKeyOrMousePrompt);

    private static string FormatMouseButtonDisplay(ClickMouseButton mouseButton) =>
        mouseButton switch
        {
            ClickMouseButton.Left => AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.MainMouseButtonLeft),
            ClickMouseButton.Right => AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.MainMouseButtonRight),
            ClickMouseButton.Middle => AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.MainMouseButtonMiddle),
            ClickMouseButton.XButton1 => AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.MainMouseButton4),
            ClickMouseButton.XButton2 => AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.MainMouseButton5),
            _ => mouseButton.ToString(),
        };

    private void AddScreenshotLog(string message)
    {
        AddActivityLog(message);
    }

    private void AddMacroLog(string message)
    {
        MacroLogEntries.Insert(0, $"{DateTime.Now:HH:mm:ss}  {message}");
        AddActivityLog(message);
    }

    private void AddActivityLog(string message)
    {
        ActivityLogEntries.Insert(0, $"{DateTime.Now:HH:mm:ss}  {message}");

        while (ActivityLogEntries.Count > 200)
        {
            ActivityLogEntries.RemoveAt(ActivityLogEntries.Count - 1);
        }
    }

    private void RefreshAdminModeState()
    {
        IsRunningAsAdministrator = GetIsCurrentProcessElevated();
        RefreshWindowTitle();
        if (!IsRunningAsAdministrator)
        {
            AddActivityLog(L(AppLanguageKeys.MainAdminActivityNotAdmin));
        }
    }

    private void RefreshWindowTitle()
    {
        WindowTitle = IsRunning
            ? L(AppLanguageKeys.MainWindowTitleRunning)
            : L(AppLanguageKeys.MainWindowTitleDefault);

        if (!IsRunningAsAdministrator)
        {
            WindowTitle += L(AppLanguageKeys.MainWindowTitleNotAdminSuffix);
        }
    }

    private static bool GetIsCurrentProcessElevated()
    {
        using var identity = WindowsIdentity.GetCurrent();
        return identity is not null && new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
    }

    private void UpdateLatestScreenshotPreview(string filePath, string fileName)
    {
        LatestScreenshotPreview = LoadPreview(filePath);
        LatestScreenshotCaption = fileName;
        latestScreenshotUpdatedAtUtc = File.GetLastWriteTimeUtc(filePath);
        OnPropertyChanged(nameof(IsLatestMediaVideo));
    }

    private bool RefreshLatestVideoFromCaptureService()
    {
        var previousPath = LatestVideoPath;
        var filePath = screenshotCaptureService.LastSavedVideoPath;
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return false;
        }

        var fileLastWriteTimeUtc = File.GetLastWriteTimeUtc(filePath);
        var isSamePath = string.Equals(previousPath, filePath, StringComparison.OrdinalIgnoreCase);
        var wasUpdated = !isSamePath || fileLastWriteTimeUtc != latestVideoUpdatedAtUtc;

        // Force a source refresh when the recorder writes to the same file path.
        if (isSamePath)
        {
            LatestVideoPath = null;
        }

        LatestVideoPath = filePath;
        LatestVideoCaption = Path.GetFileName(filePath);
        latestVideoUpdatedAtUtc = fileLastWriteTimeUtc;
        OnPropertyChanged(nameof(IsLatestMediaVideo));
        return wasUpdated;
    }

    private static ImageSource LoadPreview(string filePath)
    {
        using var stream = File.OpenRead(filePath);

        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.DecodePixelWidth = 720;
        image.StreamSource = stream;
        image.EndInit();
        image.Freeze();
        return image;
    }

    private void ApplyLoadedMacro(RecordedMacro macro)
    {
        MacroName = macro.Name;
        HasRecordedMacro = macro.Events.Count > 0;
        MacroSummaryText = BuildMacroSummary(macro);
    }

    private static string BuildMacroSummary(RecordedMacro macro) =>
        macro.Events.Count > 0
            ? AppLanguageStrings.FormatForCurrentLanguage(AppLanguageKeys.MainMacroSummaryRecordedFormat, macro.Name, macro.Events.Count, macro.Duration.TotalMilliseconds)
            : AppLanguageStrings.FormatForCurrentLanguage(AppLanguageKeys.MainMacroSummaryNoInputCapturedFormat, macro.Name);

    private void RefreshSavedMacrosInternal(string? preferredPath = null)
    {
        IReadOnlyList<SavedMacroEntry> savedMacros;
        try
        {
            savedMacros = macroLibraryService.GetSavedMacros();
        }
        catch (Exception ex)
        {
            SavedMacros.Clear();
            SelectedSavedMacro = null;
            OnPropertyChanged(nameof(HasSavedMacros));
            MacroStatusMessage = F(AppLanguageKeys.MainMacroStatusSavedFolderUnavailableFormat, ex.Message);
            AddMacroLog(MacroStatusMessage);
            return;
        }

        var selectedPath = preferredPath ?? SelectedSavedMacro?.FilePath;

        SavedMacros.Clear();
        foreach (var savedMacro in savedMacros)
        {
            SavedMacros.Add(savedMacro);
        }

        SelectedSavedMacro = savedMacros.FirstOrDefault(macro => string.Equals(macro.FilePath, selectedPath, StringComparison.OrdinalIgnoreCase))
            ?? savedMacros.FirstOrDefault();

        OnPropertyChanged(nameof(HasSavedMacros));
        LoadSelectedSavedMacroCommand.NotifyCanExecuteChanged();
        ManageMacroHotkeysCommand.NotifyCanExecuteChanged();
        AssignSelectedMacroHotkeyCommand.NotifyCanExecuteChanged();
        SynchronizeAssignedMacroHotkeysWithSavedMacros();
    }

    [RelayCommand]
    private void BrowseScreenshotFolder()
    {
        var selectedPath = folderPickerService.PickFolder(ScreenshotFolderPath, L(AppLanguageKeys.MainScreenshotFolderPickerPrompt));
        if (string.IsNullOrWhiteSpace(selectedPath))
        {
            ScreenshotStatusMessage = L(AppLanguageKeys.MainScreenshotStatusFolderSelectionCanceled);
            AddScreenshotLog(ScreenshotStatusMessage);
            return;
        }

        ScreenshotFolderPath = selectedPath;
        ScreenshotStatusMessage = F(AppLanguageKeys.MainScreenshotStatusFolderSetFormat, selectedPath);
        AddScreenshotLog(ScreenshotStatusMessage);
    }

    [RelayCommand]
    private void CopyActivityLog()
    {
        var text = ActivityLogEntries.Count > 0
            ? string.Join(Environment.NewLine, ActivityLogEntries)
            : L(AppLanguageKeys.MainActivityLogEmpty);

        clipboardTextService.SetText(text);
        SettingsStatusMessage = L(AppLanguageKeys.MainSettingsStatusCopiedActivityLog);
        AddActivityLog(SettingsStatusMessage);
    }

    [RelayCommand]
    private async Task ResetAllSettingsAsync()
    {
        var defaults = DefaultSettingsFactory.Create();
        ApplySettings(defaults);
        HotkeysChanged?.Invoke(this, EventArgs.Empty);

        var saved = await SaveSettingsAsync(L(AppLanguageKeys.MainSettingsStatusResetRequested), updateStatusOnSuccess: true);
        SettingsStatusMessage = saved
            ? L(AppLanguageKeys.MainSettingsStatusResetCompleted)
            : L(AppLanguageKeys.MainSettingsStatusResetSaveFailed);
    }

    private async Task ShowScreenshotOptionsAsync()
    {
        var settings = BuildScreenshotSettings();
        var validation = settingsValidator.ValidateScreenshot(settings);
        if (!validation.IsValid)
        {
            ScreenshotStatusMessage = validation.Summary;
            AddScreenshotLog(validation.Summary);
            return;
        }

        var result = screenshotOptionsDialogService.SelectMode(settings);
        if (result.WasIgnoredBecauseAlreadyOpen)
        {
            return;
        }

        if (result.WasHandledInDialog)
        {
            var videoWasSaved = RefreshLatestVideoFromCaptureService();
            ScreenshotStatusMessage = videoWasSaved
                ? F(AppLanguageKeys.MainScreenshotStatusSavedVideoFormat, LatestVideoCaption)
                : L(AppLanguageKeys.MainScreenshotStatusVideoHandledInOptionsWindow);
            AddScreenshotLog(ScreenshotStatusMessage);
            return;
        }

        if (result.WasCanceled || result.Mode is null)
        {
            ScreenshotStatusMessage = L(AppLanguageKeys.MainScreenshotStatusOptionsCanceled);
            AddScreenshotLog(ScreenshotStatusMessage);
            return;
        }

        await PerformScreenshotAsync(result.Mode.Value);
    }

    private async Task PerformScreenshotAsync(ScreenshotMode mode)
    {
        try
        {
            var settings = BuildScreenshotSettings();
            var validation = settingsValidator.ValidateScreenshot(settings);
            if (!validation.IsValid)
            {
                ScreenshotStatusMessage = validation.Summary;
                AddScreenshotLog(validation.Summary);
                return;
            }

            switch (mode)
            {
                case ScreenshotMode.FullScreen:
                    {
                        var path = await screenshotCaptureService.CaptureDesktopAsync(settings.SaveFolderPath, settings.FilePrefix);
                        var fileName = Path.GetFileName(path);
                        UpdateLatestScreenshotPreview(path, fileName);
                        ScreenshotStatusMessage = F(AppLanguageKeys.MainScreenshotStatusSavedAndCopiedFormat, fileName);
                        AddScreenshotLog(F(AppLanguageKeys.MainScreenshotLogSavedFullScreenFormat, path));
                        break;
                    }
                case ScreenshotMode.Area:
                    {
                        AppLog.Info("Area capture requested from MainWindowViewModel.");
                        var area = screenshotAreaSelectionService.SelectArea();
                        if (area is null)
                        {
                            AppLog.Info("Area capture canceled before capture service call.");
                            ScreenshotStatusMessage = L(AppLanguageKeys.MainScreenshotStatusAreaCanceled);
                            AddScreenshotLog(ScreenshotStatusMessage);
                            return;
                        }

                        // Give the area-selection overlay a moment to fully disappear before capturing.
                        await Task.Delay(120);
                        AppLog.Info($"Area capture calling capture service with area=({area.Value.X},{area.Value.Y},{area.Value.Width}x{area.Value.Height}) saveFolder={settings.SaveFolderPath} prefix={settings.FilePrefix}");
                        var path = await screenshotCaptureService.CaptureAreaAsync(area.Value, settings.SaveFolderPath, settings.FilePrefix);
                        var fileName = Path.GetFileName(path);
                        AppLog.Info($"Area capture completed. OutputPath={path}");
                        UpdateLatestScreenshotPreview(path, fileName);
                        ScreenshotStatusMessage = F(AppLanguageKeys.MainScreenshotStatusSavedAndCopiedFormat, fileName);
                        AddScreenshotLog(F(AppLanguageKeys.MainScreenshotLogSavedAreaFormat, path));
                        break;
                    }
                case ScreenshotMode.Video:
                    throw new NotSupportedException("Video recording is handled directly inside the screenshot options window.");
                default:
                    throw new NotSupportedException($"Screenshot mode {mode} is not supported.");
            }
        }
        catch (Exception ex)
        {
            ScreenshotStatusMessage = F(AppLanguageKeys.MainScreenshotStatusFailedFormat, ex.Message);
            AddScreenshotLog(ScreenshotStatusMessage);
        }
    }


    private async Task<bool> SaveSettingsAsync(string successMessage, bool updateStatusOnSuccess, bool addActivityLogOnSuccess = true)
    {
        if (!initialized)
        {
            return false;
        }

        var settings = BuildAppSettings();
        var validation = settingsValidator.Validate(settings);
        if (!validation.IsValid)
        {
            StatusMessage = validation.Summary;
            return false;
        }

        await saveLock.WaitAsync();

        try
        {
            await settingsStore.SaveAsync(settings);
        }
        catch (Exception ex)
        {
            StatusMessage = F(AppLanguageKeys.MainStatusUnableSaveSettingsFormat, ex.Message);
            return false;
        }
        finally
        {
            saveLock.Release();
        }

        if (updateStatusOnSuccess)
        {
            StatusMessage = successMessage;
        }

        if (addActivityLogOnSuccess)
        {
            AddActivityLog(successMessage);
        }

        return true;
    }

    private void ScheduleSettingsAutoSave()
    {
        if (!initialized)
        {
            return;
        }

        pendingAutoSaveCancellationTokenSource?.Cancel();
        pendingAutoSaveCancellationTokenSource?.Dispose();
        pendingAutoSaveCancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = pendingAutoSaveCancellationTokenSource.Token;

        _ = RunDebouncedSettingsAutoSaveAsync(cancellationToken);
    }

    private async Task RunDebouncedSettingsAutoSaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(450, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            if (synchronizationContext is null)
            {
                await AutoSaveAsync();
                return;
            }

            var completionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            synchronizationContext.Post(
                async _ =>
                {
                    try
                    {
                        await AutoSaveAsync();
                        completionSource.SetResult();
                    }
                    catch (Exception ex)
                    {
                        completionSource.SetException(ex);
                    }
                },
                null);

            await completionSource.Task;
        }
        catch (OperationCanceledException)
        {
        }
    }
}
