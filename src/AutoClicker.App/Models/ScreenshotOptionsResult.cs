using AutoClicker.Core.Enums;

namespace AutoClicker.App.Models;

public sealed class ScreenshotOptionsResult
{
    public static ScreenshotOptionsResult Canceled { get; } = new(isCanceled: true, wasIgnoredBecauseAlreadyOpen: false, wasHandledInDialog: false, mode: null);

    public static ScreenshotOptionsResult IgnoredBecauseAlreadyOpen { get; } = new(isCanceled: false, wasIgnoredBecauseAlreadyOpen: true, wasHandledInDialog: false, mode: null);

    public static ScreenshotOptionsResult HandledInDialog { get; } = new(isCanceled: false, wasIgnoredBecauseAlreadyOpen: false, wasHandledInDialog: true, mode: null);

    public ScreenshotOptionsResult(ScreenshotMode mode)
    {
        WasCanceled = false;
        Mode = mode;
    }

    private ScreenshotOptionsResult(bool isCanceled, bool wasIgnoredBecauseAlreadyOpen, bool wasHandledInDialog, ScreenshotMode? mode)
    {
        WasCanceled = isCanceled;
        WasIgnoredBecauseAlreadyOpen = wasIgnoredBecauseAlreadyOpen;
        WasHandledInDialog = wasHandledInDialog;
        Mode = mode;
    }

    public bool WasCanceled { get; }

    public bool WasIgnoredBecauseAlreadyOpen { get; }

    public bool WasHandledInDialog { get; }

    public ScreenshotMode? Mode { get; }
}
