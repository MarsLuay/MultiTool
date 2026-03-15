using AutoClicker.Core.Enums;
using AutoClicker.Core.Models;
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

    public string PositionDisplayName => UsesPosition ? $"{X}, {Y}" : "Not used";

    public string DetailsDisplayName =>
        Kind switch
        {
            MacroEventKind.MouseMove => $"Move mouse to {X}, {Y}",
            MacroEventKind.MouseButtonDown => $"{FormatMouseButton(MouseButton)} down at {X}, {Y}",
            MacroEventKind.MouseButtonUp => $"{FormatMouseButton(MouseButton)} up at {X}, {Y}",
            MacroEventKind.KeyDown => $"{FormatVirtualKey(VirtualKey)} down",
            MacroEventKind.KeyUp => $"{FormatVirtualKey(VirtualKey)} up",
            _ => "Event details unavailable",
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
            MacroEventKind.MouseMove => "Move Mouse",
            MacroEventKind.MouseButtonDown => "Mouse Down",
            MacroEventKind.MouseButtonUp => "Mouse Up",
            MacroEventKind.KeyDown => "Key Down",
            MacroEventKind.KeyUp => "Key Up",
            _ => kind.ToString(),
        };

    private static string FormatMouseButton(ClickMouseButton button) =>
        button switch
        {
            ClickMouseButton.Left => "Left",
            ClickMouseButton.Right => "Right",
            ClickMouseButton.Middle => "Middle",
            ClickMouseButton.XButton1 => "Side 1",
            ClickMouseButton.XButton2 => "Side 2",
            ClickMouseButton.Custom => "Custom",
            _ => button.ToString(),
        };

    private static string FormatVirtualKey(int virtualKey)
    {
        if (virtualKey <= 0)
        {
            return "None";
        }

        var key = KeyInterop.KeyFromVirtualKey(virtualKey);
        return key == Key.None
            ? $"VK {virtualKey}"
            : $"{key} (VK {virtualKey})";
    }
}
