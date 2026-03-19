namespace MultiTool.Core.Models;

public sealed class ScreenshotSettings
{
    public const int DefaultCaptureVirtualKey = 0x6A;

    public const string DefaultCaptureDisplayName = "*";

    public HotkeyBinding CaptureHotkey { get; set; } = CreateDefaultCaptureBinding();

    public string SaveFolderPath { get; set; } = GetDefaultSaveFolderPath();

    public string FilePrefix { get; set; } = "Screenshot";

    public static HotkeyBinding CreateDefaultCaptureBinding() => new(DefaultCaptureVirtualKey, DefaultCaptureDisplayName);

    public static string GetLegacyDefaultSaveFolderPath() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        "Downloads");

    public static string GetDefaultSaveFolderPath() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        "Downloads",
        "Screenshots");

    public ScreenshotSettings Clone() =>
        new()
        {
            CaptureHotkey = CaptureHotkey.Clone(),
            SaveFolderPath = SaveFolderPath,
            FilePrefix = FilePrefix,
        };
}
