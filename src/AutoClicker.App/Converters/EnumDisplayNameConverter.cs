using System.Globalization;
using System.Windows.Data;
using AutoClicker.App.Localization;
using AutoClicker.Core.Enums;

namespace AutoClicker.App.Converters;

public sealed class EnumDisplayNameConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value switch
        {
            RepeatMode.Infinite => "∞",
            RepeatMode.Count => AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.EnumCount),
            ClickLocationMode.CurrentCursor => AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.EnumCursor),
            ClickLocationMode.FixedPoint => AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.EnumFixed),
            ClickMouseButton.Left => AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.EnumLeft),
            ClickMouseButton.Right => AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.EnumRight),
            ClickMouseButton.Middle => AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.EnumMiddle),
            ClickMouseButton.Custom => AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.EnumCustom),
            ClickMouseButton.XButton1 => AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.EnumSide1),
            ClickMouseButton.XButton2 => AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.EnumSide2),
            ClickKind.Single => AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.EnumSingle),
            ClickKind.Double => AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.EnumDouble),
            ClickKind.Hold => AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.EnumHold),
            MacroHotkeyPlaybackMode.PlayOnce => AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.EnumRunOnce),
            MacroHotkeyPlaybackMode.ToggleRepeat => AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.EnumStartStop),
            MacroEventKind.MouseMove => AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.EnumMoveMouse),
            MacroEventKind.MouseButtonDown => AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.EnumMouseDown),
            MacroEventKind.MouseButtonUp => AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.EnumMouseUp),
            MacroEventKind.KeyDown => AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.EnumKeyDown),
            MacroEventKind.KeyUp => AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.EnumKeyUp),
            _ => value?.ToString() ?? string.Empty,
        };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
