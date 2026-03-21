using System.Windows;
using System.Windows.Input;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media;
using MultiTool.App.ViewModels;
using MultiTool.Core.Models;
using Point = System.Windows.Point;
using Rect = System.Windows.Rect;

namespace MultiTool.App.Views;

public partial class MacroEditorWindow : Window
{
    private const double ScreenInset = 24d;
    private readonly MacroEditorViewModel viewModel;

    public MacroEditorWindow(MacroEditorViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        this.viewModel = viewModel;
        Opacity = 0;
        Loaded += MacroEditorWindow_Loaded;
        ContentRendered += MacroEditorWindow_ContentRendered;
    }

    public RecordedMacro? EditedMacro { get; private set; }

    private void MacroEditorWindow_Loaded(object sender, RoutedEventArgs e)
    {
        Keyboard.Focus(MacroNameTextBox);
        MacroNameTextBox.SelectAll();
    }

    private void MacroEditorWindow_ContentRendered(object? sender, EventArgs e)
    {
        EnsureFullyVisible();
        if (Opacity < 1)
        {
            Opacity = 1;
        }
    }

    private void SaveButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (!viewModel.TryBuildMacro(out var macro, out var errorMessage))
        {
            viewModel.StatusMessage = errorMessage;
            return;
        }

        EditedMacro = macro;
        DialogResult = true;
        Close();
    }

    private void CancelButton_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void EnsureFullyVisible()
    {
        UpdateLayout();

        var workArea = GetCurrentWorkArea();
        var safeArea = InsetRect(workArea, ScreenInset);
        var desiredWidth = ActualWidth > 0 ? ActualWidth : Width;
        var desiredHeight = ActualHeight > 0 ? ActualHeight : Height;

        if (desiredWidth > safeArea.Width)
        {
            Width = Math.Max(MinWidth, safeArea.Width);
            desiredWidth = Width;
        }

        if (desiredHeight > safeArea.Height)
        {
            Height = Math.Max(MinHeight, safeArea.Height);
            desiredHeight = Height;
        }

        var maxLeft = Math.Max(safeArea.Left, safeArea.Right - desiredWidth);
        var maxTop = Math.Max(safeArea.Top, safeArea.Bottom - desiredHeight);

        Left = Math.Clamp(Left, safeArea.Left, maxLeft);
        Top = Math.Clamp(Top, safeArea.Top, maxTop);
    }

    private Rect GetCurrentWorkArea()
    {
        var handle = new WindowInteropHelper(this).Handle;
        Screen screen;

        if (Owner is { IsVisible: true })
        {
            var ownerHandle = new WindowInteropHelper(Owner).Handle;
            screen = ownerHandle != IntPtr.Zero
                ? Screen.FromHandle(ownerHandle)
                : Screen.FromPoint(System.Windows.Forms.Cursor.Position);
        }
        else
        {
            screen = handle != IntPtr.Zero
                ? Screen.FromHandle(handle)
                : Screen.FromPoint(System.Windows.Forms.Cursor.Position);
        }

        return ConvertToWpfRect(screen.WorkingArea);
    }

    private Rect ConvertToWpfRect(System.Drawing.Rectangle bounds)
    {
        var source = PresentationSource.FromVisual(this);
        if (source?.CompositionTarget is not null)
        {
            var transform = source.CompositionTarget.TransformFromDevice;
            var topLeft = transform.Transform(new Point(bounds.Left, bounds.Top));
            var bottomRight = transform.Transform(new Point(bounds.Right, bounds.Bottom));
            return new Rect(topLeft, bottomRight);
        }

        return new Rect(
            SystemParameters.WorkArea.Left,
            SystemParameters.WorkArea.Top,
            SystemParameters.WorkArea.Width,
            SystemParameters.WorkArea.Height);
    }

    private static Rect InsetRect(Rect rect, double inset)
    {
        var horizontalInset = Math.Min(inset, Math.Max(0, (rect.Width - 120d) / 2d));
        var verticalInset = Math.Min(inset, Math.Max(0, (rect.Height - 120d) / 2d));

        return new Rect(
            rect.Left + horizontalInset,
            rect.Top + verticalInset,
            Math.Max(120d, rect.Width - (horizontalInset * 2d)),
            Math.Max(120d, rect.Height - (verticalInset * 2d)));
    }
}
