using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AutoClicker.App.Helpers;
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
    private readonly IDisplayRefreshRateService displayRefreshRateService;
    private readonly IHardwareInventoryService hardwareInventoryService;
    private readonly IDriverUpdateService driverUpdateService;
    private readonly IOneDriveRemovalService oneDriveRemovalService;
    private readonly SemaphoreSlim saveLock = new(1, 1);
    private readonly SynchronizationContext? synchronizationContext;

    private HotkeySettings hotkeySettings = new();
    private CancellationTokenSource? pendingAutoSaveCancellationTokenSource;
    private bool initialized;
    private bool suppressThemeChange;

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
        IDisplayRefreshRateService displayRefreshRateService,
        IHardwareInventoryService hardwareInventoryService,
        IDriverUpdateService driverUpdateService,
        IOneDriveRemovalService oneDriveRemovalService)
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
        this.displayRefreshRateService = displayRefreshRateService;
        this.hardwareInventoryService = hardwareInventoryService;
        this.driverUpdateService = driverUpdateService;
        this.oneDriveRemovalService = oneDriveRemovalService;
        synchronizationContext = SynchronizationContext.Current;

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
            "Macro log ready.",
            "No macro recorded yet.",
        ];

        SavedMacros = [];
        InitializeInstallerState();
        InitializeToolsState();

        ActivityLogEntries =
        [
            $"{DateTime.Now:HH:mm:ss}  Activity log ready.",
        ];

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

    public bool HasSavedMacros => SavedMacros.Count > 0;

    [ObservableProperty]
    private string windowTitle = "MultiTool";

    [ObservableProperty]
    private string statusMessage = "Loading settings...";

    [ObservableProperty]
    private string toggleButtonText = "Toggle On (-)";

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
    private string customKeyDisplayText = "Click here and press a key";

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
    private string screenshotFilePrefix = "Screenshot";

    [ObservableProperty]
    private string screenshotHotkeyDisplay = ScreenshotSettings.DefaultCaptureDisplayName;

    [ObservableProperty]
    private int screenshotHotkeyVirtualKey = ScreenshotSettings.DefaultCaptureVirtualKey;

    [ObservableProperty]
    private string screenshotStatusMessage = "Ready to capture the desktop.";

    [ObservableProperty]
    private ImageSource? latestScreenshotPreview;

    [ObservableProperty]
    private string latestScreenshotCaption = "No screenshot captured yet.";

    [ObservableProperty]
    private string macroName = "New Macro";

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
    private string macroSummaryText = "No macro recorded yet.";

    [ObservableProperty]
    private string macroStatusMessage = "Ready for recording or playback setup.";

    [ObservableProperty]
    private SavedMacroEntry? selectedSavedMacro;

    [ObservableProperty]
    private bool isDarkMode;

    [ObservableProperty]
    private bool isCtrlWheelResizeEnabled = true;

    [ObservableProperty]
    private string settingsStatusMessage = "Dark mode will match Windows the first time the app runs.";

    public async Task InitializeAsync()
    {
        if (initialized)
        {
            return;
        }

        var settings = await settingsStore.LoadAsync();
        ApplySettings(settings);
        RefreshSavedMacrosInternal();
        StatusMessage = "Ready.";
        AddActivityLog("Settings loaded.");
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
        StatusMessage = autoClickerController.IsRunning ? "Automation running." : "Automation stopped.";
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        await SaveSettingsAsync("Settings saved.", updateStatusOnSuccess: true);
    }

    [RelayCommand]
    private async Task OpenHotkeySettingsAsync()
    {
        var updated = hotkeySettingsDialogService.Edit(hotkeySettings.Clone());
        if (updated is null)
        {
            return;
        }

        var validation = settingsValidator.ValidateHotkeys(updated);
        if (!validation.IsValid)
        {
            StatusMessage = validation.Summary;
            return;
        }

        hotkeySettings = updated;
        RefreshHotkeyLabels();
        HotkeysChanged?.Invoke(this, EventArgs.Empty);
        await SaveSettingsAsync("Hotkeys updated.", updateStatusOnSuccess: true);
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
        StatusMessage = $"Captured coordinates: {FixedX}, {FixedY}.";
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

            ScreenshotStatusMessage = "Opened screenshot folder.";
            AddScreenshotLog("Opened screenshot folder.");
        }
        catch (Exception ex)
        {
            ScreenshotStatusMessage = $"Unable to open screenshot folder: {ex.Message}";
            AddScreenshotLog($"Unable to open screenshot folder: {ex.Message}");
        }
    }

    private bool CanStartMacroRecording => !IsMacroRecording && !IsMacroPlaying;

    [RelayCommand(CanExecute = nameof(CanStartMacroRecording))]
    private void StartMacroRecording()
    {
        try
        {
            var name = string.IsNullOrWhiteSpace(MacroName) ? "New Macro" : MacroName.Trim();
            macroService.StartRecording(name, RecordMacroMouseMovement);
            MacroName = name;
            IsMacroRecording = true;
            HasRecordedMacro = false;
            MacroSummaryText = $"Recording '{name}'...";
            MacroStatusMessage = RecordMacroMouseMovement
                ? $"Recording '{name}' with mouse movement. Input inside this window is ignored while it stays focused."
                : $"Recording '{name}' without mouse movement. Input inside this window is ignored while it stays focused.";
            AddMacroLog($"Started recording '{name}'.");
        }
        catch (Exception ex)
        {
            MacroStatusMessage = $"Unable to start recording: {ex.Message}";
            AddMacroLog($"Unable to start recording: {ex.Message}");
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
            MacroStatusMessage = "Cannot start recording while a macro is playing.";
            AddMacroLog("Record hotkey ignored because a macro is currently playing.");
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
                ? $"{macro.Name}: {macro.Events.Count} events over {macro.Duration.TotalMilliseconds:N0} ms"
                : $"{macro.Name}: no input captured";
            MacroStatusMessage = HasRecordedMacro
                ? $"Stopped recording '{macro.Name}'."
                : $"Stopped recording '{macro.Name}', but no input was captured.";
            AddMacroLog($"Stopped recording '{macro.Name}' with {macro.Events.Count} events.");
        }
        catch (Exception ex)
        {
            MacroStatusMessage = $"Unable to stop recording: {ex.Message}";
            AddMacroLog($"Unable to stop recording: {ex.Message}");
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
                MacroStatusMessage = "There is no recorded macro to play.";
                AddMacroLog("Playback requested, but no recorded macro is available.");
                return;
            }

            var count = Math.Max(1, MacroPlaybackCount);
            MacroPlaybackCount = count;
            IsMacroPlaying = true;
            MacroStatusMessage = $"Playing '{macro.Name}' x{count}.";
            AddMacroLog($"Playing '{macro.Name}' x{count}.");

            await macroService.PlayAsync(count);

            MacroStatusMessage = $"Finished playing '{macro.Name}'.";
            AddMacroLog($"Finished playing '{macro.Name}'.");
        }
        catch (Exception ex)
        {
            MacroStatusMessage = $"Unable to play macro: {ex.Message}";
            AddMacroLog($"Unable to play macro: {ex.Message}");
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
                MacroStatusMessage = "There is no recorded macro to save.";
                AddMacroLog("Save requested, but no macro is available.");
                return;
            }

            var suggestedName = string.IsNullOrWhiteSpace(MacroName) ? macro.Name : MacroName.Trim();
            var chosenName = macroNamePromptService.PromptForName(suggestedName);
            if (string.IsNullOrWhiteSpace(chosenName))
            {
                MacroStatusMessage = "Save canceled.";
                AddMacroLog("Save canceled.");
                return;
            }

            var macroToSave = macro with { Name = chosenName.Trim() };
            var filePath = macroLibraryService.GetSavePath(macroToSave.Name);
            await macroFileStore.SaveAsync(filePath, macroToSave);
            macroService.SetCurrentMacro(macroToSave);
            ApplyLoadedMacro(macroToSave);
            RefreshSavedMacrosInternal(filePath);
            MacroStatusMessage = $"Saved macro to Macros\\{Path.GetFileName(filePath)}.";
            AddMacroLog($"Saved macro to {filePath}.");
        }
        catch (Exception ex)
        {
            MacroStatusMessage = $"Unable to save macro: {ex.Message}";
            AddMacroLog($"Unable to save macro: {ex.Message}");
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
                MacroStatusMessage = "Load canceled.";
                AddMacroLog("Load canceled.");
                return;
            }

            var macro = await macroFileStore.LoadAsync(filePath);
            macroService.SetCurrentMacro(macro);
            ApplyLoadedMacro(macro);
            MacroStatusMessage = $"Loaded macro from {Path.GetFileName(filePath)}.";
            AddMacroLog($"Loaded macro from {filePath}.");
        }
        catch (Exception ex)
        {
            MacroStatusMessage = $"Unable to load macro: {ex.Message}";
            AddMacroLog($"Unable to load macro: {ex.Message}");
        }
    }

    private bool CanLoadSelectedSavedMacro => SelectedSavedMacro is not null && !IsMacroRecording && !IsMacroPlaying;

    [RelayCommand(CanExecute = nameof(CanLoadSelectedSavedMacro))]
    private async Task LoadSelectedSavedMacroAsync()
    {
        if (SelectedSavedMacro is null)
        {
            MacroStatusMessage = "Choose a saved macro first.";
            AddMacroLog("Load selected requested, but no saved macro is selected.");
            return;
        }

        try
        {
            var macro = await macroFileStore.LoadAsync(SelectedSavedMacro.FilePath);
            macroService.SetCurrentMacro(macro);
            ApplyLoadedMacro(macro);
            MacroStatusMessage = $"Loaded saved macro '{SelectedSavedMacro.DisplayName}'.";
            AddMacroLog($"Loaded saved macro from {SelectedSavedMacro.FilePath}.");
        }
        catch (Exception ex)
        {
            MacroStatusMessage = $"Unable to load saved macro: {ex.Message}";
            AddMacroLog($"Unable to load saved macro: {ex.Message}");
        }
    }

    private bool CanEditSelectedSavedMacro => SelectedSavedMacro is not null && !IsMacroRecording && !IsMacroPlaying;

    [RelayCommand(CanExecute = nameof(CanEditSelectedSavedMacro))]
    private async Task EditSelectedSavedMacroAsync()
    {
        if (SelectedSavedMacro is null)
        {
            MacroStatusMessage = "Choose a saved macro first.";
            AddMacroLog("Edit requested, but no saved macro is selected.");
            return;
        }

        try
        {
            var originalPath = SelectedSavedMacro.FilePath;
            var macro = await macroFileStore.LoadAsync(originalPath);
            var editedMacro = macroEditorDialogService.Edit(macro);
            if (editedMacro is null)
            {
                MacroStatusMessage = "Edit canceled.";
                AddMacroLog("Edit canceled.");
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
            RefreshSavedMacrosInternal(updatedPath);
            MacroStatusMessage = $"Saved edits to '{editedMacro.Name}'.";
            AddMacroLog($"Saved edited macro to {updatedPath}.");
        }
        catch (Exception ex)
        {
            MacroStatusMessage = $"Unable to edit saved macro: {ex.Message}";
            AddMacroLog($"Unable to edit saved macro: {ex.Message}");
        }
    }

    [RelayCommand]
    private void RefreshSavedMacros()
    {
        RefreshSavedMacrosInternal();
        MacroStatusMessage = HasSavedMacros
            ? $"Found {SavedMacros.Count} saved macro{(SavedMacros.Count == 1 ? string.Empty : "s")}."
            : "No saved macros found in the default macros folder.";
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

            MacroStatusMessage = "Opened the saved macros folder.";
            AddMacroLog("Opened the saved macros folder.");
        }
        catch (Exception ex)
        {
            MacroStatusMessage = $"Unable to open the saved macros folder: {ex.Message}";
            AddMacroLog($"Unable to open the saved macros folder: {ex.Message}");
        }
    }

    [RelayCommand]
    private void ClearMacroLog()
    {
        MacroLogEntries.Clear();
        AddMacroLog("Macro log cleared.");
        MacroStatusMessage = "Macro log cleared.";
    }

    public HotkeySettings CurrentHotkeys => hotkeySettings.Clone();

    public ScreenshotSettings CurrentScreenshotSettings => BuildScreenshotSettings();

    public MacroSettings CurrentMacroSettings => BuildMacroSettings();

    public Task<bool> AutoSaveAsync() => SaveSettingsAsync("Settings auto-saved.", updateStatusOnSuccess: false, addActivityLogOnSuccess: false);

    public void CaptureCustomKey(Key key)
    {
        var virtualKey = HotkeyDisplayNameFormatter.ToVirtualKey(key);
        var displayName = HotkeyDisplayNameFormatter.FormatVirtualKey(virtualKey);

        CustomInputKind = AutoClicker.Core.Enums.CustomInputKind.Keyboard;
        CustomKeyVirtualKey = virtualKey;
        CustomMouseButton = ClickMouseButton.Left;
        CustomKeyDisplayText = displayName;
        StatusMessage = $"Custom key set to {displayName}.";
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
        ScreenshotStatusMessage = $"Screenshot hotkey set to {ScreenshotHotkeyDisplay}.";
        AddScreenshotLog($"Screenshot hotkey set to {ScreenshotHotkeyDisplay}.");
        HotkeysChanged?.Invoke(this, EventArgs.Empty);
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
        MacroStatusMessage = $"Macro hotkey set to {MacroHotkeyDisplay}.";
        AddMacroLog($"Macro hotkey set to {MacroHotkeyDisplay}.");
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
        MacroStatusMessage = $"Macro record hotkey set to {MacroRecordHotkeyDisplay}.";
        AddMacroLog($"Macro record hotkey set to {MacroRecordHotkeyDisplay}.");
        HotkeysChanged?.Invoke(this, EventArgs.Empty);
    }

    public void CaptureCustomMouseButton(ClickMouseButton mouseButton)
    {
        CustomInputKind = AutoClicker.Core.Enums.CustomInputKind.MouseButton;
        CustomKeyVirtualKey = 0;
        CustomMouseButton = mouseButton;
        CustomKeyDisplayText = FormatMouseButtonDisplay(mouseButton);
        StatusMessage = $"Custom input set to {CustomKeyDisplayText}.";
    }

    public void UpdateRunningState(bool running)
    {
        IsRunning = running;
        WindowTitle = running ? "MultiTool - Running..." : "MultiTool";
        RefreshHotkeyLabels();
    }

    public void SetStatus(string message)
    {
        StatusMessage = message;
    }

    public async Task HandleHotkeyAsync(HotkeyAction action)
    {
        switch (action)
        {
            case HotkeyAction.Toggle:
                await ToggleAsync();
                return;
            case HotkeyAction.ScreenshotCapture:
                if (await screenshotOptionsDialogService.TryHandleCaptureHotkeyAsync())
                {
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
        ApplyInstallerSettings(settings.Installer);
        suppressThemeChange = true;
        IsDarkMode = settings.Ui.IsDarkMode ?? themeService.GetSystemPrefersDarkMode();
        IsCtrlWheelResizeEnabled = settings.Ui.EnableCtrlWheelResize;
        suppressThemeChange = false;
        themeService.ApplyTheme(IsDarkMode);
        SettingsStatusMessage = IsDarkMode ? "Dark mode is on." : "Dark mode is off.";
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
            Ui = new UiSettings
            {
                IsDarkMode = IsDarkMode,
                EnableCtrlWheelResize = IsCtrlWheelResizeEnabled,
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
        };

    private void RefreshHotkeyLabels()
    {
        var actionText = IsRunning ? "Toggle Off" : "Toggle On";
        ToggleButtonText = $"{actionText} ({hotkeySettings.Toggle.DisplayName})";
    }

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
    }

    partial void OnLatestScreenshotPreviewChanged(ImageSource? value)
    {
        OnPropertyChanged(nameof(HasLatestScreenshot));
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
        SettingsStatusMessage = value ? "Dark mode is on." : "Dark mode is off.";
        ScheduleSettingsAutoSave();

        if (initialized)
        {
            AddActivityLog(value ? "Dark mode enabled." : "Dark mode disabled.");
        }
    }

    partial void OnIsCtrlWheelResizeEnabledChanged(bool value)
    {
        if (suppressThemeChange)
        {
            return;
        }

        SettingsStatusMessage = value
            ? "Ctrl + mouse wheel window resize is on."
            : "Ctrl + mouse wheel window resize is off.";
        ScheduleSettingsAutoSave();

        if (initialized)
        {
            AddActivityLog(value
                ? "Enabled Ctrl + mouse wheel window resize."
                : "Disabled Ctrl + mouse wheel window resize.");
        }
    }

    private void RefreshMacroCommandStates()
    {
        StartMacroRecordingCommand.NotifyCanExecuteChanged();
        StopMacroRecordingCommand.NotifyCanExecuteChanged();
        PlayMacroCommand.NotifyCanExecuteChanged();
        SaveMacroCommand.NotifyCanExecuteChanged();
        LoadMacroCommand.NotifyCanExecuteChanged();
        LoadSelectedSavedMacroCommand.NotifyCanExecuteChanged();
        EditSelectedSavedMacroCommand.NotifyCanExecuteChanged();
    }

    private string GetCustomKeyDisplayName() =>
        !string.IsNullOrWhiteSpace(CustomKeyDisplayText) && CustomInputKind != AutoClicker.Core.Enums.CustomInputKind.None
            ? CustomKeyDisplayText
            : string.Empty;

    private static string GetCustomKeyDisplayText(ClickSettings settings) =>
        !string.IsNullOrWhiteSpace(settings.CustomKeyDisplayName)
            ? settings.CustomKeyDisplayName
            : "Click here and press a key or mouse button";

    private static string FormatMouseButtonDisplay(ClickMouseButton mouseButton) =>
        mouseButton switch
        {
            ClickMouseButton.Left => "Left Mouse Button",
            ClickMouseButton.Right => "Right Mouse Button",
            ClickMouseButton.Middle => "Middle Mouse Button",
            ClickMouseButton.XButton1 => "Mouse Button 4",
            ClickMouseButton.XButton2 => "Mouse Button 5",
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

    private void UpdateLatestScreenshotPreview(string filePath, string fileName)
    {
        LatestScreenshotPreview = LoadPreview(filePath);
        LatestScreenshotCaption = fileName;
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
            ? $"{macro.Name}: {macro.Events.Count} events over {macro.Duration.TotalMilliseconds:N0} ms"
            : $"{macro.Name}: no input captured";

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
            MacroStatusMessage = $"Saved macros folder is unavailable: {ex.Message}";
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
    }

    [RelayCommand]
    private void BrowseScreenshotFolder()
    {
        var selectedPath = folderPickerService.PickFolder(ScreenshotFolderPath, "Select the folder to save screenshots in");
        if (string.IsNullOrWhiteSpace(selectedPath))
        {
            ScreenshotStatusMessage = "Folder selection canceled.";
            AddScreenshotLog("Folder selection canceled.");
            return;
        }

        ScreenshotFolderPath = selectedPath;
        ScreenshotStatusMessage = $"Screenshot folder set to {selectedPath}.";
        AddScreenshotLog($"Screenshot folder set to {selectedPath}.");
    }

    [RelayCommand]
    private void CopyActivityLog()
    {
        var text = ActivityLogEntries.Count > 0
            ? string.Join(Environment.NewLine, ActivityLogEntries)
            : "Activity log is empty.";

        clipboardTextService.SetText(text);
        SettingsStatusMessage = "Copied activity log to the clipboard.";
        AddActivityLog("Copied the activity log to the clipboard.");
    }

    [RelayCommand]
    private async Task ResetAllSettingsAsync()
    {
        var defaults = DefaultSettingsFactory.Create();
        ApplySettings(defaults);
        HotkeysChanged?.Invoke(this, EventArgs.Empty);

        var saved = await SaveSettingsAsync("Settings reset to defaults.", updateStatusOnSuccess: true);
        SettingsStatusMessage = saved
            ? "All settings were reset to defaults."
            : "Reset applied in the UI, but saving the defaults failed.";
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
            ScreenshotStatusMessage = "Video recording was handled in the screenshot options window.";
            AddScreenshotLog("Handled video recording from the screenshot options window.");
            return;
        }

        if (result.WasCanceled || result.Mode is null)
        {
            ScreenshotStatusMessage = "Screenshot options canceled.";
            AddScreenshotLog("Screenshot options canceled.");
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
                        ScreenshotStatusMessage = $"Saved {fileName} and copied it to the clipboard.";
                        AddScreenshotLog($"Saved full-screen capture to {path} and copied it to the clipboard.");
                        break;
                    }
                case ScreenshotMode.Area:
                    {
                        var area = screenshotAreaSelectionService.SelectArea();
                        if (area is null)
                        {
                            ScreenshotStatusMessage = "Area capture canceled.";
                            AddScreenshotLog("Area capture canceled.");
                            return;
                        }

                        var path = await screenshotCaptureService.CaptureAreaAsync(area.Value, settings.SaveFolderPath, settings.FilePrefix);
                        var fileName = Path.GetFileName(path);
                        UpdateLatestScreenshotPreview(path, fileName);
                        ScreenshotStatusMessage = $"Saved {fileName} and copied it to the clipboard.";
                        AddScreenshotLog($"Saved area capture to {path} and copied it to the clipboard.");
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
            ScreenshotStatusMessage = $"Screenshot failed: {ex.Message}";
            AddScreenshotLog($"Screenshot failed: {ex.Message}");
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
            StatusMessage = $"Unable to save settings: {ex.Message}";
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
