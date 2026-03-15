namespace AutoClicker.Core.Models;

public sealed class MacroSettings
{
    public const int DefaultPlayVirtualKey = 0x6B;

    public const string DefaultPlayDisplayName = "+";

    public const int DefaultRecordVirtualKey = 0x6F;

    public const string DefaultRecordDisplayName = "/";

    public HotkeyBinding PlayHotkey { get; set; } = CreateDefaultPlayBinding();

    public HotkeyBinding RecordHotkey { get; set; } = CreateDefaultRecordBinding();

    public bool RecordMouseMovement { get; set; } = true;

    public static HotkeyBinding CreateDefaultPlayBinding() => new(DefaultPlayVirtualKey, DefaultPlayDisplayName);

    public static HotkeyBinding CreateDefaultRecordBinding() => new(DefaultRecordVirtualKey, DefaultRecordDisplayName);

    public MacroSettings Clone() =>
        new()
        {
            PlayHotkey = PlayHotkey.Clone(),
            RecordHotkey = RecordHotkey.Clone(),
            RecordMouseMovement = RecordMouseMovement,
        };
}
