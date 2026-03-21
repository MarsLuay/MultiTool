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
    public async Task ScreenshotPreview_ShouldUnloadWhenLeavingScreenshotTabAndReloadWhenReturning()
    {
        await StaDispatcherTestRunner.RunAsync(
            async () =>
            {
                var settings = DefaultSettingsFactory.Create();
                settings.Screenshot.SaveFolderPath = Path.Combine(Path.GetTempPath(), $"multitool-screenshot-test-{Guid.NewGuid():N}");

                var context = new MainWindowViewModelTestContext(settings);

                await context.ViewModel.InitializeAsync();
                context.ViewModel.SelectedMainTabIndex = 1;

                await context.ViewModel.HandleHotkeyAsync(HotkeyAction.ScreenshotCapture);

                context.ViewModel.LatestScreenshotPreview.Should().NotBeNull();
                context.ViewModel.HasLatestScreenshot.Should().BeTrue();

                context.ViewModel.SelectedMainTabIndex = 0;

                context.ViewModel.LatestScreenshotPreview.Should().BeNull();
                context.ViewModel.HasLatestScreenshot.Should().BeFalse();

                context.ViewModel.SelectedMainTabIndex = 1;

                context.ViewModel.LatestScreenshotPreview.Should().NotBeNull();
                context.ViewModel.HasLatestScreenshot.Should().BeTrue();
            });
    }

    [Fact]
    public async Task LatestVideoPath_ShouldUnloadWhenLeavingScreenshotTabAndReloadWhenReturning()
    {
        await StaDispatcherTestRunner.RunAsync(
            async () =>
            {
                var settings = DefaultSettingsFactory.Create();
                settings.Screenshot.SaveFolderPath = Path.Combine(Path.GetTempPath(), $"multitool-video-test-{Guid.NewGuid():N}");

                var context = new MainWindowViewModelTestContext(settings);

                await context.ViewModel.InitializeAsync();
                context.ViewModel.SelectedMainTabIndex = 1;
                context.ScreenshotCaptureService.SetVideoCaptureRunning();

                await context.ViewModel.HandleHotkeyAsync(HotkeyAction.ScreenshotCapture);

                context.ViewModel.LatestVideoPath.Should().NotBeNull();
                context.ViewModel.HasLatestVideo.Should().BeTrue();

                context.ViewModel.SelectedMainTabIndex = 0;

                context.ViewModel.LatestVideoPath.Should().BeNull();
                context.ViewModel.HasLatestVideo.Should().BeFalse();

                context.ViewModel.SelectedMainTabIndex = 1;

                context.ViewModel.LatestVideoPath.Should().NotBeNull();
                context.ViewModel.HasLatestVideo.Should().BeTrue();
            });
    }

    [Fact]
    public async Task ScreenshotHotkey_WhenRepeatedImmediatelyAfterStoppingVideo_ShouldNotCaptureScreenshot()
    {
        await StaDispatcherTestRunner.RunAsync(
            async () =>
            {
                var settings = DefaultSettingsFactory.Create();
                settings.Screenshot.SaveFolderPath = Path.Combine(Path.GetTempPath(), $"multitool-video-stop-test-{Guid.NewGuid():N}");

                var context = new MainWindowViewModelTestContext(settings);

                await context.ViewModel.InitializeAsync();
                context.ViewModel.SelectedMainTabIndex = 1;
                context.ScreenshotCaptureService.SetVideoCaptureRunning();

                var stopTask = context.ViewModel.HandleHotkeyAsync(HotkeyAction.ScreenshotCapture);
                await Task.Delay(50);
                var repeatedTask = context.ViewModel.HandleHotkeyAsync(HotkeyAction.ScreenshotCapture);

                await Task.WhenAll(stopTask, repeatedTask);
                await Task.Delay(450);

                context.ScreenshotCaptureService.CaptureDesktopCallCount.Should().Be(0);
                context.ScreenshotCaptureService.CaptureAreaCallCount.Should().Be(0);
                context.ViewModel.LatestVideoPath.Should().NotBeNull();
                context.ViewModel.LatestVideoCaption.Should().NotBe(
                    AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.MainLatestVideoNone));
                context.ViewModel.ScreenshotStatusMessage.Should().StartWith("Saved video:");
            });
    }

    [Fact]
    public async Task ScreenshotHotkey_WhenPressedAgainDuringAreaSelection_ShouldPromoteToVideoCapture()
    {
        await StaDispatcherTestRunner.RunAsync(
            async () =>
            {
                var context = new MainWindowViewModelTestContext(DefaultSettingsFactory.Create());
                await context.ViewModel.InitializeAsync();
                var promotedVideoArea = new ScreenRectangle(10, 20, 300, 200);

                context.ScreenshotAreaSelectionService.EnqueueBehavior(
                    async cancellationToken =>
                    {
                        try
                        {
                            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                        }
                        catch (OperationCanceledException)
                        {
                        }

                        return null;
                    });
                context.ScreenshotAreaSelectionService.EnqueueVideoSelectionResult(
                    new VideoCaptureSelection(VideoCaptureSelectionKind.CurrentScreen, promotedVideoArea));

                var firstPressTask = context.ViewModel.HandleHotkeyAsync(HotkeyAction.ScreenshotCapture);
                await Task.Delay(50);
                var secondPressTask = context.ViewModel.HandleHotkeyAsync(HotkeyAction.ScreenshotCapture);

                await context.ScreenshotAreaSelectionService.WaitForCallCountAsync(expectedCount: 1);

                var thirdPressTask = context.ViewModel.HandleHotkeyAsync(HotkeyAction.ScreenshotCapture);

                await Task.WhenAll(firstPressTask, secondPressTask, thirdPressTask);
                await context.ScreenshotCaptureService.WaitForVideoStartCountAsync(expectedCount: 1);

                context.ScreenshotAreaSelectionService.CallCount.Should().Be(1);
                context.ScreenshotAreaSelectionService.VideoSelectionCallCount.Should().Be(1);
                context.ScreenshotCaptureService.StartVideoCaptureCallCount.Should().Be(1);
                context.ScreenshotCaptureService.CaptureAreaCallCount.Should().Be(0);
                context.ScreenshotCaptureService.LastStartVideoCaptureArea.Should().Be(promotedVideoArea);
                context.ViewModel.ScreenshotStatusMessage.Should().Be(
                    AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.MainScreenshotStatusCurrentScreenRecordingStarted));
            });
    }

    [Fact]
    public async Task ScreenshotHotkey_WhenPressedThreeTimes_ShouldOpenVideoPickerAndStartChosenCapture()
    {
        await StaDispatcherTestRunner.RunAsync(
            async () =>
            {
                var context = new MainWindowViewModelTestContext(DefaultSettingsFactory.Create());
                await context.ViewModel.InitializeAsync();

                context.ScreenshotAreaSelectionService.EnqueueVideoSelectionResult(
                    new VideoCaptureSelection(VideoCaptureSelectionKind.AllScreens, null));

                var firstPressTask = context.ViewModel.HandleHotkeyAsync(HotkeyAction.ScreenshotCapture);
                await Task.Delay(50);
                var secondPressTask = context.ViewModel.HandleHotkeyAsync(HotkeyAction.ScreenshotCapture);
                await Task.Delay(50);
                var thirdPressTask = context.ViewModel.HandleHotkeyAsync(HotkeyAction.ScreenshotCapture);

                await Task.WhenAll(firstPressTask, secondPressTask, thirdPressTask);
                await context.ScreenshotCaptureService.WaitForVideoStartCountAsync(expectedCount: 1);

                context.ScreenshotAreaSelectionService.CallCount.Should().Be(0);
                context.ScreenshotAreaSelectionService.VideoSelectionCallCount.Should().Be(1);
                context.ScreenshotCaptureService.StartVideoCaptureCallCount.Should().Be(1);
                context.ScreenshotCaptureService.LastStartVideoCaptureArea.Should().BeNull();
                context.ViewModel.ScreenshotStatusMessage.Should().Be(
                    AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.MainScreenshotStatusAllScreensRecordingStarted));
            });
    }

    [Fact]
    public async Task ScreenshotHotkey_WhenPressedFourTimes_ShouldStartCurrentScreenRecordingWithoutPicker()
    {
        await StaDispatcherTestRunner.RunAsync(
            async () =>
            {
                var context = new MainWindowViewModelTestContext(DefaultSettingsFactory.Create());
                await context.ViewModel.InitializeAsync();

                var firstPressTask = context.ViewModel.HandleHotkeyAsync(HotkeyAction.ScreenshotCapture);
                await Task.Delay(50);
                var secondPressTask = context.ViewModel.HandleHotkeyAsync(HotkeyAction.ScreenshotCapture);
                await Task.Delay(50);
                var thirdPressTask = context.ViewModel.HandleHotkeyAsync(HotkeyAction.ScreenshotCapture);
                await Task.Delay(50);
                var fourthPressTask = context.ViewModel.HandleHotkeyAsync(HotkeyAction.ScreenshotCapture);

                await Task.WhenAll(firstPressTask, secondPressTask, thirdPressTask, fourthPressTask);
                await context.ScreenshotCaptureService.WaitForVideoStartCountAsync(expectedCount: 1);

                context.ScreenshotAreaSelectionService.CallCount.Should().Be(0);
                context.ScreenshotAreaSelectionService.VideoSelectionCallCount.Should().Be(0);
                context.ScreenshotCaptureService.StartVideoCaptureCallCount.Should().Be(1);
                context.ScreenshotCaptureService.LastStartVideoCaptureArea.Should().NotBeNull();
                context.ScreenshotCaptureService.LastStartVideoCaptureArea!.Value.Width.Should().BeGreaterThan(0);
                context.ScreenshotCaptureService.LastStartVideoCaptureArea!.Value.Height.Should().BeGreaterThan(0);
                context.ViewModel.ScreenshotStatusMessage.Should().Be(
                    AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.MainScreenshotStatusCurrentScreenRecordingStarted));
            });
    }
}
