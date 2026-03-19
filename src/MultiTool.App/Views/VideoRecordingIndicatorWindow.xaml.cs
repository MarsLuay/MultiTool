using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using MultiTool.App.Services;

namespace MultiTool.App.Views;

public partial class VideoRecordingIndicatorWindow : Window
{
    private const int GwlExStyle = -20;
    private const int WsExTransparent = 0x20;
    private const int WsExToolWindow = 0x80;
    private const int WsExNoActivate = 0x08000000;
    private const uint WdaExcludeFromCapture = 0x11;

    public VideoRecordingIndicatorWindow()
    {
        InitializeComponent();
        LoadIconImage();
    }

    public bool IsCaptureExcluded { get; private set; }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        var handle = new WindowInteropHelper(this).Handle;
        if (handle == nint.Zero)
        {
            Opacity = 0;
            Close();
            return;
        }

        ApplyExtendedWindowStyle(handle);
        IsCaptureExcluded = TryEnableCaptureExclusion(handle);

        if (!IsCaptureExcluded)
        {
            AppLog.Info("Video recording indicator was not shown because capture exclusion is unavailable.");
            Opacity = 0;
            Close();
            return;
        }

        Opacity = 1;
        StartBlinkAnimation();
    }

    private void LoadIconImage()
    {
        var iconPath = Path.Combine(AppContext.BaseDirectory, "Resources", "Icons", "icon_running.ico");
        if (!File.Exists(iconPath))
        {
            return;
        }

        try
        {
            IconImage.Source = BitmapFrame.Create(new Uri(iconPath, UriKind.Absolute));
        }
        catch
        {
        }
    }

    private void StartBlinkAnimation()
    {
        var animation = new DoubleAnimation(1d, 0.28d, new Duration(TimeSpan.FromMilliseconds(650)))
        {
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever,
        };

        BadgeBorder.BeginAnimation(OpacityProperty, animation);
    }

    private static void ApplyExtendedWindowStyle(nint handle)
    {
        var styles = GetWindowLongPtr(handle, GwlExStyle).ToInt64();
        styles |= WsExTransparent | WsExToolWindow | WsExNoActivate;
        _ = SetWindowLongPtr(handle, GwlExStyle, new nint(styles));
    }

    private static bool TryEnableCaptureExclusion(nint handle) =>
        SetWindowDisplayAffinity(handle, WdaExcludeFromCapture);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    private static extern nint GetWindowLongPtr(nint hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern nint SetWindowLongPtr(nint hWnd, int nIndex, nint dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowDisplayAffinity(nint hWnd, uint dwAffinity);
}
