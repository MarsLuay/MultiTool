using MultiTool.Infrastructure.Windows.Tools;
using FluentAssertions;

namespace MultiTool.Infrastructure.Windows.Tests;

public sealed class WindowsShortcutHotkeyInventoryServiceTests : IDisposable
{
    private readonly string workingDirectory = Path.Combine(Path.GetTempPath(), "MultiTool.Tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task ScanAsync_ShouldReturnOnlyShortcutFilesWithAssignedHotkeys()
    {
        var rootPath = CreateDirectory("Shortcuts");
        var assignedShortcutPath = CreateFile(Path.Combine(rootPath, "Editor.lnk"));
        var noHotkeyShortcutPath = CreateFile(Path.Combine(rootPath, "NoHotkey.lnk"));

        var service = new WindowsShortcutHotkeyInventoryService(
            () => [rootPath],
            shortcutPath => shortcutPath switch
            {
                var path when string.Equals(path, assignedShortcutPath, StringComparison.OrdinalIgnoreCase) => ("CTRL+ALT+E", @"C:\Apps\Editor.exe"),
                var path when string.Equals(path, noHotkeyShortcutPath, StringComparison.OrdinalIgnoreCase) => ((string Hotkey, string TargetPath)?)null,
                _ => throw new InvalidOperationException($"Unexpected shortcut path: {shortcutPath}"),
            });

        var result = await service.ScanAsync();

        result.ScannedShortcutCount.Should().Be(2);
        result.Warnings.Should().BeEmpty();
        result.Shortcuts.Should().ContainSingle();
        result.Shortcuts[0].Hotkey.Should().Be("Ctrl + Alt + E");
        result.Shortcuts[0].ShortcutName.Should().Be("Editor");
        result.Shortcuts[0].ShortcutPath.Should().Be(assignedShortcutPath);
        result.Shortcuts[0].FolderPath.Should().Be(rootPath);
        result.Shortcuts[0].TargetPath.Should().Be(@"C:\Apps\Editor.exe");
        result.Shortcuts[0].TargetExists.Should().BeFalse();
    }

    [Fact]
    public async Task ScanAsync_ShouldCollectWarningsWhenShortcutMetadataFails()
    {
        var rootPath = CreateDirectory("BrokenShortcuts");
        var brokenShortcutPath = CreateFile(Path.Combine(rootPath, "Broken.lnk"));
        var workingShortcutPath = CreateFile(Path.Combine(rootPath, "Working.lnk"));

        var service = new WindowsShortcutHotkeyInventoryService(
            () => [rootPath],
            shortcutPath =>
            {
                if (string.Equals(shortcutPath, brokenShortcutPath, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Metadata read failed.");
                }

                if (string.Equals(shortcutPath, workingShortcutPath, StringComparison.OrdinalIgnoreCase))
                {
                    return ("SHIFT+F12", workingShortcutPath);
                }

                throw new InvalidOperationException($"Unexpected shortcut path: {shortcutPath}");
            });

        var result = await service.ScanAsync();

        result.ScannedShortcutCount.Should().Be(2);
        result.Shortcuts.Should().ContainSingle();
        result.Shortcuts[0].Hotkey.Should().Be("Shift + F12");
        result.Shortcuts[0].TargetExists.Should().BeTrue();
        result.Warnings.Should().ContainSingle();
        result.Warnings[0].Should().Contain("Metadata read failed.");
    }

    [Fact]
    public async Task ScanAsync_ShouldFlagConflictingShortcutHotkeys()
    {
        var rootPath = CreateDirectory("ConflictingShortcuts");
        var editorShortcutPath = CreateFile(Path.Combine(rootPath, "Editor.lnk"));
        var browserShortcutPath = CreateFile(Path.Combine(rootPath, "Browser.lnk"));
        var notesShortcutPath = CreateFile(Path.Combine(rootPath, "Notes.lnk"));

        var service = new WindowsShortcutHotkeyInventoryService(
            () => [rootPath],
            shortcutPath => shortcutPath switch
            {
                var path when string.Equals(path, editorShortcutPath, StringComparison.OrdinalIgnoreCase) => ("CTRL+ALT+E", @"C:\Apps\Editor.exe"),
                var path when string.Equals(path, browserShortcutPath, StringComparison.OrdinalIgnoreCase) => ("CTRL+ALT+E", @"C:\Apps\Browser.exe"),
                var path when string.Equals(path, notesShortcutPath, StringComparison.OrdinalIgnoreCase) => ("CTRL+SHIFT+N", @"C:\Apps\Notes.exe"),
                _ => throw new InvalidOperationException($"Unexpected shortcut path: {shortcutPath}"),
            });

        var result = await service.ScanAsync();

        result.ConflictGroupCount.Should().Be(1);
        result.ConflictingShortcutCount.Should().Be(2);
        result.Shortcuts.Should().HaveCount(3);
        result.Shortcuts.Count(shortcut => shortcut.HasConflict).Should().Be(2);
        result.Shortcuts.Single(shortcut => shortcut.ShortcutName == "Editor").ConflictSummary.Should().Contain("Browser");
        result.Shortcuts.Single(shortcut => shortcut.ShortcutName == "Browser").ConflictSummary.Should().Contain("Editor");
        result.Shortcuts.Single(shortcut => shortcut.ShortcutName == "Notes").HasConflict.Should().BeFalse();
    }

    [Fact]
    public async Task ScanAsync_ShouldReportProgressWhileTraversingShortcutFolders()
    {
        var rootPath = CreateDirectory("ProgressShortcuts");
        var appsPath = CreateDirectory(Path.Combine("ProgressShortcuts", "Apps"));
        var toolsPath = CreateDirectory(Path.Combine("ProgressShortcuts", "Apps", "Tools"));
        var firstShortcutPath = CreateFile(Path.Combine(rootPath, "Editor.lnk"));
        var secondShortcutPath = CreateFile(Path.Combine(appsPath, "Browser.lnk"));
        var thirdShortcutPath = CreateFile(Path.Combine(toolsPath, "Notes.lnk"));
        var updates = new List<MultiTool.Core.Models.ShortcutHotkeyScanProgress>();

        var service = new WindowsShortcutHotkeyInventoryService(
            () => [rootPath],
            shortcutPath => shortcutPath switch
            {
                var path when string.Equals(path, firstShortcutPath, StringComparison.OrdinalIgnoreCase) => ("CTRL+ALT+E", @"C:\Apps\Editor.exe"),
                var path when string.Equals(path, secondShortcutPath, StringComparison.OrdinalIgnoreCase) => ("CTRL+ALT+B", @"C:\Apps\Browser.exe"),
                var path when string.Equals(path, thirdShortcutPath, StringComparison.OrdinalIgnoreCase) => ("CTRL+ALT+N", @"C:\Apps\Notes.exe"),
                _ => throw new InvalidOperationException($"Unexpected shortcut path: {shortcutPath}"),
            });

        var result = await service.ScanAsync(new DelegateProgress<MultiTool.Core.Models.ShortcutHotkeyScanProgress>(updates.Add));

        result.ScannedShortcutCount.Should().Be(3);
        updates.Should().NotBeEmpty();
        updates.Should().Contain(update => update.ScannedShortcutCount >= 1);
        updates.Should().Contain(update => update.TotalFolderCount >= 3);
        updates.Should().Contain(update => string.Equals(update.CurrentPath, rootPath, StringComparison.OrdinalIgnoreCase));
        updates[^1].CompletedFolderCount.Should().Be(updates[^1].TotalFolderCount);
    }

    [Fact]
    public async Task ScanAsync_ShouldUseExactFolderCountWhenFastWindowsProviderIsAvailable()
    {
        var rootPath = CreateDirectory("FastCountShortcuts");
        var childPath = CreateDirectory(Path.Combine("FastCountShortcuts", "Apps"));
        var firstShortcutPath = CreateFile(Path.Combine(rootPath, "Editor.lnk"));
        var secondShortcutPath = CreateFile(Path.Combine(childPath, "Browser.lnk"));
        var updates = new List<MultiTool.Core.Models.ShortcutHotkeyScanProgress>();

        var service = new WindowsShortcutHotkeyInventoryService(
            () => [rootPath],
            shortcutPath => shortcutPath switch
            {
                var path when string.Equals(path, firstShortcutPath, StringComparison.OrdinalIgnoreCase) => ("CTRL+ALT+E", @"C:\Apps\Editor.exe"),
                var path when string.Equals(path, secondShortcutPath, StringComparison.OrdinalIgnoreCase) => ("CTRL+ALT+B", @"C:\Apps\Browser.exe"),
                _ => throw new InvalidOperationException($"Unexpected shortcut path: {shortcutPath}"),
            },
            _ => 2);

        var result = await service.ScanAsync(new DelegateProgress<MultiTool.Core.Models.ShortcutHotkeyScanProgress>(updates.Add));

        result.ScannedShortcutCount.Should().Be(2);
        updates.Should().NotBeEmpty();
        updates[0].TotalFolderCount.Should().Be(2);
        updates.Should().OnlyContain(update => update.TotalFolderCount == 2);
    }

    [Fact]
    public async Task ScanAsync_ShouldIncludeReferenceShortcutsWhenConfigured()
    {
        var rootPath = CreateDirectory("ReferenceShortcuts");
        var assignedShortcutPath = CreateFile(Path.Combine(rootPath, "Editor.lnk"));

        var service = new WindowsShortcutHotkeyInventoryService(
            () => [rootPath],
            shortcutPath => shortcutPath switch
            {
                var path when string.Equals(path, assignedShortcutPath, StringComparison.OrdinalIgnoreCase) => ("CTRL+ALT+E", @"C:\Apps\Editor.exe"),
                _ => throw new InvalidOperationException($"Unexpected shortcut path: {shortcutPath}"),
            },
            _ => null,
            () =>
            [
                new MultiTool.Core.Models.ShortcutHotkeyInfo(
                    "Ctrl + C",
                    "Copy",
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    TargetExists: false,
                    "Windows / common shortcut",
                    "Most apps and File Explorer",
                    "Copies the selected text, file, or item.",
                    IsReferenceShortcut: true),
            ]);

        var result = await service.ScanAsync();

        result.ScannedShortcutCount.Should().Be(1);
        result.Shortcuts.Should().Contain(shortcut => shortcut.ShortcutName == "Editor" && !shortcut.IsReferenceShortcut);
        result.Shortcuts.Should().Contain(shortcut => shortcut.ShortcutName == "Copy" && shortcut.IsReferenceShortcut);
    }

    [Fact]
    public void ResolveVsCodeLikeKeybindingFiles_ShouldFindCompatibleFilesAndSkipExtensionDefaults()
    {
        var rootPath = CreateDirectory("CompatibleKeymaps");
        var compatibleFilePath = CreateFile(Path.Combine(rootPath, "SuperEditor", "User", "keybindings.json"));
        var alternateCompatibleFilePath = CreateFile(Path.Combine(rootPath, "Writer", "Config", "keyboard-shortcuts.json"));
        var extensionFilePath = CreateFile(Path.Combine(rootPath, "Code", "extensions", "theme-pack", "keybindings.json"));

        var files = WindowsShortcutHotkeyInventoryService.ResolveVsCodeLikeKeybindingFiles([rootPath]).ToArray();

        files.Should().BeEquivalentTo([compatibleFilePath, alternateCompatibleFilePath]);
        files.Should().NotContain(extensionFilePath);
    }

    [Fact]
    public void ReadVsCodeLikeShortcuts_ShouldInferAppNameFromCompatibleKeybindingFiles()
    {
        var settingsPath = CreateFile(Path.Combine(workingDirectory, "SuperEditor", "User", "keybindings.json"));
        File.WriteAllText(
            settingsPath,
            """
            [
              {
                "key": "ctrl+alt+p",
                "command": "super.run",
                "when": "editorTextFocus"
              }
            ]
            """);

        var shortcuts = WindowsShortcutHotkeyInventoryService.ReadVsCodeLikeShortcuts([settingsPath]);

        shortcuts.Should().ContainSingle(shortcut =>
            shortcut.Hotkey == "Ctrl + Alt + P"
            && shortcut.ShortcutName == "super.run"
            && shortcut.AppliesTo == "SuperEditor");
        shortcuts[0].Details.Should().Contain("editorTextFocus");
    }

    [Fact]
    public void ReadWindowsTerminalShortcuts_ShouldParseSettingsJsonWithCommentsAndMultipleKeys()
    {
        var settingsPath = CreateFile(Path.Combine(workingDirectory, "terminal", "settings.json"));
        File.WriteAllText(
            settingsPath,
            """
            {
              // user comments are valid in Windows Terminal settings
              "actions": [
                {
                  "command": "newTab",
                  "keys": "ctrl+shift+t",
                },
                {
                  "command": {
                    "action": "splitPane",
                    "split": "vertical"
                  },
                  "keys": [
                    "alt+shift+-",
                    "ctrl+alt+v"
                  ]
                }
              ],
            }
            """);

        var shortcuts = WindowsShortcutHotkeyInventoryService.ReadWindowsTerminalShortcuts([settingsPath]);

        shortcuts.Should().HaveCount(3);
        shortcuts.Should().ContainSingle(shortcut => shortcut.Hotkey == "Ctrl + Shift + T" && shortcut.ShortcutName == "newTab");
        shortcuts.Should().ContainSingle(shortcut => shortcut.Hotkey == "Alt + Shift + -" && shortcut.ShortcutName == "splitPane");
        shortcuts.Should().ContainSingle(shortcut => shortcut.Hotkey == "Ctrl + Alt + V" && shortcut.ShortcutName == "splitPane");
        shortcuts.Should().OnlyContain(shortcut => shortcut.AppliesTo == "Windows Terminal");
        shortcuts.Single(shortcut => shortcut.Hotkey == "Alt + Shift + -").Details.Should().Contain("split=vertical");
    }

    [Fact]
    public void ReadHotkeysJsonShortcuts_ShouldInferAppNameFromCompatibleHotkeyFiles()
    {
        var hotkeysPath = CreateFile(Path.Combine(workingDirectory, "NoteLab", "hotkeys.json"));
        File.WriteAllText(
            hotkeysPath,
            """
            {
              "open-command-palette": [
                {
                  "modifiers": ["Ctrl", "Alt"],
                  "key": "p"
                }
              ]
            }
            """);

        var shortcuts = WindowsShortcutHotkeyInventoryService.ReadHotkeysJsonShortcuts([hotkeysPath]);

        shortcuts.Should().ContainSingle(shortcut =>
            shortcut.Hotkey == "Ctrl + Alt + P"
            && shortcut.ShortcutName == "open-command-palette"
            && shortcut.AppliesTo == "NoteLab");
    }

    [Fact]
    public void ReadFlowLauncherShortcuts_ShouldParseConfiguredAndCustomHotkeys()
    {
        var settingsPath = CreateFile(Path.Combine(workingDirectory, "FlowLauncher", "Settings", "Settings.json"));
        File.WriteAllText(
            settingsPath,
            """
            {
              "Hotkey": "Alt + Space",
              "PreviewHotkey": "F1",
              "AutoCompleteHotkey": "Ctrl + Tab",
              "SettingWindowHotkey": "Ctrl+I",
              "OpenHistoryHotkey": "Ctrl+H",
              "OpenResultModifiers": "Alt",
              "ShowOpenResultHotkey": true,
              "DialogJumpHotkey": "Alt + G",
              "CustomPluginHotkeys": [
                {
                  "Hotkey": "Alt + C",
                  "ActionKeyword": "calc"
                },
                {
                  "Hotkey": "Ctrl+Shift+P",
                  "ActionKeyword": "plugins"
                }
              ]
            }
            """);

        var shortcuts = WindowsShortcutHotkeyInventoryService.ReadFlowLauncherShortcuts([settingsPath]);

        shortcuts.Should().ContainSingle(shortcut =>
            shortcut.Hotkey == "Alt + Space"
            && shortcut.ShortcutName == "Open Flow Launcher");
        shortcuts.Should().ContainSingle(shortcut =>
            shortcut.Hotkey == "Ctrl + I"
            && shortcut.ShortcutName == "Open settings");
        shortcuts.Should().ContainSingle(shortcut =>
            shortcut.Hotkey == "Alt + 1"
            && shortcut.ShortcutName == "Open result 1");
        shortcuts.Should().ContainSingle(shortcut =>
            shortcut.Hotkey == "Alt + 0"
            && shortcut.ShortcutName == "Open result 10");
        shortcuts.Should().ContainSingle(shortcut =>
            shortcut.Hotkey == "Alt + C"
            && shortcut.ShortcutName == "Query \"calc\""
            && shortcut.Details.Contains("Action keyword: calc", StringComparison.Ordinal));
        shortcuts.Should().ContainSingle(shortcut =>
            shortcut.Hotkey == "Ctrl + Shift + P"
            && shortcut.ShortcutName == "Query \"plugins\"");
        shortcuts.Should().OnlyContain(shortcut =>
            shortcut.AppliesTo == "Flow Launcher"
            && shortcut.SourceLabel == "Detected app settings");
    }

    [Fact]
    public void ResolveJetBrainsKeymapFiles_AndReadJetBrainsShortcuts_ShouldHandlePortableKeymaps()
    {
        var rootPath = CreateDirectory("PortableJetBrains");
        var keymapPath = CreateFile(Path.Combine(rootPath, "RiderPortable", "config", "keymaps", "custom.xml"));
        var unrelatedPath = CreateFile(Path.Combine(rootPath, "Themes", "keymaps.xml"));
        File.WriteAllText(
            keymapPath,
            """
            <keymap version="1">
              <action id="EditorDuplicate">
                <keyboard-shortcut first-keystroke="ctrl D" />
              </action>
            </keymap>
            """);

        var discoveredFiles = WindowsShortcutHotkeyInventoryService.ResolveJetBrainsKeymapFiles([rootPath]).ToArray();
        var shortcuts = WindowsShortcutHotkeyInventoryService.ReadJetBrainsShortcuts(discoveredFiles);

        discoveredFiles.Should().Contain(keymapPath);
        discoveredFiles.Should().NotContain(unrelatedPath);
        shortcuts.Should().ContainSingle(shortcut =>
            shortcut.Hotkey == "Ctrl + D"
            && shortcut.ShortcutName == "EditorDuplicate"
            && shortcut.AppliesTo == "RiderPortable");
    }

    [Fact]
    public void ReadAutoHotkeyScriptShortcuts_ShouldParseContextsAndSkipHotstrings()
    {
        var scriptPath = CreateFile(Path.Combine(workingDirectory, "scripts", "bindings.ahk"));
        File.WriteAllText(
            scriptPath,
            """
            #HotIf WinActive("ahk_exe Code.exe")
            ^!c::Run "calc.exe"
            CapsLock & j::
            :*:btw::by the way
            #HotIf
            ~#f Up::MsgBox "done"
            """);

        var shortcuts = WindowsShortcutHotkeyInventoryService.ReadAutoHotkeyScriptShortcuts([scriptPath]);

        shortcuts.Should().HaveCount(3);
        shortcuts.Should().ContainSingle(shortcut =>
            shortcut.Hotkey == "Ctrl + Alt + C"
            && shortcut.ShortcutName == "Run \"calc.exe\""
            && shortcut.AppliesTo == "Code.exe");
        shortcuts.Should().ContainSingle(shortcut =>
            shortcut.Hotkey == "CapsLock & J"
            && shortcut.ShortcutName == "Script hotkey (line 3)"
            && shortcut.AppliesTo == "Code.exe");
        shortcuts.Should().ContainSingle(shortcut =>
            shortcut.Hotkey == "Win + F (Up)"
            && shortcut.AppliesTo == "AutoHotkey script");
        shortcuts.Should().OnlyContain(shortcut => shortcut.SourceLabel == "Detected AutoHotkey script");
        shortcuts.First(shortcut => shortcut.Hotkey == "Ctrl + Alt + C").Details.Should().Contain("Condition: WinActive(\"ahk_exe Code.exe\")");
    }

    public void Dispose()
    {
        if (Directory.Exists(workingDirectory))
        {
            Directory.Delete(workingDirectory, true);
        }
    }

    private string CreateDirectory(string relativePath)
    {
        var path = Path.Combine(workingDirectory, relativePath);
        Directory.CreateDirectory(path);
        return path;
    }

    private static string CreateFile(string path)
    {
        var parentDirectory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(parentDirectory))
        {
            Directory.CreateDirectory(parentDirectory);
        }

        File.WriteAllText(path, string.Empty);
        return path;
    }

    private sealed class DelegateProgress<T>(Action<T> report) : IProgress<T>
    {
        public void Report(T value)
        {
            report(value);
        }
    }
}
