using AutoClicker.Core.Enums;
using AutoClicker.Core.Models;

namespace AutoClicker.Core.Results;

public sealed class HotkeyRegistrationResult
{
    public HotkeyRegistrationResult(
        HotkeyAction action,
        HotkeyBinding binding,
        HotkeyModifiers modifiers,
        bool succeeded)
    {
        Action = action;
        Binding = binding;
        Modifiers = modifiers;
        Succeeded = succeeded;
    }

    public HotkeyAction Action { get; }

    public HotkeyBinding Binding { get; }

    public HotkeyModifiers Modifiers { get; }

    public bool Succeeded { get; }

    public string Describe()
    {
        var modifierText = Modifiers == HotkeyModifiers.None ? "None" : Modifiers.ToString();
        var status = Succeeded ? "registered" : "failed";
        return $"{Action} hotkey {Binding.DisplayName} ({modifierText}) {status}.";
    }
}
