using AutoClicker.Core.Enums;

namespace AutoClicker.Core.Models;

public sealed class MacroHotkeyAssignment
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string MacroFilePath { get; set; } = string.Empty;

    public string MacroDisplayName { get; set; } = string.Empty;

    public HotkeyBinding Hotkey { get; set; } = new();

    public MacroHotkeyPlaybackMode PlaybackMode { get; set; }

    public bool IsEnabled { get; set; } = true;

    public MacroHotkeyAssignment Clone() =>
        new()
        {
            Id = Id,
            MacroFilePath = MacroFilePath,
            MacroDisplayName = MacroDisplayName,
            Hotkey = Hotkey.Clone(),
            PlaybackMode = PlaybackMode,
            IsEnabled = IsEnabled,
        };
}
