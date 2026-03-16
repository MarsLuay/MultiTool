using AutoClicker.Infrastructure.Windows.Tools;
using FluentAssertions;

namespace AutoClicker.Infrastructure.Windows.Tests;

public sealed class WindowsShortcutHotkeyInventoryServiceTests : IDisposable
{
    private readonly string workingDirectory = Path.Combine(Path.GetTempPath(), "AutoClicker.Tests", Guid.NewGuid().ToString("N"));

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
        var updates = new List<AutoClicker.Core.Models.ShortcutHotkeyScanProgress>();

        var service = new WindowsShortcutHotkeyInventoryService(
            () => [rootPath],
            shortcutPath => shortcutPath switch
            {
                var path when string.Equals(path, firstShortcutPath, StringComparison.OrdinalIgnoreCase) => ("CTRL+ALT+E", @"C:\Apps\Editor.exe"),
                var path when string.Equals(path, secondShortcutPath, StringComparison.OrdinalIgnoreCase) => ("CTRL+ALT+B", @"C:\Apps\Browser.exe"),
                var path when string.Equals(path, thirdShortcutPath, StringComparison.OrdinalIgnoreCase) => ("CTRL+ALT+N", @"C:\Apps\Notes.exe"),
                _ => throw new InvalidOperationException($"Unexpected shortcut path: {shortcutPath}"),
            });

        var result = await service.ScanAsync(new DelegateProgress<AutoClicker.Core.Models.ShortcutHotkeyScanProgress>(updates.Add));

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
        var updates = new List<AutoClicker.Core.Models.ShortcutHotkeyScanProgress>();

        var service = new WindowsShortcutHotkeyInventoryService(
            () => [rootPath],
            shortcutPath => shortcutPath switch
            {
                var path when string.Equals(path, firstShortcutPath, StringComparison.OrdinalIgnoreCase) => ("CTRL+ALT+E", @"C:\Apps\Editor.exe"),
                var path when string.Equals(path, secondShortcutPath, StringComparison.OrdinalIgnoreCase) => ("CTRL+ALT+B", @"C:\Apps\Browser.exe"),
                _ => throw new InvalidOperationException($"Unexpected shortcut path: {shortcutPath}"),
            },
            _ => 2);

        var result = await service.ScanAsync(new DelegateProgress<AutoClicker.Core.Models.ShortcutHotkeyScanProgress>(updates.Add));

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
                new AutoClicker.Core.Models.ShortcutHotkeyInfo(
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
