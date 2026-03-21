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
    public async Task ChangingPinStateAfterInitialization_ShouldRefreshPinWindowToolPresentation()
    {
        await StaDispatcherTestRunner.RunAsync(
            async () =>
            {
                var settings = DefaultSettingsFactory.Create();
                settings.Hotkeys.PinWindow = new HotkeyBinding(0x78, "F9");

                var context = new MainWindowViewModelTestContext(settings);

                await context.ViewModel.InitializeAsync();

                context.ViewModel.IsTopMost = true;

                context.ViewModel.PinWindowStateText.Should().Be("pinned on top");
                context.ViewModel.PinWindowActionButtonText.Should().Be("Unpin Window");
                context.ViewModel.PinWindowToolStatusMessage.Should().Contain("pinned");
                context.ViewModel.PinWindowToolStatusMessage.Should().Contain("F9");
            });
    }

    [Fact]
    public async Task CapturePinWindowHotkey_ShouldUpdateHotkeyAndSave()
    {
        await StaDispatcherTestRunner.RunAsync(
            async () =>
            {
                var settings = DefaultSettingsFactory.Create();

                var context = new MainWindowViewModelTestContext(settings);

                await context.ViewModel.InitializeAsync();

                context.ViewModel.CapturePinWindowHotkey(Key.F10, ModifierKeys.None);
                await context.SettingsStore.WaitForSaveCountAsync(expectedCount: 1);

                context.ViewModel.PinWindowHotkeyLabel.Should().Be("F10");
                context.ViewModel.PinWindowHotkeySummary.Should().Contain("F10");
                context.ViewModel.PinWindowToolStatusMessage.Should().Contain("F10");
                context.SettingsStore.LastSavedSettings.Should().NotBeNull();
                context.SettingsStore.LastSavedSettings!.Hotkeys.PinWindow.VirtualKey.Should().Be(0x79);
                context.SettingsStore.LastSavedSettings!.Hotkeys.PinWindow.DisplayName.Should().Be("F10");
            });
    }

    [Fact]
    public async Task CapturePinWindowHotkey_ShouldStoreModifierComboAndSave()
    {
        await StaDispatcherTestRunner.RunAsync(
            async () =>
            {
                var context = new MainWindowViewModelTestContext(DefaultSettingsFactory.Create());

                await context.ViewModel.InitializeAsync();

                context.ViewModel.CapturePinWindowHotkey(Key.C, ModifierKeys.Control);
                await context.SettingsStore.WaitForSaveCountAsync(expectedCount: 1);

                context.ViewModel.PinWindowHotkeyLabel.Should().Be("Ctrl + C");
                context.SettingsStore.LastSavedSettings.Should().NotBeNull();
                context.SettingsStore.LastSavedSettings!.Hotkeys.PinWindow.VirtualKey.Should().Be(0x43);
                context.SettingsStore.LastSavedSettings!.Hotkeys.PinWindow.Modifiers.Should().Be(HotkeyModifiers.Control);
                context.SettingsStore.LastSavedSettings!.Hotkeys.PinWindow.DisplayName.Should().Be("Ctrl + C");
            });
    }

    [Fact]
    public async Task ClearPinWindowHotkeyCommand_ShouldUnassignHotkeyAndSave()
    {
        await StaDispatcherTestRunner.RunAsync(
            async () =>
            {
                var settings = DefaultSettingsFactory.Create();
                settings.Hotkeys.PinWindow = new HotkeyBinding(0x79, "F10");

                var context = new MainWindowViewModelTestContext(settings);

                await context.ViewModel.InitializeAsync();

                context.ViewModel.ClearPinWindowHotkeyCommand.Execute(null);
                await context.SettingsStore.WaitForSaveCountAsync(expectedCount: 1);

                context.ViewModel.PinWindowHotkeyLabel.Should().Be(HotkeySettings.UnassignedDisplayName);
                context.ViewModel.PinWindowHotkeySummary.Should().Contain("No pin hotkey");
                context.SettingsStore.LastSavedSettings.Should().NotBeNull();
                context.SettingsStore.LastSavedSettings!.Hotkeys.PinWindow.VirtualKey.Should().Be(0);
                context.SettingsStore.LastSavedSettings!.Hotkeys.PinWindow.DisplayName.Should().Be(HotkeySettings.UnassignedDisplayName);
            });
    }

    [Fact]
    public async Task ToggleHotkey_WhenMainWindowIsActive_ShouldNotStartAutoClicker()
    {
        await StaDispatcherTestRunner.RunAsync(
            async () =>
            {
                var context = new MainWindowViewModelTestContext(DefaultSettingsFactory.Create());

                await context.ViewModel.InitializeAsync();
                context.ViewModel.SetMainWindowActive(true);

                await context.ViewModel.HandleHotkeyAsync(HotkeyAction.Toggle);

                context.AutoClickerController.StartAsyncCallCount.Should().Be(0);
                context.AutoClickerController.IsRunning.Should().BeFalse();
                context.ViewModel.IsRunning.Should().BeFalse();
                context.ViewModel.StatusMessage.Should().Be(
                    AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.MainStatusClickerHotkeyIgnoredWhileFocused));
            });
    }

    [Fact]
    public async Task ToggleHotkey_WhenMainWindowIsActiveAndAutoClickerIsAlreadyRunning_ShouldStillStopAutoClicker()
    {
        await StaDispatcherTestRunner.RunAsync(
            async () =>
            {
                var context = new MainWindowViewModelTestContext(DefaultSettingsFactory.Create());

                await context.ViewModel.InitializeAsync();
                context.ViewModel.SetMainWindowActive(false);
                await context.ViewModel.HandleHotkeyAsync(HotkeyAction.Toggle);

                context.ViewModel.SetMainWindowActive(true);
                await context.ViewModel.HandleHotkeyAsync(HotkeyAction.Toggle);

                context.AutoClickerController.StartAsyncCallCount.Should().Be(1);
                context.AutoClickerController.StopAsyncCallCount.Should().Be(1);
                context.AutoClickerController.IsRunning.Should().BeFalse();
                context.ViewModel.IsRunning.Should().BeFalse();
                context.ViewModel.StatusMessage.Should().Be(
                    AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.MainStatusAutomationStopped));
            });
    }
}
