using System.Reflection;
using System.Windows;

namespace AutoClicker.App.Views;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
        DataContext = new
        {
            VersionText = $"Version {Assembly.GetExecutingAssembly().GetName().Version}",
        };
    }

    private void CloseButton_OnClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
