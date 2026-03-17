using AutoClicker.Core.Enums;
using AutoClicker.Core.Models;
using AutoClicker.App.Localization;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Input;

namespace AutoClicker.App.ViewModels;

public partial class MacroEventEditorItem : ObservableObject
{
    [ObservableProperty]
    private int index;

    [ObservableProperty]
    private int offsetMilliseconds;

    [ObservableProperty]
    private MacroEventKind kind;

    [ObservableProperty]
    private int virtualKey;

    [ObservableProperty]
    private ClickMouseButton mouseButton;

    [ObservableProperty]
    private int x;

    [ObservableProperty]
    private int y;

    public bool IsKeyboardKind => Kind is MacroEventKind.KeyDown or MacroEventKind.KeyUp;

    public bool IsMouseButtonKind => Kind is MacroEventKind.MouseButtonDown or MacroEventKind.MouseButtonUp;

    public bool UsesPosition => Kind is MacroEventKind.MouseMove or MacroEventKind.MouseButtonDown or MacroEventKind.MouseButtonUp;

    public string KindDisplayName => FormatKind(Kind);

    public string KeyDisplayName => FormatVirtualKey(VirtualKey);

    public string MouseButtonDisplayName => FormatMouseButton(MouseButton);

    public string PositionDisplayName => UsesPosition ? $"{X}, {Y}" : L(AppLanguageKeys.MacroEventItemPositionNotUsed);

    public string DetailsDisplayName =>
        Kind switch
        {
            MacroEventKind.MouseMove => F(AppLanguageKeys.MacroEventItemDetailsMoveMouseFormat, X, Y),
            MacroEventKind.MouseButtonDown => F(AppLanguageKeys.MacroEventItemDetailsMouseDownFormat, FormatMouseButton(MouseButton), X, Y),
            MacroEventKind.MouseButtonUp => F(AppLanguageKeys.MacroEventItemDetailsMouseUpFormat, FormatMouseButton(MouseButton), X, Y),
            MacroEventKind.KeyDown => F(AppLanguageKeys.MacroEventItemDetailsKeyDownFormat, FormatVirtualKey(VirtualKey)),
            MacroEventKind.KeyUp => F(AppLanguageKeys.MacroEventItemDetailsKeyUpFormat, FormatVirtualKey(VirtualKey)),
            _ => L(AppLanguageKeys.MacroEventItemDetailsUnavailable),
        };

    public MacroEvent ToMacroEvent() =>
        new(
            Offset: TimeSpan.FromMilliseconds(Math.Max(0, OffsetMilliseconds)),
            Kind: Kind,
            VirtualKey: Math.Max(0, VirtualKey),
            MouseButton: MouseButton,
            Position: new ScreenPoint(X, Y));

    public static MacroEventEditorItem FromMacroEvent(int index, MacroEvent macroEvent) =>
        new()
        {
            Index = index,
            OffsetMilliseconds = (int)Math.Max(0, Math.Round(macroEvent.Offset.TotalMilliseconds)),
            Kind = macroEvent.Kind,
            VirtualKey = macroEvent.VirtualKey,
            MouseButton = macroEvent.MouseButton,
            X = macroEvent.Position.X,
            Y = macroEvent.Position.Y,
        };

    partial void OnKindChanged(MacroEventKind value) => RaiseComputedPropertiesChanged();

    partial void OnVirtualKeyChanged(int value)
    {
        OnPropertyChanged(nameof(KeyDisplayName));
        OnPropertyChanged(nameof(DetailsDisplayName));
    }

    partial void OnMouseButtonChanged(ClickMouseButton value)
    {
        OnPropertyChanged(nameof(MouseButtonDisplayName));
        OnPropertyChanged(nameof(DetailsDisplayName));
    }

    partial void OnXChanged(int value)
    {
        OnPropertyChanged(nameof(PositionDisplayName));
        OnPropertyChanged(nameof(DetailsDisplayName));
    }

    partial void OnYChanged(int value)
    {
        OnPropertyChanged(nameof(PositionDisplayName));
        OnPropertyChanged(nameof(DetailsDisplayName));
    }

    private void RaiseComputedPropertiesChanged()
    {
        OnPropertyChanged(nameof(IsKeyboardKind));
        OnPropertyChanged(nameof(IsMouseButtonKind));
        OnPropertyChanged(nameof(UsesPosition));
        OnPropertyChanged(nameof(KindDisplayName));
        OnPropertyChanged(nameof(PositionDisplayName));
        OnPropertyChanged(nameof(DetailsDisplayName));
    }

    private static string FormatKind(MacroEventKind kind) =>
        kind switch
        {
            MacroEventKind.MouseMove => L(AppLanguageKeys.EnumMoveMouse),
            MacroEventKind.MouseButtonDown => L(AppLanguageKeys.EnumMouseDown),
            MacroEventKind.MouseButtonUp => L(AppLanguageKeys.EnumMouseUp),
            MacroEventKind.KeyDown => L(AppLanguageKeys.EnumKeyDown),
            MacroEventKind.KeyUp => L(AppLanguageKeys.EnumKeyUp),
            _ => kind.ToString(),
        };

    private static string FormatMouseButton(ClickMouseButton button) =>
        button switch
        {
            ClickMouseButton.Left => L(AppLanguageKeys.EnumLeft),
            ClickMouseButton.Right => L(AppLanguageKeys.EnumRight),
            ClickMouseButton.Middle => L(AppLanguageKeys.EnumMiddle),
            ClickMouseButton.XButton1 => L(AppLanguageKeys.EnumSide1),
            ClickMouseButton.XButton2 => L(AppLanguageKeys.EnumSide2),
            ClickMouseButton.Custom => L(AppLanguageKeys.EnumCustom),
            _ => button.ToString(),
        };

    private static string FormatVirtualKey(int virtualKey)
    {
        if (virtualKey <= 0)
        {
            return L(AppLanguageKeys.MacroEventItemVirtualKeyNone);
        }

        var key = KeyInterop.KeyFromVirtualKey(virtualKey);
        return key == Key.None
            ? F(AppLanguageKeys.MacroEventItemVirtualKeyOnlyFormat, virtualKey)
            : F(AppLanguageKeys.MacroEventItemVirtualKeyNamedFormat, key, virtualKey);
    }

    private static string L(string key) => AppLanguageStrings.GetForCurrentLanguage(key);

    private static string F(string key, params object[] args) => AppLanguageStrings.FormatForCurrentLanguage(key, args);
}
