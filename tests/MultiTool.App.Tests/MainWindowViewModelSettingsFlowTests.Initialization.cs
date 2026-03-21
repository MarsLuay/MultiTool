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
    [Fact]
    public void MainWindowXaml_ShouldUseOneWayBindingForRunTextBindings()
    {
        var viewsDirectory = Path.GetFullPath(
            Path.Combine(
                AppContext.BaseDirectory,
                "..",
                "..",
                "..",
                "..",
                "..",
                "src",
                "MultiTool.App",
                "Views"));

        Directory.Exists(viewsDirectory).Should().BeTrue();

        var xamlPaths = Directory.GetFiles(viewsDirectory, "*.xaml", SearchOption.TopDirectoryOnly)
            .Where(path => Path.GetFileName(path) is "MainWindow.xaml" or "ClickerTabView.xaml" or "ScreenshotTabView.xaml" or "MacroTabView.xaml" or "InstallerTabView.xaml" or "ToolsTabView.xaml" or "SettingsTabView.xaml")
            .ToArray();

        xamlPaths.Should().NotBeEmpty();

        var matches = xamlPaths
            .SelectMany(
                path => Regex.Matches(File.ReadAllText(path), "<Run\\s+Text=\"\\{Binding[^\\\"]*\\}\"")
                    .Cast<Match>())
            .ToArray();

        matches.Should().OnlyContain(match => match.Value.Contains("Mode=OneWay", StringComparison.Ordinal));
    }

    [Fact]
    public async Task InitializeAsync_ShouldApplyLoadedUiSettings()
    {
        await StaDispatcherTestRunner.RunAsync(
            async () =>
            {
                var settings = DefaultSettingsFactory.Create();
                settings.Ui.IsDarkMode = true;
                settings.Ui.EnableCtrlWheelResize = false;
                settings.Ui.AutoHideOnStartup = true;
                settings.Hotkeys.OverrideApplicationShortcuts = true;
                settings.Screenshot.CaptureHotkey = new HotkeyBinding(0x43, "Ctrl + C", HotkeyInputKind.Keyboard, ClickMouseButton.Left, HotkeyModifiers.Control);

                var context = new MainWindowViewModelTestContext(settings);

                await context.ViewModel.InitializeAsync();

                context.ViewModel.IsDarkMode.Should().BeTrue();
                context.ViewModel.IsCtrlWheelResizeEnabled.Should().BeFalse();
                context.ViewModel.IsAutoHideOnStartupEnabled.Should().BeTrue();
                context.ViewModel.IsShortcutOverrideEnabled.Should().BeTrue();
                context.ViewModel.ScreenshotHotkeyDisplay.Should().Be("Ctrl + C");
                context.ViewModel.ScreenshotHotkeyModifiers.Should().Be(HotkeyModifiers.Control);
                context.ThemeService.AppliedModes.Should().ContainSingle().Which.Should().BeTrue();
            });
    }

    [Fact]
    public async Task InitializeAsync_WhenRunAtStartupSettingIsMissing_ShouldMigrateCurrentSystemState()
    {
        await StaDispatcherTestRunner.RunAsync(
            async () =>
            {
                var settings = DefaultSettingsFactory.Create();
                settings.Ui.RunAtStartup = null;

                var context = new MainWindowViewModelTestContext(settings);
                context.RunAtStartupService.CurrentState = true;

                await context.ViewModel.InitializeAsync();
                await context.SettingsStore.WaitForSaveCountAsync(expectedCount: 1);

                context.ViewModel.IsRunAtStartupEnabled.Should().BeTrue();
                context.RunAtStartupService.SetEnabledCalls.Should().Equal(true);
                context.SettingsStore.LastSavedSettings.Should().NotBeNull();
                context.SettingsStore.LastSavedSettings!.Ui.RunAtStartup.Should().BeTrue();
            });
    }

    [Fact]
    public async Task ChangingUiSettingsAfterInitialization_ShouldAutoSaveUpdatedValues()
    {
        await StaDispatcherTestRunner.RunAsync(
            async () =>
            {
                var settings = DefaultSettingsFactory.Create();
                settings.Ui.RunAtStartup = null;

                var context = new MainWindowViewModelTestContext(settings);

                await context.ViewModel.InitializeAsync();

                context.ViewModel.IsCtrlWheelResizeEnabled = false;
                context.ViewModel.IsAutoHideOnStartupEnabled = true;
                context.ViewModel.IsRunAtStartupEnabled = true;
                context.ViewModel.IsShortcutOverrideEnabled = true;

                await context.SettingsStore.WaitForSaveCountAsync(expectedCount: 1);

                context.RunAtStartupService.SetEnabledCalls.Should().Equal(true);
                context.SettingsStore.LastSavedSettings.Should().NotBeNull();
                context.SettingsStore.LastSavedSettings!.Ui.EnableCtrlWheelResize.Should().BeFalse();
                context.SettingsStore.LastSavedSettings!.Ui.AutoHideOnStartup.Should().BeTrue();
                context.SettingsStore.LastSavedSettings!.Ui.RunAtStartup.Should().BeTrue();
                context.SettingsStore.LastSavedSettings!.Hotkeys.OverrideApplicationShortcuts.Should().BeTrue();
            });
    }
}
