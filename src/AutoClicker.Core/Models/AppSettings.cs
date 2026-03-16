namespace AutoClicker.Core.Models;

public sealed class AppSettings
{
    public const int CurrentVersion = 9;

    public int Version { get; set; } = CurrentVersion;

    public ClickSettings Clicker { get; set; } = new();

    public HotkeySettings Hotkeys { get; set; } = new();

    public ScreenshotSettings Screenshot { get; set; } = new();

    public MacroSettings Macro { get; set; } = new();

    public InstallerSettings Installer { get; set; } = new();

    public ToolSettings Tools { get; set; } = new();

    public UiSettings Ui { get; set; } = new();

    public AppSettings Clone() =>
        new()
        {
            Version = Version,
            Clicker = Clicker.Clone(),
            Hotkeys = Hotkeys.Clone(),
            Screenshot = Screenshot.Clone(),
            Macro = Macro.Clone(),
            Installer = Installer.Clone(),
            Tools = Tools.Clone(),
            Ui = Ui.Clone(),
        };
}
