namespace AutoClicker.Core.Models;

public sealed class UiSettings
{
    public bool? IsDarkMode { get; set; }

    public bool EnableCtrlWheelResize { get; set; } = true;

    public bool AutoHideOnStartup { get; set; }

    public UiSettings Clone() =>
        new()
        {
            IsDarkMode = IsDarkMode,
            EnableCtrlWheelResize = EnableCtrlWheelResize,
            AutoHideOnStartup = AutoHideOnStartup,
        };
}
