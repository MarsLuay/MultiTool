using System.Management;
using AutoClicker.Core.Models;
using AutoClicker.Core.Services;

namespace AutoClicker.Infrastructure.Windows.Tools;

public delegate HardwareInventoryReport HardwareInventoryReader();

public sealed class WindowsHardwareInventoryService : IHardwareInventoryService
{
    private readonly HardwareInventoryReader reportReader;

    public WindowsHardwareInventoryService()
        : this(BuildReport)
    {
    }

    public WindowsHardwareInventoryService(HardwareInventoryReader reportReader)
    {
        this.reportReader = reportReader;
    }

    public Task<HardwareInventoryReport> GetReportAsync(CancellationToken cancellationToken = default) =>
        Task.Run(() => reportReader(), cancellationToken);

    private static HardwareInventoryReport BuildReport()
    {
        var warnings = new List<string>();

        var systemInfo = TryReadSingle(
            "SELECT Name, Manufacturer, Model, TotalPhysicalMemory FROM Win32_ComputerSystem",
            item => new
            {
                Name = GetString(item, "Name", Environment.MachineName),
                Manufacturer = GetString(item, "Manufacturer", "Unknown manufacturer"),
                Model = GetString(item, "Model", "Unknown model"),
                TotalPhysicalMemory = GetUInt64(item, "TotalPhysicalMemory"),
            },
            new
            {
                Name = Environment.MachineName,
                Manufacturer = "Unknown manufacturer",
                Model = "Unknown model",
                TotalPhysicalMemory = 0UL,
            },
            warnings,
            "Computer system");

        var processorSummary = BuildProcessorSummary(warnings);
        var memorySummary = BuildMemorySummary(systemInfo.TotalPhysicalMemory, warnings);
        var operatingSystemSummary = BuildOperatingSystemSummary(warnings);
        var motherboardSummary = BuildMotherboardSummary(warnings);
        var biosSummary = BuildBiosSummary(warnings);
        var graphicsAdapters = BuildGraphicsAdapters(warnings);
        var storageDrives = BuildStorageDrives(warnings);

        return new HardwareInventoryReport(
            $"{systemInfo.Name}  |  {systemInfo.Manufacturer} {systemInfo.Model}".Trim(),
            operatingSystemSummary,
            processorSummary,
            memorySummary,
            motherboardSummary,
            biosSummary,
            graphicsAdapters,
            storageDrives,
            warnings);
    }

    private static string BuildProcessorSummary(ICollection<string> warnings)
    {
        var processors = TryReadMany(
            "SELECT Name, NumberOfCores, NumberOfLogicalProcessors FROM Win32_Processor",
            item => new
            {
                Name = GetString(item, "Name", "Unknown CPU"),
                Cores = GetInt(item, "NumberOfCores"),
                Threads = GetInt(item, "NumberOfLogicalProcessors"),
            },
            warnings,
            "Processor");

        if (processors.Count == 0)
        {
            return "Processor details unavailable.";
        }

        return string.Join(
            "  |  ",
            processors.Select(
                processor =>
                {
                    var topology = processor.Cores > 0 || processor.Threads > 0
                        ? $" ({processor.Cores} cores / {processor.Threads} threads)"
                        : string.Empty;
                    return $"{processor.Name}{topology}";
                }));
    }

    private static string BuildMemorySummary(ulong totalPhysicalMemory, ICollection<string> warnings)
    {
        var modules = TryReadMany(
            "SELECT Capacity, Speed FROM Win32_PhysicalMemory WHERE Capacity IS NOT NULL",
            item => new
            {
                Capacity = GetUInt64(item, "Capacity"),
                Speed = GetInt(item, "Speed"),
            },
            warnings,
            "Physical memory");

        var total = totalPhysicalMemory > 0
            ? totalPhysicalMemory
            : modules.Aggregate(0UL, (sum, module) => sum + module.Capacity);

        if (total == 0 && modules.Count == 0)
        {
            return "Memory details unavailable.";
        }

        var speed = modules
            .Select(static module => module.Speed)
            .Where(static speed => speed > 0)
            .DefaultIfEmpty()
            .Max();

        var speedText = speed > 0 ? $" at up to {speed} MHz" : string.Empty;
        return $"{FormatBytes(total)} installed across {modules.Count} module{(modules.Count == 1 ? string.Empty : "s")}{speedText}.";
    }

    private static string BuildOperatingSystemSummary(ICollection<string> warnings)
    {
        var os = TryReadSingle(
            "SELECT Caption, Version, BuildNumber FROM Win32_OperatingSystem",
            item => new
            {
                Caption = GetString(item, "Caption", "Windows"),
                Version = GetString(item, "Version"),
                BuildNumber = GetString(item, "BuildNumber"),
            },
            new
            {
                Caption = "Windows",
                Version = string.Empty,
                BuildNumber = string.Empty,
            },
            warnings,
            "Operating system");

        var details = string.Join(
            "  |  ",
            new[]
            {
                os.Caption,
                string.IsNullOrWhiteSpace(os.Version) ? string.Empty : $"Version {os.Version}",
                string.IsNullOrWhiteSpace(os.BuildNumber) ? string.Empty : $"Build {os.BuildNumber}",
            }.Where(static value => !string.IsNullOrWhiteSpace(value)));

        return string.IsNullOrWhiteSpace(details)
            ? "Windows details unavailable."
            : details;
    }

    private static string BuildMotherboardSummary(ICollection<string> warnings)
    {
        var board = TryReadSingle(
            "SELECT Manufacturer, Product FROM Win32_BaseBoard",
            item => new
            {
                Manufacturer = GetString(item, "Manufacturer", "Unknown manufacturer"),
                Product = GetString(item, "Product", "Unknown board"),
            },
            new
            {
                Manufacturer = "Unknown manufacturer",
                Product = "Unknown board",
            },
            warnings,
            "Motherboard");

        return $"{board.Manufacturer} {board.Product}".Trim();
    }

    private static string BuildBiosSummary(ICollection<string> warnings)
    {
        var bios = TryReadSingle(
            "SELECT SMBIOSBIOSVersion, ReleaseDate FROM Win32_BIOS",
            item => new
            {
                Version = GetString(item, "SMBIOSBIOSVersion", "Unknown BIOS"),
                ReleaseDate = GetDateString(item, "ReleaseDate"),
            },
            new
            {
                Version = "Unknown BIOS",
                ReleaseDate = string.Empty,
            },
            warnings,
            "BIOS");

        return string.IsNullOrWhiteSpace(bios.ReleaseDate)
            ? bios.Version
            : $"{bios.Version}  |  Released {bios.ReleaseDate}";
    }

    private static IReadOnlyList<HardwareDisplayAdapterInfo> BuildGraphicsAdapters(ICollection<string> warnings)
    {
        var adapters = TryReadMany(
            "SELECT Name, DriverVersion, AdapterRAM FROM Win32_VideoController WHERE Name IS NOT NULL",
            item => new HardwareDisplayAdapterInfo(
                GetString(item, "Name", "Unknown GPU"),
                GetString(item, "DriverVersion", "Unknown driver"),
                FormatBytes(GetUInt64(item, "AdapterRAM"))),
            warnings,
            "Graphics adapter");

        return adapters.Count == 0
            ? [new HardwareDisplayAdapterInfo("No graphics adapters detected.", string.Empty, string.Empty)]
            : adapters;
    }

    private static IReadOnlyList<HardwareStorageDriveInfo> BuildStorageDrives(ICollection<string> warnings)
    {
        var drives = TryReadMany(
            "SELECT Model, Size, InterfaceType, MediaType FROM Win32_DiskDrive WHERE Model IS NOT NULL",
            item => new HardwareStorageDriveInfo(
                GetString(item, "Model", "Unknown drive"),
                FormatBytes(GetUInt64(item, "Size")),
                GetString(item, "InterfaceType", "Unknown interface"),
                GetString(item, "MediaType", "Unknown media")),
            warnings,
            "Storage drive");

        return drives.Count == 0
            ? [new HardwareStorageDriveInfo("No storage drives detected.", string.Empty, string.Empty, string.Empty)]
            : drives;
    }

    private static T TryReadSingle<T>(string query, Func<ManagementObject, T> selector, T fallback, ICollection<string> warnings, string context)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(query);
            foreach (ManagementObject item in searcher.Get())
            {
                return selector(item);
            }
        }
        catch (Exception ex)
        {
            warnings.Add($"{context}: {ex.Message}");
        }

        return fallback;
    }

    private static IReadOnlyList<T> TryReadMany<T>(string query, Func<ManagementObject, T> selector, ICollection<string> warnings, string context)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(query);
            return
            [
                .. searcher.Get().Cast<ManagementObject>().Select(selector),
            ];
        }
        catch (Exception ex)
        {
            warnings.Add($"{context}: {ex.Message}");
            return [];
        }
    }

    private static string GetString(ManagementBaseObject source, string propertyName, string fallback = "") =>
        Convert.ToString(source[propertyName])?.Trim() ?? fallback;

    private static int GetInt(ManagementBaseObject source, string propertyName)
    {
        var value = source[propertyName];
        return value is null ? 0 : Convert.ToInt32(value);
    }

    private static ulong GetUInt64(ManagementBaseObject source, string propertyName)
    {
        var value = source[propertyName];
        return value is null ? 0UL : Convert.ToUInt64(value);
    }

    private static string GetDateString(ManagementBaseObject source, string propertyName)
    {
        var rawValue = Convert.ToString(source[propertyName])?.Trim();
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return string.Empty;
        }

        try
        {
            return ManagementDateTimeConverter.ToDateTime(rawValue).ToString("yyyy-MM-dd");
        }
        catch
        {
            return rawValue;
        }
    }

    private static string FormatBytes(ulong bytes)
    {
        if (bytes == 0)
        {
            return string.Empty;
        }

        string[] suffixes = ["B", "KB", "MB", "GB", "TB", "PB"];
        var value = bytes;
        var index = 0;
        decimal scaled = value;
        while (scaled >= 1024 && index < suffixes.Length - 1)
        {
            scaled /= 1024;
            index++;
        }

        return $"{scaled:0.#} {suffixes[index]}";
    }
}
