namespace AutoClicker.Core.Models;

public sealed class InstallerSettings
{
    public List<string> SelectedPackageIds { get; set; } = [];

    public List<string> SelectedCleanupPackageIds { get; set; } = [];

    public List<InstallerPackageOptionSelection> PackageOptions { get; set; } = [];

    public InstallerSettings Clone() =>
        new()
        {
            SelectedPackageIds =
            [
                .. SelectedPackageIds
                    .Where(static packageId => !string.IsNullOrWhiteSpace(packageId))
                    .Distinct(StringComparer.OrdinalIgnoreCase),
            ],
            SelectedCleanupPackageIds =
            [
                .. SelectedCleanupPackageIds
                    .Where(static packageId => !string.IsNullOrWhiteSpace(packageId))
                    .Distinct(StringComparer.OrdinalIgnoreCase),
            ],
            PackageOptions =
            [
                .. PackageOptions
                    .Where(static option => option is not null && !string.IsNullOrWhiteSpace(option.PackageId))
                    .Select(static option => option.Clone())
                    .Where(static option => option.SelectedOptionIds.Count > 0)
                    .GroupBy(static option => option.PackageId, StringComparer.OrdinalIgnoreCase)
                    .Select(
                        static group => new InstallerPackageOptionSelection
                        {
                            PackageId = group.First().PackageId,
                            SelectedOptionIds =
                            [
                                .. group.SelectMany(static option => option.SelectedOptionIds)
                                    .Where(static optionId => !string.IsNullOrWhiteSpace(optionId))
                                    .Distinct(StringComparer.OrdinalIgnoreCase),
                            ],
                        }),
            ],
        };
}
