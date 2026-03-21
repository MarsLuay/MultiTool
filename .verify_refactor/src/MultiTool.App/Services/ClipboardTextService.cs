namespace MultiTool.App.Services;

public sealed class ClipboardTextService : IClipboardTextService
{
    public void SetText(string text)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() => System.Windows.Clipboard.SetText(text ?? string.Empty));
    }
}
