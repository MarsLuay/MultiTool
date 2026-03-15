using AutoClicker.Core.Models;

namespace AutoClicker.Core.Services;

public interface IBrowserLauncherService
{
    BrowserLaunchResult OpenUrl(string url);
}
