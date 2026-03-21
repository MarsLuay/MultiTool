using MultiTool.App.Views;

namespace MultiTool.App.Services;

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
