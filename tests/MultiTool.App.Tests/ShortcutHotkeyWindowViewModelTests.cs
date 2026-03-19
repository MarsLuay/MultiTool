using FluentAssertions;
using MultiTool.App.Models;
using MultiTool.App.ViewModels;
using MultiTool.Core.Models;

namespace MultiTool.App.Tests;

public sealed class ShortcutHotkeyWindowViewModelTests
{
    [Fact]
    public async Task ApplyShortcutChangesCommand_ShouldDisableRowsUncheckedInEnabledColumn()
    {
        await StaDispatcherTestRunner.RunAsync(
            async () =>
            {
                var supportedShortcut = new ShortcutHotkeyInfo(
                    "Ctrl + Alt + E",
                    "Editor",
                    @"C:\Shortcuts\Editor.lnk",
                    @"C:\Shortcuts",
                    @"C:\Apps\Editor.exe",
                    TargetExists: true,
                    CanDisable: true);
                var unsupportedShortcut = new ShortcutHotkeyInfo(
                    "Ctrl + Shift + P",
                    "Open Command Palette",
                    @"C:\Users\Test\AppData\Roaming\SuperEditor\User\keybindings.json",
                    @"C:\Users\Test\AppData\Roaming\SuperEditor\User",
                    string.Empty,
                    TargetExists: false,
                    SourceLabel: "Detected app keymap",
                    AppliesTo: "SuperEditor");
                var initialResult = new ShortcutHotkeyScanResult([supportedShortcut, unsupportedShortcut], 1, []);
                var refreshedResult = new ShortcutHotkeyScanResult([unsupportedShortcut], 1, []);
                IReadOnlyList<ShortcutHotkeyInfo>? disabledShortcuts = null;

                var viewModel = new ShortcutHotkeyWindowViewModel(
                    initialResult,
                    isCachedResult: false,
                    rescanAsync: () => Task.FromResult(initialResult),
                    disableAsync: shortcuts =>
                    {
                        disabledShortcuts = shortcuts;
                        return Task.FromResult(
                            new ShortcutHotkeyDisableOperationResult(
                                refreshedResult,
                                new ShortcutHotkeyDisableResult(1, 1, 0, [])));
                    });

                var rows = viewModel.ShortcutsView.Cast<ShortcutHotkeyItemViewModel>().ToArray();
                rows.Should().HaveCount(2);
                rows.Should().ContainSingle(row => row.ShortcutName == "Editor" && row.CanEditShortcutEnabledState);
                rows.Should().ContainSingle(row => row.ShortcutName == "Open Command Palette" && !row.CanEditShortcutEnabledState);

                viewModel.ApplyShortcutChangesCommand.CanExecute(null).Should().BeFalse();

                rows.Single(row => row.ShortcutName == "Editor").IsShortcutEnabled = false;

                viewModel.ApplyShortcutChangesCommand.CanExecute(null).Should().BeTrue();

                await viewModel.ApplyShortcutChangesCommand.ExecuteAsync(null);

                disabledShortcuts.Should().NotBeNull();
                disabledShortcuts.Should().ContainSingle(shortcut => shortcut.ShortcutName == "Editor");
                viewModel.ShortcutsView.Cast<ShortcutHotkeyItemViewModel>()
                    .Should()
                    .ContainSingle(row => row.ShortcutName == "Open Command Palette");
            });
    }
}
