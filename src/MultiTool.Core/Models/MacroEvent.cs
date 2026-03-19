using MultiTool.Core.Enums;

namespace MultiTool.Core.Models;

public readonly record struct MacroEvent(
    TimeSpan Offset,
    MacroEventKind Kind,
    int VirtualKey = 0,
    ClickMouseButton MouseButton = ClickMouseButton.Left,
    ScreenPoint Position = default);
