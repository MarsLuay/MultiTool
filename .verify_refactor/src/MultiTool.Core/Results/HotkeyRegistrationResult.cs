using MultiTool.Core.Enums;
using MultiTool.Core.Models;

namespace MultiTool.Core.Results;

public sealed class HotkeyRegistrationResult
{
    public HotkeyRegistrationResult(
        HotkeyAction action,
        HotkeyBinding binding,
        HotkeyModifiers modifiers,
        bool succeeded,
        string? actionLabel = null)
    {
        Action = action;
        Binding = binding;
        Modifiers = modifiers;
        Succeeded = succeeded;
        ActionLabel = actionLabel;
    }

    public HotkeyAction Action { get; }

    public HotkeyBinding Binding { get; }

    public HotkeyModifiers Modifiers { get; }

    public bool Succeeded { get; }

    public string? ActionLabel { get; }

    public string Describe()
    {
        var modifierText = Modifiers == HotkeyModifiers.None ? "None" : Modifiers.ToString();
        var status = Succeeded ? "registered" : "failed";
        var label = string.IsNullOrWhiteSpace(ActionLabel) ? Action.ToString() : ActionLabel;
        return $"{label} hotkey {Binding.DisplayName} ({modifierText}) {status}.";
    }
}
