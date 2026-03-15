using AutoClicker.Core.Models;
using AutoClicker.Core.Services;

namespace AutoClicker.Infrastructure.Windows.Input;

public sealed class WindowsCursorService : ICursorService
{
    public ScreenPoint GetCursorPosition()
    {
        var position = System.Windows.Forms.Cursor.Position;
        return new ScreenPoint(position.X, position.Y);
    }

    public void SetCursorPosition(ScreenPoint point)
    {
        if (!Interop.User32.SetCursorPosition(point.X, point.Y))
        {
            throw new InvalidOperationException("Failed to move the mouse cursor to the requested position.");
        }
    }
}
