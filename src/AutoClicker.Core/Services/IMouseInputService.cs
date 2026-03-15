using AutoClicker.Core.Enums;

namespace AutoClicker.Core.Services;

public interface IMouseInputService
{
    void Click(ClickMouseButton mouseButton, int times);

    void Press(ClickMouseButton mouseButton);

    void Release(ClickMouseButton mouseButton);

    void ClickKey(int virtualKey, int times);

    void PressKey(int virtualKey);

    void ReleaseKey(int virtualKey);
}
