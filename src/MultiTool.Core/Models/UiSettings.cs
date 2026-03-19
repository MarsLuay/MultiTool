namespace MultiTool.Core.Models;

public sealed class UiSettings
{
    public bool? IsDarkMode { get; set; }

    public bool EnableCtrlWheelResize { get; set; } = true;

    public bool? RunAtStartup { get; set; }

    public bool AutoHideOnStartup { get; set; }

    public bool SillyMode { get; set; }

    public UiSettings Clone() =>
        new()
        {
            IsDarkMode = IsDarkMode,
            EnableCtrlWheelResize = EnableCtrlWheelResize,
            RunAtStartup = RunAtStartup,
            AutoHideOnStartup = AutoHideOnStartup,
            SillyMode = SillyMode,
        };
}
