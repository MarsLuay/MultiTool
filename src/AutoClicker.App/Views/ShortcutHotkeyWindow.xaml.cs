using System.Windows;
using AutoClicker.App.ViewModels;

namespace AutoClicker.App.Views;

public partial class ShortcutHotkeyWindow : Window
{
    public ShortcutHotkeyWindow(ShortcutHotkeyWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void CloseButton_OnClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
