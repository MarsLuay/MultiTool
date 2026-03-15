using AutoClicker.Core.Enums;
using AutoClicker.Core.Models;
using AutoClicker.Core.Results;
using AutoClicker.Core.Services;
using FluentAssertions;

namespace AutoClicker.Core.Tests;

public sealed class AutoClickerControllerTests
{
    [Fact]
    public async Task StartAsync_ShouldStopAfterRepeatCount()
    {
        var mouseInputService = new FakeMouseInputService();
        var cursorService = new FakeCursorService();
        var controller = new AutoClickerController(mouseInputService, cursorService);
        var stopped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        controller.RunningStateChanged += (_, args) =>
        {
            if (!args.IsRunning)
            {
                stopped.TrySetResult();
            }
        };

        var settings = new ClickSettings
        {
            Milliseconds = 1,
            RepeatMode = RepeatMode.Count,
            RepeatCount = 2,
            ClickType = ClickKind.Single,
        };

        await controller.StartAsync(settings);
        var completed = await Task.WhenAny(stopped.Task, Task.Delay(TimeSpan.FromSeconds(1)));

        completed.Should().Be(stopped.Task);
        mouseInputService.TotalClicks.Should().Be(2);
    }

    [Fact]
    public async Task ToggleAsync_ShouldHoldMouseButtonUntilStopped()
    {
        var mouseInputService = new FakeMouseInputService();
        var cursorService = new FakeCursorService();
        var controller = new AutoClickerController(mouseInputService, cursorService);

        var settings = new ClickSettings
        {
            ClickType = ClickKind.Hold,
            MouseButton = ClickMouseButton.Right,
            LocationMode = ClickLocationMode.FixedPoint,
            FixedX = 120,
            FixedY = 240,
        };

        await controller.StartAsync(settings);
        await Task.Delay(50);

        mouseInputService.PressCount.Should().Be(1);
        mouseInputService.ReleaseCount.Should().Be(0);
        mouseInputService.LastPressedButton.Should().Be(ClickMouseButton.Right);
        cursorService.CurrentPoint.Should().Be(new ScreenPoint(120, 240));

        await controller.StopAsync();

        mouseInputService.ReleaseCount.Should().Be(1);
        mouseInputService.LastReleasedButton.Should().Be(ClickMouseButton.Right);
        controller.IsRunning.Should().BeFalse();
    }

    [Fact]
    public async Task StartAsync_ShouldClickCustomKeyInsteadOfMouse()
    {
        var mouseInputService = new FakeMouseInputService();
        var cursorService = new FakeCursorService();
        var controller = new AutoClickerController(mouseInputService, cursorService);
        var stopped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        controller.RunningStateChanged += (_, args) =>
        {
            if (!args.IsRunning)
            {
                stopped.TrySetResult();
            }
        };

        var settings = new ClickSettings
        {
            Milliseconds = 1,
            RepeatMode = RepeatMode.Count,
            RepeatCount = 2,
            MouseButton = ClickMouseButton.Custom,
            CustomInputKind = CustomInputKind.Keyboard,
            CustomKeyVirtualKey = 0x41,
            CustomKeyDisplayName = "A",
            LocationMode = ClickLocationMode.FixedPoint,
            FixedX = 420,
            FixedY = 840,
        };

        await controller.StartAsync(settings);
        var completed = await Task.WhenAny(stopped.Task, Task.Delay(TimeSpan.FromSeconds(1)));

        completed.Should().Be(stopped.Task);
        mouseInputService.TotalClicks.Should().Be(0);
        mouseInputService.TotalKeyClicks.Should().Be(2);
        mouseInputService.LastClickedKey.Should().Be(0x41);
        cursorService.CurrentPoint.Should().Be(new ScreenPoint(10, 10));
    }

    [Fact]
    public async Task ToggleAsync_ShouldHoldCustomKeyUntilStopped()
    {
        var mouseInputService = new FakeMouseInputService();
        var cursorService = new FakeCursorService();
        var controller = new AutoClickerController(mouseInputService, cursorService);

        var settings = new ClickSettings
        {
            ClickType = ClickKind.Hold,
            MouseButton = ClickMouseButton.Custom,
            CustomInputKind = CustomInputKind.Keyboard,
            CustomKeyVirtualKey = 0x45,
            CustomKeyDisplayName = "E",
            LocationMode = ClickLocationMode.FixedPoint,
            FixedX = 120,
            FixedY = 240,
        };

        await controller.StartAsync(settings);
        await Task.Delay(50);

        mouseInputService.KeyPressCount.Should().Be(1);
        mouseInputService.KeyReleaseCount.Should().Be(0);
        mouseInputService.LastPressedKey.Should().Be(0x45);
        cursorService.CurrentPoint.Should().Be(new ScreenPoint(10, 10));

        await controller.StopAsync();

        mouseInputService.KeyReleaseCount.Should().Be(1);
        mouseInputService.LastReleasedKey.Should().Be(0x45);
        controller.IsRunning.Should().BeFalse();
    }

    [Fact]
    public async Task StartAsync_ShouldClickCustomMouseButtonInsteadOfKeyboard()
    {
        var mouseInputService = new FakeMouseInputService();
        var cursorService = new FakeCursorService();
        var controller = new AutoClickerController(mouseInputService, cursorService);
        var stopped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        controller.RunningStateChanged += (_, args) =>
        {
            if (!args.IsRunning)
            {
                stopped.TrySetResult();
            }
        };

        var settings = new ClickSettings
        {
            Milliseconds = 1,
            RepeatMode = RepeatMode.Count,
            RepeatCount = 2,
            MouseButton = ClickMouseButton.Custom,
            CustomInputKind = CustomInputKind.MouseButton,
            CustomMouseButton = ClickMouseButton.XButton1,
            CustomKeyDisplayName = "Mouse Button 4",
            LocationMode = ClickLocationMode.FixedPoint,
            FixedX = 420,
            FixedY = 840,
        };

        await controller.StartAsync(settings);
        var completed = await Task.WhenAny(stopped.Task, Task.Delay(TimeSpan.FromSeconds(1)));

        completed.Should().Be(stopped.Task);
        mouseInputService.TotalClicks.Should().Be(2);
        mouseInputService.LastClickedButton.Should().Be(ClickMouseButton.XButton1);
        mouseInputService.TotalKeyClicks.Should().Be(0);
        cursorService.CurrentPoint.Should().Be(new ScreenPoint(420, 840));
    }

    private sealed class FakeMouseInputService : IMouseInputService
    {
        public int TotalClicks { get; private set; }

        public int TotalKeyClicks { get; private set; }

        public int PressCount { get; private set; }

        public int ReleaseCount { get; private set; }

        public int KeyPressCount { get; private set; }

        public int KeyReleaseCount { get; private set; }

        public ClickMouseButton? LastPressedButton { get; private set; }

        public ClickMouseButton? LastReleasedButton { get; private set; }

        public ClickMouseButton? LastClickedButton { get; private set; }

        public int? LastClickedKey { get; private set; }

        public int? LastPressedKey { get; private set; }

        public int? LastReleasedKey { get; private set; }

        public void Click(ClickMouseButton mouseButton, int times)
        {
            TotalClicks += times;
            LastClickedButton = mouseButton;
        }

        public void Press(ClickMouseButton mouseButton)
        {
            PressCount++;
            LastPressedButton = mouseButton;
        }

        public void Release(ClickMouseButton mouseButton)
        {
            ReleaseCount++;
            LastReleasedButton = mouseButton;
        }

        public void ClickKey(int virtualKey, int times)
        {
            TotalKeyClicks += times;
            LastClickedKey = virtualKey;
        }

        public void PressKey(int virtualKey)
        {
            KeyPressCount++;
            LastPressedKey = virtualKey;
        }

        public void ReleaseKey(int virtualKey)
        {
            KeyReleaseCount++;
            LastReleasedKey = virtualKey;
        }
    }

    private sealed class FakeCursorService : ICursorService
    {
        public ScreenPoint CurrentPoint { get; set; } = new(10, 10);

        public ScreenPoint GetCursorPosition() => CurrentPoint;

        public void SetCursorPosition(ScreenPoint point)
        {
            CurrentPoint = point;
        }
    }
}
