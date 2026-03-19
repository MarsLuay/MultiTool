using MultiTool.Core.Models;
using MultiTool.Core.Results;

namespace MultiTool.Core.Services;

public interface IHotkeyService : IDisposable
{
    event EventHandler<HotkeyPressedEventArgs>? HotkeyPressed;

    bool IsAttached { get; }

    void Attach(nint windowHandle);

    IReadOnlyCollection<HotkeyRegistrationResult> RegisterHotkeys(
        HotkeySettings settings,
        ScreenshotSettings screenshotSettings,
        MacroSettings macroSettings);

    void UnregisterAll();
}
