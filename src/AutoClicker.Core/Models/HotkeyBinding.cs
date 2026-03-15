using AutoClicker.Core.Enums;

namespace AutoClicker.Core.Models;

public sealed class HotkeyBinding
{
    public HotkeyBinding()
    {
    }

    public HotkeyBinding(int virtualKey, string displayName)
    {
        VirtualKey = virtualKey;
        DisplayName = displayName;
    }

    public HotkeyBinding(int virtualKey, string displayName, HotkeyInputKind inputKind, ClickMouseButton mouseButton = ClickMouseButton.Left)
    {
        VirtualKey = virtualKey;
        DisplayName = displayName;
        InputKind = inputKind;
        MouseButton = mouseButton;
    }

    public int VirtualKey { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public HotkeyInputKind InputKind { get; set; } = HotkeyInputKind.Keyboard;

    public ClickMouseButton MouseButton { get; set; } = ClickMouseButton.Left;

    public HotkeyBinding Clone() => new(VirtualKey, DisplayName, InputKind, MouseButton);
}
