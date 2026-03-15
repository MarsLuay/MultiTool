using AutoClicker.Core.Models;

namespace AutoClicker.Core.Services;

public interface ICursorService
{
    ScreenPoint GetCursorPosition();

    void SetCursorPosition(ScreenPoint point);
}
