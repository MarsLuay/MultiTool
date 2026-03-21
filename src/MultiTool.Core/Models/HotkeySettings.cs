namespace MultiTool.Core.Models;

public sealed class HotkeySettings
{
    public const int DefaultToggleVirtualKey = 0x6D;

    public const int LegacyDefaultToggleVirtualKey = 0x77;

    public const string DefaultToggleDisplayName = "-";

    public const string UnassignedDisplayName = "Not set";

    public HotkeyBinding Toggle { get; set; } = CreateDefaultToggleBinding();

    public HotkeyBinding PinWindow { get; set; } = CreateUnassignedBinding();

    public bool AllowModifierVariants { get; set; } = true;

    public bool OverrideApplicationShortcuts { get; set; }

    public static HotkeyBinding CreateDefaultToggleBinding() => new(DefaultToggleVirtualKey, DefaultToggleDisplayName);

    public static HotkeyBinding CreateUnassignedBinding() => new(0, UnassignedDisplayName);

    public HotkeySettings Clone() =>
        new()
        {
            Toggle = Toggle.Clone(),
            PinWindow = PinWindow.Clone(),
            AllowModifierVariants = AllowModifierVariants,
            OverrideApplicationShortcuts = OverrideApplicationShortcuts,
        };
}
