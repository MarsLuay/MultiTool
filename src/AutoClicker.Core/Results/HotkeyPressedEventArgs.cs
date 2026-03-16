using AutoClicker.Core.Enums;

namespace AutoClicker.Core.Results;

public sealed class HotkeyPressedEventArgs : EventArgs
{
    public HotkeyPressedEventArgs(HotkeyAction action, string? payload = null)
    {
        Action = action;
        Payload = payload;
    }

    public HotkeyAction Action { get; }

    public string? Payload { get; }
}
