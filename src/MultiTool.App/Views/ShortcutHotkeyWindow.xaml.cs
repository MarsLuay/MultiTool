using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MultiTool.App.ViewModels;
using MultiTool.Core.Models;

namespace MultiTool.App.Views;

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

    private void ShortcutGrid_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not ShortcutHotkeyWindowViewModel viewModel || sender is not DataGrid dataGrid)
        {
            return;
        }

        viewModel.UpdateSelection(dataGrid.SelectedItems.OfType<ShortcutHotkeyInfo>().ToArray());
    }
}
