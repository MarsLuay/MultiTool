using System.Windows;
using AutoClicker.App.Localization;
using AutoClicker.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AutoClicker.App.Views;

public partial class CoordinateCaptureWindow : Window
{
    private readonly OverlayViewModel viewModel = new();

    public CoordinateCaptureWindow()
    {
        InitializeComponent();
        DataContext = viewModel;

        CaptureInstructionTextBlock.Text = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.CoordinateCaptureInstruction);
        CaptureCancelHintTextBlock.Text = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.CoordinateCaptureEscHint);
        viewModel.CoordinateText = AppLanguageStrings.FormatForCurrentLanguage(AppLanguageKeys.CoordinateCapturePositionFormat, 0, 0);

        Left = SystemParameters.VirtualScreenLeft;
        Top = SystemParameters.VirtualScreenTop;
        Width = SystemParameters.VirtualScreenWidth;
        Height = SystemParameters.VirtualScreenHeight;
        WindowStartupLocation = WindowStartupLocation.Manual;
    }

    public ScreenPoint? CapturedPoint { get; private set; }

    protected override void OnMouseMove(System.Windows.Input.MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var point = System.Windows.Forms.Cursor.Position;
        viewModel.CoordinateText = AppLanguageStrings.FormatForCurrentLanguage(
            AppLanguageKeys.CoordinateCapturePositionFormat,
            point.X,
            point.Y);
    }

    protected override void OnMouseDown(System.Windows.Input.MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);
        var point = System.Windows.Forms.Cursor.Position;
        CapturedPoint = new ScreenPoint(point.X, point.Y);
        DialogResult = true;
        Close();
    }

    protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key != System.Windows.Input.Key.Escape)
        {
            return;
        }

        DialogResult = false;
        Close();
    }

    private sealed partial class OverlayViewModel : ObservableObject
    {
        [ObservableProperty]
        private string coordinateText = string.Empty;
    }
}
