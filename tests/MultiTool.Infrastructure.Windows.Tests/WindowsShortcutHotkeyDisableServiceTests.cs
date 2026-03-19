using MultiTool.Core.Models;
using MultiTool.Infrastructure.Windows.Tools;
using FluentAssertions;

namespace MultiTool.Infrastructure.Windows.Tests;

public sealed class WindowsShortcutHotkeyDisableServiceTests : IDisposable
{
    private readonly string workingDirectory = Path.Combine(Path.GetTempPath(), "MultiTool.Tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task DisableAsync_ShouldDisableSupportedShortcutFilesAndSkipUnsupportedEntries()
    {
        var clearedPaths = new List<string>();
        var supportedShortcutPath = CreateFile(Path.Combine(workingDirectory, "Editor.lnk"));
        var duplicateSupportedShortcut = new ShortcutHotkeyInfo(
            "Ctrl + Alt + E",
            "Editor duplicate",
            supportedShortcutPath,
            workingDirectory,
            @"C:\Apps\Editor.exe",
            TargetExists: false,
            "Detected shortcut file",
            "Windows .lnk hotkey",
            "Target: C:\\Apps\\Editor.exe",
            CanDisable: true);
        var service = new WindowsShortcutHotkeyDisableService(path =>
        {
            clearedPaths.Add(path);
            return true;
        });

        var result = await service.DisableAsync(
            [
                new ShortcutHotkeyInfo(
                    "Ctrl + Alt + E",
                    "Editor",
                    supportedShortcutPath,
                    workingDirectory,
                    @"C:\Apps\Editor.exe",
                    TargetExists: false,
                    "Detected shortcut file",
                    "Windows .lnk hotkey",
                    "Target: C:\\Apps\\Editor.exe",
                    CanDisable: true),
                duplicateSupportedShortcut,
                new ShortcutHotkeyInfo(
                    "Ctrl + Shift + P",
                    "Command palette",
                    Path.Combine(workingDirectory, "keybindings.json"),
                    workingDirectory,
                    string.Empty,
                    TargetExists: false,
                    "Detected app keymap",
                    "Visual Studio Code",
                    "Detected from keybindings.json"),
            ]);

        result.DisabledCount.Should().Be(1);
        result.SupportedCount.Should().Be(1);
        result.UnsupportedCount.Should().Be(1);
        result.Warnings.Should().BeEmpty();
        clearedPaths.Should().ContainSingle().Which.Should().Be(supportedShortcutPath);
    }

    [Fact]
    public async Task DisableAsync_ShouldReportWarningsWhenShortcutFileIsMissing()
    {
        var missingShortcutPath = Path.Combine(workingDirectory, "Missing.lnk");
        var service = new WindowsShortcutHotkeyDisableService(_ => true);

        var result = await service.DisableAsync(
            [
                new ShortcutHotkeyInfo(
                    "Ctrl + Alt + M",
                    "Missing",
                    missingShortcutPath,
                    workingDirectory,
                    string.Empty,
                    TargetExists: false,
                    "Detected shortcut file",
                    "Windows .lnk hotkey",
                    "Assigned on a Windows shortcut file.",
                    CanDisable: true),
            ]);

        result.DisabledCount.Should().Be(0);
        result.SupportedCount.Should().Be(1);
        result.UnsupportedCount.Should().Be(0);
        result.Warnings.Should().ContainSingle();
        result.Warnings[0].Should().Contain("no longer exists");
    }

    public void Dispose()
    {
        if (Directory.Exists(workingDirectory))
        {
            Directory.Delete(workingDirectory, true);
        }
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
}
