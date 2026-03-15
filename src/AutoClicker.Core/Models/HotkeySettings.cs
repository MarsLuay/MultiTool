namespace AutoClicker.Core.Models;

public sealed class HotkeySettings
{
    public const int DefaultToggleVirtualKey = 0x6D;

    public const int LegacyDefaultToggleVirtualKey = 0x77;

    public const string DefaultToggleDisplayName = "-";

    public HotkeyBinding Toggle { get; set; } = CreateDefaultToggleBinding();

    public bool AllowModifierVariants { get; set; }

    public static HotkeyBinding CreateDefaultToggleBinding() => new(DefaultToggleVirtualKey, DefaultToggleDisplayName);

    public HotkeySettings Clone() =>
        new()
        {
            Toggle = Toggle.Clone(),
            AllowModifierVariants = AllowModifierVariants,
        };
}
