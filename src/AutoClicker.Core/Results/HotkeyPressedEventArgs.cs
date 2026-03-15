using AutoClicker.Core.Enums;

namespace AutoClicker.Core.Results;

public sealed class HotkeyPressedEventArgs : EventArgs
{
    public HotkeyPressedEventArgs(HotkeyAction action)
    {
        Action = action;
    }

    public HotkeyAction Action { get; }
}
