using System.Windows.Input;
using MultiTool.App.Localization;
using MultiTool.Core.Enums;
using MultiTool.Core.Models;

namespace MultiTool.App.Helpers;

public static class HotkeyDisplayNameFormatter
{
    public static string FormatVirtualKey(int virtualKey)
    {
        if (virtualKey == 0x6A)
        {
            return "*";
        }

        if (virtualKey is 0x6B or 0xBB)
        {
            return "+";
        }

        if (virtualKey is 0x6D or 0xBD)
        {
            return "-";
        }

        var name = Enum.GetName(typeof(System.Windows.Forms.Keys), virtualKey);
        return string.IsNullOrWhiteSpace(name)
            ? $"0x{virtualKey:X2}"
            : name;
    }

    public static string FormatMouseButton(ClickMouseButton mouseButton) =>
        mouseButton switch
        {
            ClickMouseButton.Right => AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.RightMouseButton),
            ClickMouseButton.Middle => AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.MiddleMouseButton),
            ClickMouseButton.XButton1 => AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.MouseButton4),
            ClickMouseButton.XButton2 => AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.MouseButton5),
            _ => mouseButton.ToString(),
        };

    public static string FormatKeyboardHotkey(int virtualKey, HotkeyModifiers modifiers)
    {
        var modifierNames = GetModifierDisplayNames(modifiers);
        if (modifierNames.Count == 0)
        {
            return FormatVirtualKey(virtualKey);
        }

        return string.Join(" + ", [.. modifierNames, FormatVirtualKey(virtualKey)]);
    }

    public static HotkeyBinding CreateKeyboardBinding(Key key, ModifierKeys modifiers)
    {
        var virtualKey = ToVirtualKey(key);
        var hotkeyModifiers = ToHotkeyModifiers(modifiers);
        return new HotkeyBinding(virtualKey, FormatKeyboardHotkey(virtualKey, hotkeyModifiers))
        {
            Modifiers = hotkeyModifiers,
        };
    }

    public static HotkeyModifiers ToHotkeyModifiers(ModifierKeys modifiers)
    {
        var hotkeyModifiers = HotkeyModifiers.None;

        if ((modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            hotkeyModifiers |= HotkeyModifiers.Control;
        }

        if ((modifiers & ModifierKeys.Alt) == ModifierKeys.Alt)
        {
            hotkeyModifiers |= HotkeyModifiers.Alt;
        }

        if ((modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
        {
            hotkeyModifiers |= HotkeyModifiers.Shift;
        }

        if ((modifiers & ModifierKeys.Windows) == ModifierKeys.Windows)
        {
            hotkeyModifiers |= HotkeyModifiers.Windows;
        }

        return hotkeyModifiers;
    }

    public static int ToVirtualKey(Key key) => KeyInterop.VirtualKeyFromKey(key);

    private static IReadOnlyList<string> GetModifierDisplayNames(HotkeyModifiers modifiers)
    {
        var names = new List<string>(4);
        if ((modifiers & HotkeyModifiers.Control) == HotkeyModifiers.Control)
        {
            names.Add("Ctrl");
        }

        if ((modifiers & HotkeyModifiers.Alt) == HotkeyModifiers.Alt)
        {
            names.Add("Alt");
        }

        if ((modifiers & HotkeyModifiers.Shift) == HotkeyModifiers.Shift)
        {
            names.Add("Shift");
        }

        if ((modifiers & HotkeyModifiers.Windows) == HotkeyModifiers.Windows)
        {
            names.Add("Win");
        }

        return names;
    }
}
