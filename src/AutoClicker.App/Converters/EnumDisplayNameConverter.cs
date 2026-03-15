using System.Globalization;
using System.Windows.Data;
using AutoClicker.Core.Enums;

namespace AutoClicker.App.Converters;

public sealed class EnumDisplayNameConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value switch
        {
            RepeatMode.Infinite => "Infinite",
            RepeatMode.Count => "Count",
            ClickLocationMode.CurrentCursor => "Cursor",
            ClickLocationMode.FixedPoint => "Fixed",
            ClickMouseButton.Left => "Left",
            ClickMouseButton.Right => "Right",
            ClickMouseButton.Middle => "Middle",
            ClickMouseButton.Custom => "Custom",
            ClickMouseButton.XButton1 => "Side 1",
            ClickMouseButton.XButton2 => "Side 2",
            ClickKind.Single => "Single",
            ClickKind.Double => "Double",
            ClickKind.Hold => "Hold",
            MacroEventKind.MouseMove => "Move Mouse",
            MacroEventKind.MouseButtonDown => "Mouse Down",
            MacroEventKind.MouseButtonUp => "Mouse Up",
            MacroEventKind.KeyDown => "Key Down",
            MacroEventKind.KeyUp => "Key Up",
            _ => value?.ToString() ?? string.Empty,
        };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
