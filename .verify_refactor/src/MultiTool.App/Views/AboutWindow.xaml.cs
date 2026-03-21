using System.Reflection;
using System.Windows;
using MultiTool.App.Localization;

namespace MultiTool.App.Views;

public partial class AboutWindow : Window
{
    public string AppNameText { get; }
    public string AboutSubtitleText { get; }
    public string VersionText { get; }
    public string CloseButtonText { get; }

    public AboutWindow()
    {
        Title = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.AboutWindowTitle);
        AppNameText = Assembly.GetExecutingAssembly().GetName().Name ?? "MultiTool";
        AboutSubtitleText = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.AboutSubtitle);
        var versionText = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown";
        VersionText = AppLanguageStrings.FormatForCurrentLanguage(
            AppLanguageKeys.AboutVersionFormat,
            versionText);
        CloseButtonText = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.AboutCloseButton);

        DataContext = this;
        InitializeComponent();
    }

    private void CloseButton_OnClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
