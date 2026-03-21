namespace MultiTool.Core.Models;

public sealed record StorageBenchmarkTargetInfo(
    string TargetId,
    string DisplayName,
    string Model,
    string Size,
    string InterfaceType,
    string MediaType,
    string FirmwareVersion,
    string VolumeRootPath,
    string VolumeLabel,
    string FileSystem,
    string FreeSpace)
{
    public string PickerLabel
    {
        get
        {
            var volume = NormalizeVolumeRoot(VolumeRootPath);
            var location = string.IsNullOrWhiteSpace(VolumeLabel) || VolumeLabel.Equals(volume, StringComparison.OrdinalIgnoreCase)
                ? volume
                : $"{volume} ({VolumeLabel})";
            var driveName = string.IsNullOrWhiteSpace(Size)
                ? FirstNonEmpty(Model, DisplayName, "SSD")
                : $"{FirstNonEmpty(Model, DisplayName, "SSD")} ({Size})";

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
