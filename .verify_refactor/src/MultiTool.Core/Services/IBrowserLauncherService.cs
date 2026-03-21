using MultiTool.Core.Models;

namespace MultiTool.Core.Services;

public interface IBrowserLauncherService
{
    BrowserLaunchResult OpenUrl(string url);
}
