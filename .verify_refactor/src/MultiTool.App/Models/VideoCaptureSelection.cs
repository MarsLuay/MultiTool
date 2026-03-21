using MultiTool.Core.Models;

namespace MultiTool.App.Models;

public enum VideoCaptureSelectionKind
{
    Area,
    CurrentScreen,
    AllScreens,
}

public sealed record VideoCaptureSelection(
    VideoCaptureSelectionKind Kind,
    ScreenRectangle? Area);
