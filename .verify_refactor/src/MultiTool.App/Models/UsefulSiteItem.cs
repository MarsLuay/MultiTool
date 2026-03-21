namespace MultiTool.App.Models;

using MultiTool.App.Localization;

public sealed record UsefulSiteItem(
    string DisplayName,
    string Url,
    string Description)
{
    public bool OpensInTorBrowser =>
        Uri.TryCreate(Url, UriKind.Absolute, out var uri) &&
        uri.Host.EndsWith(".onion", StringComparison.OrdinalIgnoreCase);

    public string BrowserLabel => OpensInTorBrowser
        ? AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.UsefulSiteBrowserLabelTor)
        : AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.UsefulSiteBrowserLabelDefault);
}
