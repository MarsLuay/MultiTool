using AutoClicker.Core.Enums;
using AutoClicker.Core.Models;
using AutoClicker.Infrastructure.Windows.Persistence;
using FluentAssertions;

namespace AutoClicker.Infrastructure.Windows.Tests;

public sealed class JsonAppSettingsStoreTests : IDisposable
{
    private readonly string workingDirectory = Path.Combine(Path.GetTempPath(), "AutoClicker.Tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task LoadAsync_ShouldMigrateLegacySettingsFile()
    {
        Directory.CreateDirectory(workingDirectory);

        var settingsFilePath = Path.Combine(workingDirectory, "settings.json");
        var legacyFilePath = Path.Combine(workingDirectory, "AutoClicker_Settings.json");

        await File.WriteAllTextAsync(
            legacyFilePath,
            """
            {
              "HotkeySettings": {
                "StartHotkey": { "display_name": "F6", "virtual_key_code": 117 },
                "StopHotkey": { "display_name": "F7", "virtual_key_code": 118 },
                "ToggleHotkey": { "display_name": "F8", "virtual_key_code": 119 },
                "IncludeModifiers": true
              },
              "AutoClickerSettings": {
                "Hours": 0,
                "Minutes": 0,
                "Seconds": 0,
                "Milliseconds": 25,
                "SelectedMouseButton": 1,
                "SelectedMouseAction": 1,
                "SelectedRepeatMode": 1,
                "SelectedLocationMode": 1,
                "PickedXValue": 320,
                "PickedYValue": 640,
                "SelectedTimesToRepeat": 4
              }
            }
            """);

        var store = new JsonAppSettingsStore(settingsFilePath, legacyFilePath);

        var settings = await store.LoadAsync();

        settings.Clicker.Milliseconds.Should().Be(25);
        settings.Clicker.MouseButton.Should().Be(ClickMouseButton.Right);
        settings.Clicker.ClickType.Should().Be(ClickKind.Double);
        settings.Clicker.RepeatMode.Should().Be(RepeatMode.Count);
        settings.Clicker.LocationMode.Should().Be(ClickLocationMode.FixedPoint);
        settings.Clicker.FixedX.Should().Be(320);
        settings.Clicker.FixedY.Should().Be(640);
        settings.Hotkeys.Toggle.VirtualKey.Should().Be(HotkeySettings.DefaultToggleVirtualKey);
        settings.Hotkeys.Toggle.DisplayName.Should().Be(HotkeySettings.DefaultToggleDisplayName);
        settings.Hotkeys.AllowModifierVariants.Should().BeTrue();
        File.Exists(settingsFilePath).Should().BeTrue();
    }

    [Fact]
    public async Task LoadAsync_ShouldMigratePreviousCurrentSettingsFile()
    {
        Directory.CreateDirectory(workingDirectory);

        var settingsFilePath = Path.Combine(workingDirectory, "MultiTool", "settings.json");
        var previousSettingsFilePath = Path.Combine(workingDirectory, "AutoClicker", "settings.json");
        var legacyFilePath = Path.Combine(workingDirectory, "AutoClicker_Settings.json");

        Directory.CreateDirectory(Path.GetDirectoryName(previousSettingsFilePath)!);

        var previousStore = new JsonAppSettingsStore(previousSettingsFilePath, legacyFilePath);
        await previousStore.SaveAsync(
            new AppSettings
            {
                Clicker = new ClickSettings
                {
                    Milliseconds = 7,
                    MouseButton = ClickMouseButton.Middle,
                },
                Hotkeys = new HotkeySettings(),
                Screenshot = new ScreenshotSettings
                {
                    FilePrefix = "Migrated",
                },
                Macro = new MacroSettings
                {
                    RecordMouseMovement = false,
                },
                Ui = new UiSettings
                {
                    IsDarkMode = true,
                },
            });

        var store = new JsonAppSettingsStore(settingsFilePath, previousSettingsFilePath, legacyFilePath);

        var settings = await store.LoadAsync();

        settings.Clicker.Milliseconds.Should().Be(7);
        settings.Clicker.MouseButton.Should().Be(ClickMouseButton.Middle);
        settings.Screenshot.FilePrefix.Should().Be("Migrated");
        settings.Macro.RecordMouseMovement.Should().BeFalse();
        settings.Ui.IsDarkMode.Should().BeTrue();
        File.Exists(settingsFilePath).Should().BeTrue();
    }

    [Fact]
    public async Task SaveAsync_AndLoadAsync_ShouldRoundTripCustomKeySettings()
    {
        Directory.CreateDirectory(workingDirectory);

        var settingsFilePath = Path.Combine(workingDirectory, "settings.json");
        var legacyFilePath = Path.Combine(workingDirectory, "AutoClicker_Settings.json");
        var store = new JsonAppSettingsStore(settingsFilePath, legacyFilePath);

        var expected = new AppSettings
        {
            Clicker = new ClickSettings
            {
                Milliseconds = 1,
                MouseButton = ClickMouseButton.Custom,
                CustomInputKind = CustomInputKind.Keyboard,
                CustomKeyVirtualKey = 0x51,
                CustomKeyDisplayName = "Q",
                ClickType = ClickKind.Hold,
            },
            Hotkeys = new HotkeySettings(),
        };

        await store.SaveAsync(expected);
        var actual = await store.LoadAsync();

        actual.Clicker.MouseButton.Should().Be(ClickMouseButton.Custom);
        actual.Clicker.CustomKeyVirtualKey.Should().Be(0x51);
        actual.Clicker.CustomKeyDisplayName.Should().Be("Q");
        actual.Clicker.ClickType.Should().Be(ClickKind.Hold);
    }

    [Fact]
    public async Task SaveAsync_AndLoadAsync_ShouldRoundTripCustomMouseButtonSettings()
    {
        Directory.CreateDirectory(workingDirectory);

        var settingsFilePath = Path.Combine(workingDirectory, "settings.json");
        var legacyFilePath = Path.Combine(workingDirectory, "AutoClicker_Settings.json");
        var store = new JsonAppSettingsStore(settingsFilePath, legacyFilePath);

        var expected = new AppSettings
        {
            Clicker = new ClickSettings
            {
                Milliseconds = 1,
                MouseButton = ClickMouseButton.Custom,
                CustomInputKind = CustomInputKind.MouseButton,
                CustomMouseButton = ClickMouseButton.XButton2,
                CustomKeyDisplayName = "Mouse Button 5",
                ClickType = ClickKind.Single,
            },
            Hotkeys = new HotkeySettings(),
        };

        await store.SaveAsync(expected);
        var actual = await store.LoadAsync();

        actual.Clicker.MouseButton.Should().Be(ClickMouseButton.Custom);
        actual.Clicker.CustomInputKind.Should().Be(CustomInputKind.MouseButton);
        actual.Clicker.CustomMouseButton.Should().Be(ClickMouseButton.XButton2);
        actual.Clicker.CustomKeyDisplayName.Should().Be("Mouse Button 5");
    }

    [Fact]
    public async Task SaveAsync_AndLoadAsync_ShouldRoundTripMouseToggleHotkey()
    {
        Directory.CreateDirectory(workingDirectory);

        var settingsFilePath = Path.Combine(workingDirectory, "settings.json");
        var legacyFilePath = Path.Combine(workingDirectory, "AutoClicker_Settings.json");
        var store = new JsonAppSettingsStore(settingsFilePath, legacyFilePath);

        var expected = new AppSettings
        {
            Clicker = new ClickSettings(),
            Hotkeys = new HotkeySettings
            {
                Toggle = new HotkeyBinding(
                    virtualKey: 0,
                    displayName: "Mouse Button 4",
                    inputKind: HotkeyInputKind.MouseButton,
                    mouseButton: ClickMouseButton.XButton1),
                AllowModifierVariants = false,
            },
        };

        await store.SaveAsync(expected);
        var actual = await store.LoadAsync();

        actual.Hotkeys.Toggle.InputKind.Should().Be(HotkeyInputKind.MouseButton);
        actual.Hotkeys.Toggle.MouseButton.Should().Be(ClickMouseButton.XButton1);
        actual.Hotkeys.Toggle.DisplayName.Should().Be("Mouse Button 4");
    }

    [Fact]
    public async Task SaveAsync_AndLoadAsync_ShouldRoundTripScreenshotSettings()
    {
        Directory.CreateDirectory(workingDirectory);

        var settingsFilePath = Path.Combine(workingDirectory, "settings.json");
        var legacyFilePath = Path.Combine(workingDirectory, "AutoClicker_Settings.json");
        var store = new JsonAppSettingsStore(settingsFilePath, legacyFilePath);

        var expected = new AppSettings
        {
            Clicker = new ClickSettings(),
            Hotkeys = new HotkeySettings(),
            Screenshot = new ScreenshotSettings
            {
                CaptureHotkey = new HotkeyBinding(0x6A, "*"),
                SaveFolderPath = @"C:\Users\marwa\Downloads\Shots",
                FilePrefix = "Clip",
            },
        };

        await store.SaveAsync(expected);
        var actual = await store.LoadAsync();

        actual.Screenshot.CaptureHotkey.VirtualKey.Should().Be(0x6A);
        actual.Screenshot.CaptureHotkey.DisplayName.Should().Be("*");
        actual.Screenshot.SaveFolderPath.Should().Be(@"C:\Users\marwa\Downloads\Shots");
        actual.Screenshot.FilePrefix.Should().Be("Clip");
    }

    [Fact]
    public async Task SaveAsync_AndLoadAsync_ShouldRoundTripUiSettings()
    {
        Directory.CreateDirectory(workingDirectory);

        var settingsFilePath = Path.Combine(workingDirectory, "settings.json");
        var legacyFilePath = Path.Combine(workingDirectory, "AutoClicker_Settings.json");
        var store = new JsonAppSettingsStore(settingsFilePath, legacyFilePath);

        var expected = new AppSettings
        {
            Clicker = new ClickSettings(),
            Hotkeys = new HotkeySettings(),
            Screenshot = new ScreenshotSettings(),
            Ui = new UiSettings
            {
                IsDarkMode = true,
                EnableCtrlWheelResize = false,
            },
        };

        await store.SaveAsync(expected);
        var actual = await store.LoadAsync();

        actual.Ui.IsDarkMode.Should().BeTrue();
        actual.Ui.EnableCtrlWheelResize.Should().BeFalse();
    }

    [Fact]
    public async Task SaveAsync_AndLoadAsync_ShouldRoundTripToolScanMaxCache()
    {
        Directory.CreateDirectory(workingDirectory);

        var settingsFilePath = Path.Combine(workingDirectory, "settings.json");
        var legacyFilePath = Path.Combine(workingDirectory, "AutoClicker_Settings.json");
        var store = new JsonAppSettingsStore(settingsFilePath, legacyFilePath);

        var expected = new AppSettings
        {
            Clicker = new ClickSettings(),
            Hotkeys = new HotkeySettings(),
            Screenshot = new ScreenshotSettings(),
            Macro = new MacroSettings(),
            Installer = new InstallerSettings(),
            Tools = new ToolSettings
            {
                ShortcutHotkeyScanMaxFolderCount = 420123,
                EmptyDirectoryScanMaxFolderCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
                {
                    [@"C:\Users\Marwan"] = 1250,
                    [@"D:\Archive"] = 89,
                },
            },
        };

        await store.SaveAsync(expected);
        var actual = await store.LoadAsync();

        actual.Tools.ShortcutHotkeyScanMaxFolderCount.Should().Be(420123);
        actual.Tools.EmptyDirectoryScanMaxFolderCounts.Should().BeEquivalentTo(
            new Dictionary<string, int>
            {
                [@"C:\Users\Marwan"] = 1250,
                [@"D:\Archive"] = 89,
            });
    }

    [Fact]
    public async Task SaveAsync_AndLoadAsync_ShouldRoundTripInstallerSelections()
    {
        Directory.CreateDirectory(workingDirectory);

        var settingsFilePath = Path.Combine(workingDirectory, "settings.json");
        var legacyFilePath = Path.Combine(workingDirectory, "AutoClicker_Settings.json");
        var store = new JsonAppSettingsStore(settingsFilePath, legacyFilePath);

        var expected = new AppSettings
        {
            Clicker = new ClickSettings(),
            Hotkeys = new HotkeySettings(),
            Screenshot = new ScreenshotSettings(),
            Macro = new MacroSettings(),
            Installer = new InstallerSettings
            {
                SelectedPackageIds = ["Git.Git", "Microsoft.VisualStudioCode"],
                SelectedCleanupPackageIds = ["9P1J8S7CCWWT", "Microsoft.Edge"],
                PackageOptions =
                [
                    new InstallerPackageOptionSelection
                    {
                        PackageId = "Mozilla.Firefox",
                        SelectedOptionIds = ["ublock-origin", "privacy-badger"],
                    },
                ],
            },
        };

        await store.SaveAsync(expected);
        var actual = await store.LoadAsync();

        actual.Installer.SelectedPackageIds.Should().BeEquivalentTo(["Git.Git", "Microsoft.VisualStudioCode"]);
        actual.Installer.SelectedCleanupPackageIds.Should().BeEquivalentTo(["9P1J8S7CCWWT", "Microsoft.Edge"]);
        actual.Installer.PackageOptions.Should().ContainSingle();
        actual.Installer.PackageOptions[0].PackageId.Should().Be("Mozilla.Firefox");
        actual.Installer.PackageOptions[0].SelectedOptionIds.Should().BeEquivalentTo(["ublock-origin", "privacy-badger"]);
    }

    [Fact]
    public async Task SaveAsync_AndLoadAsync_ShouldRoundTripMacroSettings()
    {
        Directory.CreateDirectory(workingDirectory);

        var settingsFilePath = Path.Combine(workingDirectory, "settings.json");
        var legacyFilePath = Path.Combine(workingDirectory, "AutoClicker_Settings.json");
        var store = new JsonAppSettingsStore(settingsFilePath, legacyFilePath);

        var expected = new AppSettings
        {
            Clicker = new ClickSettings(),
            Hotkeys = new HotkeySettings(),
            Screenshot = new ScreenshotSettings(),
            Macro = new MacroSettings
            {
                PlayHotkey = new HotkeyBinding(0x6B, "+"),
                RecordMouseMovement = false,
            },
        };

        await store.SaveAsync(expected);
        var actual = await store.LoadAsync();

        actual.Macro.PlayHotkey.VirtualKey.Should().Be(0x6B);
        actual.Macro.PlayHotkey.DisplayName.Should().Be("+");
        actual.Macro.RecordMouseMovement.Should().BeFalse();
    }

    [Fact]
    public async Task SaveAsync_AndLoadAsync_ShouldRoundTripAssignedMacroHotkeys()
    {
        Directory.CreateDirectory(workingDirectory);

        var settingsFilePath = Path.Combine(workingDirectory, "settings.json");
        var legacyFilePath = Path.Combine(workingDirectory, "AutoClicker_Settings.json");
        var store = new JsonAppSettingsStore(settingsFilePath, legacyFilePath);

        var expected = new AppSettings
        {
            Clicker = new ClickSettings(),
            Hotkeys = new HotkeySettings(),
            Screenshot = new ScreenshotSettings(),
            Macro = new MacroSettings
            {
                PlayHotkey = new HotkeyBinding(0x6B, "+"),
                RecordHotkey = new HotkeyBinding(0x6F, "/"),
                AssignedHotkeys =
                [
                    new MacroHotkeyAssignment
                    {
                        Id = "farm",
                        MacroDisplayName = "Farm Route",
                        MacroFilePath = @"C:\Macros\Farm Route.acmacro.json",
                        Hotkey = new HotkeyBinding(0x74, "F5"),
                        PlaybackMode = MacroHotkeyPlaybackMode.ToggleRepeat,
                        IsEnabled = true,
                    },
                ],
            },
        };

        await store.SaveAsync(expected);
        var actual = await store.LoadAsync();

        actual.Macro.AssignedHotkeys.Should().ContainSingle();
        actual.Macro.AssignedHotkeys[0].Id.Should().Be("farm");
        actual.Macro.AssignedHotkeys[0].MacroDisplayName.Should().Be("Farm Route");
        actual.Macro.AssignedHotkeys[0].MacroFilePath.Should().Be(@"C:\Macros\Farm Route.acmacro.json");
        actual.Macro.AssignedHotkeys[0].Hotkey.VirtualKey.Should().Be(0x74);
        actual.Macro.AssignedHotkeys[0].Hotkey.DisplayName.Should().Be("F5");
        actual.Macro.AssignedHotkeys[0].PlaybackMode.Should().Be(MacroHotkeyPlaybackMode.ToggleRepeat);
        actual.Macro.AssignedHotkeys[0].IsEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task LoadAsync_ShouldDropInvalidAssignedMacroHotkeys()
    {
        Directory.CreateDirectory(workingDirectory);

        var settingsFilePath = Path.Combine(workingDirectory, "settings.json");
        var legacyFilePath = Path.Combine(workingDirectory, "AutoClicker_Settings.json");
        var store = new JsonAppSettingsStore(settingsFilePath, legacyFilePath);

        await File.WriteAllTextAsync(
            settingsFilePath,
            """
            {
              "Macro": {
                "AssignedHotkeys": [
                  {
                    "Id": "bad",
                    "MacroDisplayName": "Broken",
                    "MacroFilePath": "C:\\Macros\\Broken.acmacro.json",
                    "Hotkey": {
                      "VirtualKey": 0,
                      "DisplayName": ""
                    },
                    "PlaybackMode": 0,
                    "IsEnabled": true
                  },
                  {
                    "Id": "good",
                    "MacroDisplayName": "Working",
                    "MacroFilePath": "C:\\Macros\\Working.acmacro.json",
                    "Hotkey": {
                      "VirtualKey": 117,
                      "DisplayName": "F6"
                    },
                    "PlaybackMode": 1,
                    "IsEnabled": false
                  }
                ]
              }
            }
            """);

        var actual = await store.LoadAsync();

        actual.Macro.AssignedHotkeys.Should().ContainSingle();
        actual.Macro.AssignedHotkeys[0].Id.Should().Be("good");
        actual.Macro.AssignedHotkeys[0].Hotkey.VirtualKey.Should().Be(117);
    }

    [Fact]
    public async Task LoadAsync_ShouldDefaultScreenshotFolderToDownloadsScreenshots()
    {
        Directory.CreateDirectory(workingDirectory);

        var settingsFilePath = Path.Combine(workingDirectory, "settings.json");
        var legacyFilePath = Path.Combine(workingDirectory, "AutoClicker_Settings.json");
        var store = new JsonAppSettingsStore(settingsFilePath, legacyFilePath);

        var actual = await store.LoadAsync();

        actual.Screenshot.SaveFolderPath.Should().Be(ScreenshotSettings.GetDefaultSaveFolderPath());
    }

    [Fact]
    public async Task LoadAsync_ShouldMigrateLegacyDownloadsScreenshotFolder()
    {
        Directory.CreateDirectory(workingDirectory);

        var settingsFilePath = Path.Combine(workingDirectory, "settings.json");
        var legacyFilePath = Path.Combine(workingDirectory, "AutoClicker_Settings.json");
        var store = new JsonAppSettingsStore(settingsFilePath, legacyFilePath);

        var expected = new AppSettings
        {
            Clicker = new ClickSettings(),
            Hotkeys = new HotkeySettings(),
            Screenshot = new ScreenshotSettings
            {
                SaveFolderPath = ScreenshotSettings.GetLegacyDefaultSaveFolderPath(),
            },
            Macro = new MacroSettings(),
        };

        await store.SaveAsync(expected);
        var actual = await store.LoadAsync();

        actual.Screenshot.SaveFolderPath.Should().Be(ScreenshotSettings.GetDefaultSaveFolderPath());
    }

    public void Dispose()
    {
        if (Directory.Exists(workingDirectory))
        {
            Directory.Delete(workingDirectory, true);
        }
    }
}
