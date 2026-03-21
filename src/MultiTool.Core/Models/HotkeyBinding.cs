using MultiTool.Core.Enums;

namespace MultiTool.Core.Models;

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

    public HotkeyBinding(int virtualKey, string displayName, HotkeyInputKind inputKind, ClickMouseButton mouseButton, HotkeyModifiers modifiers)
    {
        VirtualKey = virtualKey;
        DisplayName = displayName;
        InputKind = inputKind;
        MouseButton = mouseButton;
        Modifiers = modifiers;
    }

    public int VirtualKey { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public HotkeyInputKind InputKind { get; set; } = HotkeyInputKind.Keyboard;

    public ClickMouseButton MouseButton { get; set; } = ClickMouseButton.Left;

    public HotkeyModifiers Modifiers { get; set; }

    public HotkeyBinding Clone() => new(VirtualKey, DisplayName, InputKind, MouseButton, Modifiers);
}
