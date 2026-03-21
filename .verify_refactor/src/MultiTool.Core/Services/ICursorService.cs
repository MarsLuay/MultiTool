using MultiTool.Core.Models;

namespace MultiTool.Core.Services;

public interface ICursorService
{
    ScreenPoint GetCursorPosition();

    void SetCursorPosition(ScreenPoint point);
}
