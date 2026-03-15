namespace AutoClicker.App.Models;

public sealed record UsefulSiteItem(
    string DisplayName,
    string Url,
    string Description)
{
    public bool OpensInTorBrowser =>
        Uri.TryCreate(Url, UriKind.Absolute, out var uri) &&
        uri.Host.EndsWith(".onion", StringComparison.OrdinalIgnoreCase);

    public string BrowserLabel => OpensInTorBrowser ? "Tor Browser" : "Default browser";
}
