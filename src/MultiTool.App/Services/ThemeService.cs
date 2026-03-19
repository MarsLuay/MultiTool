using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace MultiTool.App.Services;

public sealed class ThemeService : IThemeService
{
    private const int HwndBroadcast = 0xFFFF;
    private const int DwmColorNone = unchecked((int)0xFFFFFFFE);
    private const int DwmwaUseImmersiveDarkMode = 20;
    private const int DwmwaUseImmersiveDarkModeBefore20 = 19;
    private const int DwmwaBorderColor = 34;
    private const int DwmwaCaptionColor = 35;
    private const int DwmwaTextColor = 36;
    private const uint WmSettingChange = 0x001A;
    private const uint SmtoAbortIfHung = 0x0002;
    private bool isDarkMode;

    public bool GetSystemPrefersDarkMode()
    {
        try
        {
            using var personalizeKey = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var lightThemeValue = personalizeKey?.GetValue("AppsUseLightTheme");
            return lightThemeValue is int value && value == 0;
        }
        catch
        {
            return false;
        }
    }

    public void ApplyTheme(bool isDarkMode)
    {
        this.isDarkMode = isDarkMode;
        var resources = System.Windows.Application.Current.Resources;

        SetBrush(resources, "WindowBackgroundBrush", isDarkMode ? "#FF15171C" : "#FFF4F5F7");
        SetBrush(resources, "SurfaceBrush", isDarkMode ? "#FF1E2128" : "#FFFFFFFF");
        SetBrush(resources, "SurfaceAltBrush", isDarkMode ? "#FF262A33" : "#FFF7F7F7");
        SetBrush(resources, "ControlBackgroundBrush", isDarkMode ? "#FF2A2F39" : "#FFFFFFFF");
        SetBrush(resources, "ControlHoverBrush", isDarkMode ? "#FF343946" : "#FFEFF2F6");
        SetBrush(resources, "BorderBrush", isDarkMode ? "#FF4A5362" : "#FFB8BDC7");
        SetBrush(resources, "TextBrush", isDarkMode ? "#FFF5F7FA" : "#FF111111");
        SetBrush(resources, "MutedTextBrush", isDarkMode ? "#FFB6BECA" : "#FF666666");
        SetBrush(resources, "AccentBrush", isDarkMode ? "#FFFFC857" : "#FF0D6EFD");
        SetBrush(resources, "AccentTextBrush", isDarkMode ? "#FF1A1D24" : "#FFFFFFFF");
        SetBrush(resources, "SelectionBackgroundBrush", isDarkMode ? "#FF3D4C61" : "#FF0D6EFD");
        SetBrush(resources, "SelectionTextBrush", "#FFFFFFFF");
        SetBrush(resources, "DisabledBackgroundBrush", isDarkMode ? "#FF4A4A4A" : "#FFE1E4E8");
        SetBrush(resources, "DisabledTextBrush", isDarkMode ? "#FFD3D3D3" : "#FF8A8A8A");
        SetBrush(resources, "InputActiveBackgroundBrush", isDarkMode ? "#FF23466A" : "#FF1F4D78");
        SetBrush(resources, "InputActiveBorderBrush", isDarkMode ? "#FF9FD1FF" : "#FF6CB4FF");
        SetBrush(resources, "InputActiveTextBrush", "#FFFFFFFF");
        SetBrush(resources, "ScrollBarTrackBrush", isDarkMode ? "#FF20242D" : "#FFE5E8ED");
        SetBrush(resources, "ScrollBarThumbBrush", isDarkMode ? "#FF5E687A" : "#FF9AA3B2");
        SetBrush(resources, "ScrollBarThumbHoverBrush", isDarkMode ? "#FF76839A" : "#FF7E889B");

        foreach (System.Windows.Window window in System.Windows.Application.Current.Windows)
        {
            ApplyWindowChromeTheme(window, isDarkMode);
        }
    }

    public void ApplyThemeToWindow(System.Windows.Window window)
    {
        ApplyWindowChromeTheme(window, isDarkMode);
    }

    public bool TryApplySystemDarkModePreference(out string message)
    {
        try
        {
            using var personalizeKey = Registry.CurrentUser.CreateSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                writable: true)
                ?? throw new InvalidOperationException("Windows Personalize settings are unavailable.");

            personalizeKey.SetValue("AppsUseLightTheme", 0, RegistryValueKind.DWord);
            personalizeKey.SetValue("SystemUsesLightTheme", 0, RegistryValueKind.DWord);

            _ = SendMessageTimeout(
                new nint(HwndBroadcast),
                WmSettingChange,
                nuint.Zero,
                "ImmersiveColorSet",
                SmtoAbortIfHung,
                250,
                out _);

            message = "Applied Windows dark mode preference for system UI and supported apps. Some apps may still need a restart or their own in-app theme change.";
            return true;
        }
        catch (Exception ex)
        {
            message = $"Unable to apply Windows dark mode preference: {ex.Message}";
            return false;
        }
    }

    private static void SetBrush(System.Windows.ResourceDictionary resources, string key, string colorValue)
    {
        var brush = new System.Windows.Media.SolidColorBrush(
            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(colorValue)!);
        brush.Freeze();
        resources[key] = brush;
    }

    private static void ApplyWindowChromeTheme(System.Windows.Window window, bool isDarkMode)
    {
        var handle = new System.Windows.Interop.WindowInteropHelper(window).Handle;
        if (handle == nint.Zero)
        {
            return;
        }

        var darkModeValue = isDarkMode ? 1 : 0;
        _ = DwmSetWindowAttribute(handle, DwmwaUseImmersiveDarkMode, ref darkModeValue, sizeof(int));
        _ = DwmSetWindowAttribute(handle, DwmwaUseImmersiveDarkModeBefore20, ref darkModeValue, sizeof(int));

        var captionColor = isDarkMode ? ToColorRef(0x1E, 0x21, 0x28) : ToColorRef(0xF4, 0xF5, 0xF7);
        var borderColor = DwmColorNone;
        var textColor = isDarkMode ? ToColorRef(0xF5, 0xF7, 0xFA) : ToColorRef(0x11, 0x11, 0x11);

        _ = DwmSetWindowAttribute(handle, DwmwaCaptionColor, ref captionColor, sizeof(int));
        _ = DwmSetWindowAttribute(handle, DwmwaBorderColor, ref borderColor, sizeof(int));
        _ = DwmSetWindowAttribute(handle, DwmwaTextColor, ref textColor, sizeof(int));
    }

    private static int ToColorRef(byte red, byte green, byte blue) => red | (green << 8) | (blue << 16);

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(nint hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint SendMessageTimeout(
        nint hWnd,
        uint msg,
        nuint wParam,
        string lParam,
        uint fuFlags,
        uint uTimeout,
        out nuint lpdwResult);
}
