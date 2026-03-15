using AutoClicker.Core.Enums;

namespace AutoClicker.Core.Models;

public sealed class ClickSettings
{
    public int Hours { get; set; }

    public int Minutes { get; set; }

    public int Seconds { get; set; }

    public int Milliseconds { get; set; } = 1;

    public ClickMouseButton MouseButton { get; set; } = ClickMouseButton.Left;

    public CustomInputKind CustomInputKind { get; set; }

    public int CustomKeyVirtualKey { get; set; }

    public string CustomKeyDisplayName { get; set; } = string.Empty;

    public ClickMouseButton CustomMouseButton { get; set; } = ClickMouseButton.Left;

    public ClickKind ClickType { get; set; } = ClickKind.Single;

    public RepeatMode RepeatMode { get; set; } = RepeatMode.Infinite;

    public ClickLocationMode LocationMode { get; set; } = ClickLocationMode.CurrentCursor;

    public int FixedX { get; set; }

    public int FixedY { get; set; }

    public int RepeatCount { get; set; } = 1;

    public bool AlwaysOnTop { get; set; }

    public TimeSpan GetInterval()
    {
        var totalMilliseconds =
            ((long)Hours * 60 * 60 * 1000)
            + ((long)Minutes * 60 * 1000)
            + ((long)Seconds * 1000)
            + Milliseconds;

        return TimeSpan.FromMilliseconds(totalMilliseconds);
    }

    public ScreenPoint GetFixedPoint() => new(FixedX, FixedY);

    public ClickSettings Clone() =>
        new()
        {
            Hours = Hours,
            Minutes = Minutes,
            Seconds = Seconds,
            Milliseconds = Milliseconds,
            MouseButton = MouseButton,
            CustomInputKind = CustomInputKind,
            CustomKeyVirtualKey = CustomKeyVirtualKey,
            CustomKeyDisplayName = CustomKeyDisplayName,
            CustomMouseButton = CustomMouseButton,
            ClickType = ClickType,
            RepeatMode = RepeatMode,
            LocationMode = LocationMode,
            FixedX = FixedX,
            FixedY = FixedY,
            RepeatCount = RepeatCount,
            AlwaysOnTop = AlwaysOnTop,
        };
}
