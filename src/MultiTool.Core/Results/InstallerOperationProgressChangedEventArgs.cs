using MultiTool.Core.Models;

namespace MultiTool.Core.Results;

public sealed class InstallerOperationProgressChangedEventArgs : EventArgs
{
    public InstallerOperationProgressChangedEventArgs(
        string packageId,
        string displayName,
        InstallerPackageAction action,
        string statusText,
        int? percent = null)
    {
        PackageId = packageId;
        DisplayName = displayName;
        Action = action;
        StatusText = statusText;
        Percent = percent;
    }

    public string PackageId { get; }

    public string DisplayName { get; }

    public InstallerPackageAction Action { get; }

    public string StatusText { get; }

    public int? Percent { get; }
}
