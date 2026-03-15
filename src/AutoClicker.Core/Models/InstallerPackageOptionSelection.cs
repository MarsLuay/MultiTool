namespace AutoClicker.Core.Models;

public sealed class InstallerPackageOptionSelection
{
    public string PackageId { get; set; } = string.Empty;

    public List<string> SelectedOptionIds { get; set; } = [];

    public InstallerPackageOptionSelection Clone() =>
        new()
        {
            PackageId = PackageId.Trim(),
            SelectedOptionIds =
            [
                .. SelectedOptionIds
                    .Where(static optionId => !string.IsNullOrWhiteSpace(optionId))
                    .Select(static optionId => optionId.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase),
            ],
        };
}
