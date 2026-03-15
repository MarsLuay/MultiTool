using AutoClicker.App.Views;

namespace AutoClicker.App.Services;

public sealed class AboutWindowService : IAboutWindowService
{
    public void Show()
    {
        var window = new AboutWindow();
        if (System.Windows.Application.Current?.MainWindow is System.Windows.Window owner)
        {
            window.Owner = owner;
        }

        window.ShowDialog();
    }
}
