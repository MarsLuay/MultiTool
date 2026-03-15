using AutoClicker.Core.Enums;
using System.Windows.Input;

namespace AutoClicker.App.Helpers;

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
            ClickMouseButton.Right => "Right Mouse Button",
            ClickMouseButton.Middle => "Middle Mouse Button",
            ClickMouseButton.XButton1 => "Mouse Button 4",
            ClickMouseButton.XButton2 => "Mouse Button 5",
            _ => mouseButton.ToString(),
        };

    public static int ToVirtualKey(Key key) => KeyInterop.VirtualKeyFromKey(key);
}
