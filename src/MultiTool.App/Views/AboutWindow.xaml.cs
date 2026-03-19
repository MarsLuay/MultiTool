using System.Reflection;
using System.Windows;
using MultiTool.App.Localization;

namespace MultiTool.App.Views;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();

        Title = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.AboutWindowTitle);
        AboutSubtitleTextBlock.Text = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.AboutSubtitle);
        var versionText = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown";
        VersionTextBlock.Text = AppLanguageStrings.FormatForCurrentLanguage(
            AppLanguageKeys.AboutVersionFormat,
            versionText);
        CloseButton.Content = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.AboutCloseButton);
    }

    private void CloseButton_OnClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
