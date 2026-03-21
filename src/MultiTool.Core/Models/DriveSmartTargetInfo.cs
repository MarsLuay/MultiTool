namespace MultiTool.Core.Models;

public sealed record DriveSmartTargetInfo(
    string DeviceId,
    string DisplayName,
    string Model,
    string Size,
    string InterfaceType,
    string MediaType,
    string FirmwareVersion,
    string SerialNumber,
    string PrimaryVolumeRootPath = "",
    string VolumePathsSummary = "")
{
    public string PickerLabel
    {
        get
        {
            var driveName = string.IsNullOrWhiteSpace(Size)
                ? FirstNonEmpty(Model, DisplayName, "Drive")
                : $"{FirstNonEmpty(Model, DisplayName, "Drive")} ({Size})";
            var location = FirstNonEmpty(VolumePathsSummary, NormalizeVolumeRoot(PrimaryVolumeRootPath));

            return string.IsNullOrWhiteSpace(location)
                ? driveName
                : $"{location} - {driveName}";
        }
    }

    public override string ToString() => PickerLabel;

    private static string FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(static value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? string.Empty;

    private static string NormalizeVolumeRoot(string? path) =>
        string.IsNullOrWhiteSpace(path)
            ? string.Empty
            : path.Trim().TrimEnd('\\').ToUpperInvariant();
}
