namespace AutoClicker.Core.Models;

public sealed class UiSettings
{
    public bool? IsDarkMode { get; set; }

    public bool EnableCtrlWheelResize { get; set; } = true;

    public UiSettings Clone() =>
        new()
        {
            IsDarkMode = IsDarkMode,
            EnableCtrlWheelResize = EnableCtrlWheelResize,
        };
}
