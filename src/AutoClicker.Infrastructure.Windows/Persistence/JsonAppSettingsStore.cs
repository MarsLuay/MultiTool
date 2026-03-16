using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using AutoClicker.Core.Defaults;
using AutoClicker.Core.Enums;
using AutoClicker.Core.Models;
using AutoClicker.Core.Services;

namespace AutoClicker.Infrastructure.Windows.Persistence;

public sealed class JsonAppSettingsStore : IAppSettingsStore
{
    private readonly string settingsFilePath;
    private readonly string? previousSettingsFilePath;
    private readonly string legacySettingsFilePath;
    private readonly JsonSerializerOptions serializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };

    public JsonAppSettingsStore()
        : this(
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "MultiTool",
                "settings.json"),
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "AutoClicker",
                "settings.json"),
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "AutoClicker_Settings.json"))
    {
    }

    public JsonAppSettingsStore(string settingsFilePath, string legacySettingsFilePath)
        : this(settingsFilePath, null, legacySettingsFilePath)
    {
    }

    public JsonAppSettingsStore(string settingsFilePath, string? previousSettingsFilePath, string legacySettingsFilePath)
    {
        this.settingsFilePath = settingsFilePath;
        this.previousSettingsFilePath = previousSettingsFilePath;
        this.legacySettingsFilePath = legacySettingsFilePath;
    }

    public async Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        EnsureSettingsDirectory();

        if (File.Exists(settingsFilePath))
        {
            return await LoadCurrentAsync(settingsFilePath, cancellationToken).ConfigureAwait(false);
        }

        if (!string.IsNullOrWhiteSpace(previousSettingsFilePath) && File.Exists(previousSettingsFilePath))
        {
            var migratedCurrent = await LoadCurrentAsync(previousSettingsFilePath, cancellationToken).ConfigureAwait(false);
            await SaveAsync(migratedCurrent, cancellationToken).ConfigureAwait(false);
            return migratedCurrent;
        }

        if (File.Exists(legacySettingsFilePath))
        {
            var migrated = await LoadLegacyAsync(cancellationToken).ConfigureAwait(false);
            await SaveAsync(migrated, cancellationToken).ConfigureAwait(false);
            return migrated;
        }

        return DefaultSettingsFactory.Create();
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        EnsureSettingsDirectory();

        await using var stream = File.Create(settingsFilePath);
        await JsonSerializer.SerializeAsync(stream, settings, serializerOptions, cancellationToken).ConfigureAwait(false);
    }

    private async Task<AppSettings> LoadCurrentAsync(string filePath, CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = File.OpenRead(filePath);
            var settings = await JsonSerializer.DeserializeAsync<AppSettings>(stream, serializerOptions, cancellationToken)
                .ConfigureAwait(false);
            return NormalizeSettings(settings ?? DefaultSettingsFactory.Create());
        }
        catch (JsonException)
        {
            BackupCorruptSettingsFile(filePath);
            return DefaultSettingsFactory.Create();
        }
    }

    private async Task<AppSettings> LoadLegacyAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = File.OpenRead(legacySettingsFilePath);
            var legacy = await JsonSerializer.DeserializeAsync<LegacyApplicationSettings>(stream, serializerOptions, cancellationToken)
                .ConfigureAwait(false);
            return NormalizeSettings(MapLegacySettings(legacy));
        }
        catch (JsonException)
        {
            return DefaultSettingsFactory.Create();
        }
    }

    private static AppSettings MapLegacySettings(LegacyApplicationSettings? legacy)
    {
        if (legacy is null)
        {
            return DefaultSettingsFactory.Create();
        }

        return new AppSettings
        {
            Clicker = new ClickSettings
            {
                Hours = legacy.AutoClickerSettings.Hours,
                Minutes = legacy.AutoClickerSettings.Minutes,
                Seconds = legacy.AutoClickerSettings.Seconds,
                Milliseconds = legacy.AutoClickerSettings.Milliseconds <= 0 ? 1 : legacy.AutoClickerSettings.Milliseconds,
                MouseButton = ParseEnum(legacy.AutoClickerSettings.SelectedMouseButton, ClickMouseButton.Left),
                ClickType = ParseEnum(legacy.AutoClickerSettings.SelectedMouseAction, ClickKind.Single),
                RepeatMode = ParseEnum(legacy.AutoClickerSettings.SelectedRepeatMode, RepeatMode.Infinite),
                LocationMode = ParseEnum(legacy.AutoClickerSettings.SelectedLocationMode, ClickLocationMode.CurrentCursor),
                FixedX = legacy.AutoClickerSettings.PickedXValue,
                FixedY = legacy.AutoClickerSettings.PickedYValue,
                RepeatCount = legacy.AutoClickerSettings.SelectedTimesToRepeat <= 0 ? 1 : legacy.AutoClickerSettings.SelectedTimesToRepeat,
            },
            Hotkeys = new HotkeySettings
            {
                Toggle = MapLegacyBinding(legacy.HotkeySettings.ToggleHotkey, HotkeySettings.CreateDefaultToggleBinding()),
                AllowModifierVariants = legacy.HotkeySettings.IncludeModifiers,
            },
            Screenshot = new ScreenshotSettings(),
            Macro = new MacroSettings(),
            Installer = new InstallerSettings(),
            Tools = new ToolSettings(),
            Ui = new UiSettings(),
        };
    }

    private static AppSettings NormalizeSettings(AppSettings settings)
    {
        settings.Clicker ??= new ClickSettings();
        settings.Hotkeys ??= new HotkeySettings();
        settings.Screenshot ??= new ScreenshotSettings();
        settings.Macro ??= new MacroSettings();
        settings.Installer ??= new InstallerSettings();
        settings.Tools ??= new ToolSettings();
        settings.Ui ??= new UiSettings();
        settings.Hotkeys.Toggle ??= HotkeySettings.CreateDefaultToggleBinding();
        settings.Screenshot.CaptureHotkey ??= ScreenshotSettings.CreateDefaultCaptureBinding();
        settings.Macro.PlayHotkey ??= MacroSettings.CreateDefaultPlayBinding();
        settings.Macro.RecordHotkey ??= MacroSettings.CreateDefaultRecordBinding();
        settings.Macro.AssignedHotkeys ??= [];
        settings.Installer.SelectedPackageIds ??= [];
        settings.Installer.SelectedCleanupPackageIds ??= [];
        settings.Installer.PackageOptions ??= [];
        settings.Tools.EmptyDirectoryScanMaxFolderCounts ??= new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var packageOptions in settings.Installer.PackageOptions)
        {
            packageOptions.PackageId ??= string.Empty;
            packageOptions.SelectedOptionIds ??= [];
        }

        settings.Tools.ShortcutHotkeyScanMaxFolderCount = Math.Max(settings.Tools.ShortcutHotkeyScanMaxFolderCount, 0);
        settings.Tools.EmptyDirectoryScanMaxFolderCounts = settings.Tools.EmptyDirectoryScanMaxFolderCounts
            .Where(static entry => !string.IsNullOrWhiteSpace(entry.Key) && entry.Value > 0)
            .ToDictionary(
                static entry => entry.Key,
                static entry => entry.Value,
                StringComparer.OrdinalIgnoreCase);

        if (!HasValidHotkeyBinding(settings.Hotkeys.Toggle))
        {
            settings.Hotkeys.Toggle = HotkeySettings.CreateDefaultToggleBinding();
        }
        else if (settings.Hotkeys.Toggle.InputKind == HotkeyInputKind.Keyboard
                 && settings.Hotkeys.Toggle.VirtualKey == HotkeySettings.LegacyDefaultToggleVirtualKey)
        {
            settings.Hotkeys.Toggle = HotkeySettings.CreateDefaultToggleBinding();
        }

        if (!HasValidHotkeyBinding(settings.Screenshot.CaptureHotkey)
            || settings.Screenshot.CaptureHotkey.InputKind != HotkeyInputKind.Keyboard)
        {
            settings.Screenshot.CaptureHotkey = ScreenshotSettings.CreateDefaultCaptureBinding();
        }

        if (!HasValidHotkeyBinding(settings.Macro.PlayHotkey)
            || settings.Macro.PlayHotkey.InputKind != HotkeyInputKind.Keyboard)
        {
            settings.Macro.PlayHotkey = MacroSettings.CreateDefaultPlayBinding();
        }

        if (!HasValidHotkeyBinding(settings.Macro.RecordHotkey)
            || settings.Macro.RecordHotkey.InputKind != HotkeyInputKind.Keyboard)
        {
            settings.Macro.RecordHotkey = MacroSettings.CreateDefaultRecordBinding();
        }

        settings.Macro.AssignedHotkeys = NormalizeMacroHotkeyAssignments(settings.Macro.AssignedHotkeys);

        if (string.IsNullOrWhiteSpace(settings.Screenshot.SaveFolderPath))
        {
            settings.Screenshot.SaveFolderPath = ScreenshotSettings.GetDefaultSaveFolderPath();
        }
        else if (string.Equals(
                     settings.Screenshot.SaveFolderPath,
                     ScreenshotSettings.GetLegacyDefaultSaveFolderPath(),
                     StringComparison.OrdinalIgnoreCase))
        {
            settings.Screenshot.SaveFolderPath = ScreenshotSettings.GetDefaultSaveFolderPath();
        }

        if (string.IsNullOrWhiteSpace(settings.Screenshot.FilePrefix))
        {
            settings.Screenshot.FilePrefix = "Screenshot";
        }

        if (settings.Clicker.MouseButton == ClickMouseButton.Custom && settings.Clicker.CustomInputKind == CustomInputKind.None)
        {
            if (settings.Clicker.CustomKeyVirtualKey > 0 && !string.IsNullOrWhiteSpace(settings.Clicker.CustomKeyDisplayName))
            {
                settings.Clicker.CustomInputKind = CustomInputKind.Keyboard;
            }
            else if (settings.Clicker.CustomMouseButton is ClickMouseButton.Left
                     or ClickMouseButton.Right
                     or ClickMouseButton.Middle
                     or ClickMouseButton.XButton1
                     or ClickMouseButton.XButton2)
            {
                settings.Clicker.CustomInputKind = CustomInputKind.MouseButton;
            }
        }

        return settings;
    }

    private static bool HasValidHotkeyBinding(HotkeyBinding binding) =>
        binding.InputKind switch
        {
            HotkeyInputKind.Keyboard => binding.VirtualKey > 0 && !string.IsNullOrWhiteSpace(binding.DisplayName),
            HotkeyInputKind.MouseButton => binding.MouseButton is ClickMouseButton.Right
                or ClickMouseButton.Middle
                or ClickMouseButton.XButton1
                or ClickMouseButton.XButton2
                && !string.IsNullOrWhiteSpace(binding.DisplayName),
            _ => false,
        };

    private static List<MacroHotkeyAssignment> NormalizeMacroHotkeyAssignments(IEnumerable<MacroHotkeyAssignment> assignments)
    {
        var normalizedAssignments = new List<MacroHotkeyAssignment>();
        var seenMacroPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var assignment in assignments)
        {
            if (assignment is null)
            {
                continue;
            }

            assignment.Id = string.IsNullOrWhiteSpace(assignment.Id)
                ? Guid.NewGuid().ToString("N")
                : assignment.Id.Trim();
            assignment.MacroFilePath ??= string.Empty;
            assignment.MacroDisplayName ??= string.Empty;
            assignment.Hotkey ??= new HotkeyBinding();

            if (string.IsNullOrWhiteSpace(assignment.MacroFilePath)
                || string.IsNullOrWhiteSpace(assignment.MacroDisplayName)
                || !seenMacroPaths.Add(assignment.MacroFilePath)
                || !HasValidHotkeyBinding(assignment.Hotkey)
                || assignment.Hotkey.InputKind != HotkeyInputKind.Keyboard)
            {
                continue;
            }

            normalizedAssignments.Add(assignment.Clone());
        }

        return normalizedAssignments;
    }

    private static TEnum ParseEnum<TEnum>(int value, TEnum fallback)
        where TEnum : struct, Enum =>
        Enum.IsDefined(typeof(TEnum), value) ? (TEnum)Enum.ToObject(typeof(TEnum), value) : fallback;

    private static HotkeyBinding MapLegacyBinding(LegacyKeyMapping? legacy, HotkeyBinding fallback)
    {
        if (legacy is null || legacy.VirtualKeyCode <= 0 || string.IsNullOrWhiteSpace(legacy.DisplayName))
        {
            return fallback;
        }

        return new HotkeyBinding(legacy.VirtualKeyCode, legacy.DisplayName);
    }

    private void EnsureSettingsDirectory()
    {
        var directory = Path.GetDirectoryName(settingsFilePath)
            ?? throw new InvalidOperationException("The settings file path is invalid.");
        Directory.CreateDirectory(directory);
    }

    private void BackupCorruptSettingsFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return;
        }

        var directory = Path.GetDirectoryName(filePath) ?? string.Empty;
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        var extension = Path.GetExtension(filePath);
        var backupPath = Path.Combine(directory, $"{fileName}.corrupt.{DateTime.UtcNow:yyyyMMddHHmmss}{extension}");
        File.Move(filePath, backupPath, true);
    }

    private sealed class LegacyApplicationSettings
    {
        public LegacyHotkeySettings HotkeySettings { get; set; } = new();

        public LegacyClickSettings AutoClickerSettings { get; set; } = new();
    }

    private sealed class LegacyHotkeySettings
    {
        public LegacyKeyMapping? StartHotkey { get; set; }

        public LegacyKeyMapping? StopHotkey { get; set; }

        public LegacyKeyMapping? ToggleHotkey { get; set; }

        public bool IncludeModifiers { get; set; }
    }

    private sealed class LegacyClickSettings
    {
        public int Hours { get; set; }

        public int Minutes { get; set; }

        public int Seconds { get; set; }

        public int Milliseconds { get; set; }

        public int SelectedMouseButton { get; set; }

        public int SelectedMouseAction { get; set; }

        public int SelectedRepeatMode { get; set; }

        public int SelectedLocationMode { get; set; }

        public int PickedXValue { get; set; }

        public int PickedYValue { get; set; }

        public int SelectedTimesToRepeat { get; set; }
    }

    private sealed class LegacyKeyMapping
    {
        [JsonPropertyName("display_name")]
        public string DisplayName { get; set; } = string.Empty;

        [JsonPropertyName("virtual_key_code")]
        public int VirtualKeyCode { get; set; }
    }
}
