using MultiTool.Core.Models;

namespace MultiTool.Core.Results;

public sealed class VideoCaptureStateChangedEventArgs : EventArgs
{
    public VideoCaptureStateChangedEventArgs(bool isRecording, ScreenRectangle? captureArea)
    {
        IsRecording = isRecording;
        CaptureArea = captureArea;
    }

    public bool IsRecording { get; }

    public ScreenRectangle? CaptureArea { get; }
}
