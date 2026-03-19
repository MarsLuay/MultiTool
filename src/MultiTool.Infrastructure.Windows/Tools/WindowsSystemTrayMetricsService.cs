using System.Globalization;
using System.IO;
using System.Management;
using System.Reflection;
using MultiTool.Core.Models;
using MultiTool.Core.Services;

namespace MultiTool.Infrastructure.Windows.Tools;

public sealed class WindowsSystemTrayMetricsService : ISystemTrayMetricsService
{
    private readonly Func<int?> getCpuUsagePercent;
    private readonly Func<double?> getTemperatureCelsius;
    private readonly Func<int?> getMemoryUsagePercent;
    private readonly Func<int?> getDiskUsagePercent;

    public WindowsSystemTrayMetricsService()
        : this(TryGetCpuUsagePercent, TryGetTemperatureCelsius, TryGetMemoryUsagePercent, TryGetDiskUsagePercent)
    {
    }

    internal WindowsSystemTrayMetricsService(
        Func<int?> getCpuUsagePercent,
        Func<double?> getTemperatureCelsius,
        Func<int?> getMemoryUsagePercent,
        Func<int?> getDiskUsagePercent)
    {
        this.getCpuUsagePercent = getCpuUsagePercent;
        this.getTemperatureCelsius = getTemperatureCelsius;
        this.getMemoryUsagePercent = getMemoryUsagePercent;
        this.getDiskUsagePercent = getDiskUsagePercent;
    }

    public Task<SystemTrayMetricsSnapshot> CaptureAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var snapshot = new SystemTrayMetricsSnapshot(
            CpuUsagePercent: ClampPercent(getCpuUsagePercent()),
            TemperatureCelsius: getTemperatureCelsius(),
            MemoryUsagePercent: ClampPercent(getMemoryUsagePercent()),
            DiskUsagePercent: ClampPercent(getDiskUsagePercent()),
            CapturedAt: DateTimeOffset.Now);

        return Task.FromResult(snapshot);
    }

    private static int? TryGetCpuUsagePercent() =>
        ReadFormattedPercent(
            @"root\CIMV2",
            "SELECT PercentProcessorTime FROM Win32_PerfFormattedData_PerfOS_Processor WHERE Name = '_Total'",
            "PercentProcessorTime");

    private static int? TryGetMemoryUsagePercent()
    {
        const string query = "SELECT TotalVisibleMemorySize, FreePhysicalMemory FROM Win32_OperatingSystem";

        try
        {
            using var searcher = new ManagementObjectSearcher(@"root\CIMV2", query);
            using var results = searcher.Get();
            foreach (ManagementObject result in results)
            {
                var total = ConvertToUInt64(result["TotalVisibleMemorySize"]);
                var free = ConvertToUInt64(result["FreePhysicalMemory"]);
                if (total <= 0 || free > total)
                {
                    continue;
                }

                var usedPercent = (int)Math.Round(((total - free) * 100d) / total, MidpointRounding.AwayFromZero);
                return ClampPercent(usedPercent);
            }
        }
        catch
        {
        }

        return null;
    }

    private static int? TryGetDiskUsagePercent() =>
        ReadFormattedPercent(
            @"root\CIMV2",
            "SELECT PercentDiskTime FROM Win32_PerfFormattedData_PerfDisk_PhysicalDisk WHERE Name = '_Total'",
            "PercentDiskTime");

    private static double? TryGetTemperatureCelsius() =>
        TryGetLibreHardwareMonitorCpuTemperatureCelsius() ?? TryGetAcpiTemperatureCelsius();

    private static double? TryGetAcpiTemperatureCelsius()
    {
        const string query = "SELECT CurrentTemperature FROM MSAcpi_ThermalZoneTemperature";

        try
        {
            using var searcher = new ManagementObjectSearcher(@"root\WMI", query);
            using var results = searcher.Get();
            var readings = new List<double>();

            foreach (ManagementObject result in results)
            {
                var value = ConvertToDouble(result["CurrentTemperature"]);
                if (!value.HasValue)
                {
                    continue;
                }

                var celsius = (value.Value / 10d) - 273.15d;
                if (celsius is >= 0d and <= 120d)
                {
                    readings.Add(celsius);
                }
            }

            return readings.Count == 0
                ? null
                : Math.Round(readings.Average(), 1, MidpointRounding.AwayFromZero);
        }
        catch
        {
            return null;
        }
    }

    private static double? TryGetLibreHardwareMonitorCpuTemperatureCelsius()
    {
        var assemblyPath = TryResolveLibreHardwareMonitorAssemblyPath();
        if (string.IsNullOrWhiteSpace(assemblyPath))
        {
            return null;
        }

        try
        {
            var assembly = Assembly.LoadFrom(assemblyPath);
            var computerType = assembly.GetType("LibreHardwareMonitor.Hardware.Computer");
            var iHardwareType = assembly.GetType("LibreHardwareMonitor.Hardware.IHardware");
            var iSensorType = assembly.GetType("LibreHardwareMonitor.Hardware.ISensor");
            if (computerType is null || iHardwareType is null || iSensorType is null)
            {
                return null;
            }

            using var computer = Activator.CreateInstance(computerType) as IDisposable;
            if (computer is null)
            {
                return null;
            }

            SetPropertyIfExists(computerType, computer, "IsCpuEnabled", true);
            computerType.GetMethod("Open")?.Invoke(computer, null);

            var hardwareItems = computerType.GetProperty("Hardware")?.GetValue(computer) as System.Collections.IEnumerable;
            if (hardwareItems is null)
            {
                computerType.GetMethod("Close")?.Invoke(computer, null);
                return null;
            }

            var candidates = new List<(string Name, float Value)>();
            foreach (var hardware in hardwareItems)
            {
                CollectLibreHardwareMonitorCpuTemperatures(hardware, iHardwareType, iSensorType, candidates);
            }

            computerType.GetMethod("Close")?.Invoke(computer, null);
            return SelectPreferredCpuTemperature(candidates);
        }
        catch
        {
            return null;
        }
    }

    private static void CollectLibreHardwareMonitorCpuTemperatures(
        object hardware,
        Type iHardwareType,
        Type iSensorType,
        ICollection<(string Name, float Value)> candidates)
    {
        iHardwareType.GetMethod("Update")?.Invoke(hardware, null);

        var hardwareTypeName = Convert.ToString(iHardwareType.GetProperty("HardwareType")?.GetValue(hardware)) ?? string.Empty;
        if (string.Equals(hardwareTypeName, "Cpu", StringComparison.OrdinalIgnoreCase))
        {
            var sensorItems = iHardwareType.GetProperty("Sensors")?.GetValue(hardware) as System.Collections.IEnumerable;
            if (sensorItems is not null)
            {
                foreach (var sensor in sensorItems)
                {
                    var sensorTypeName = Convert.ToString(iSensorType.GetProperty("SensorType")?.GetValue(sensor)) ?? string.Empty;
                    if (!string.Equals(sensorTypeName, "Temperature", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var value = iSensorType.GetProperty("Value")?.GetValue(sensor);
                    if (value is null)
                    {
                        continue;
                    }

                    var reading = Convert.ToSingle(value, CultureInfo.InvariantCulture);
                    if (reading is < 0f or > 120f)
                    {
                        continue;
                    }

                    var sensorName = Convert.ToString(iSensorType.GetProperty("Name")?.GetValue(sensor)) ?? "Temperature";
                    candidates.Add((sensorName, reading));
                }
            }
        }

        var subHardwareItems = iHardwareType.GetProperty("SubHardware")?.GetValue(hardware) as System.Collections.IEnumerable;
        if (subHardwareItems is null)
        {
            return;
        }

        foreach (var child in subHardwareItems)
        {
            CollectLibreHardwareMonitorCpuTemperatures(child, iHardwareType, iSensorType, candidates);
        }
    }

    private static double? SelectPreferredCpuTemperature(IReadOnlyCollection<(string Name, float Value)> candidates)
    {
        if (candidates.Count == 0)
        {
            return null;
        }

        var preferredKeywords = new[]
        {
            "Package",
            "Tdie",
            "Tctl",
            "Core Average",
            "CPU",
        };

        foreach (var keyword in preferredKeywords)
        {
            var matchingValues = candidates
                .Where(candidate => candidate.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                .Select(candidate => (double)candidate.Value)
                .ToArray();
            if (matchingValues.Length > 0)
            {
                return Math.Round(matchingValues.Average(), 1, MidpointRounding.AwayFromZero);
            }
        }

        return Math.Round(candidates.Average(candidate => candidate.Value), 1, MidpointRounding.AwayFromZero);
    }

    private static string? TryResolveLibreHardwareMonitorAssemblyPath()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "LibreHardwareMonitorLib.dll"),
            Path.Combine(AppContext.BaseDirectory, "LibreHardwareMonitor.dll"),
        };

        return candidates.FirstOrDefault(File.Exists);
    }

    private static void SetPropertyIfExists(Type type, object instance, string propertyName, bool value)
    {
        var property = type.GetProperty(propertyName);
        if (property?.CanWrite == true)
        {
            property.SetValue(instance, value);
        }
    }

    private static int? ReadFormattedPercent(string scopePath, string query, string propertyName)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(scopePath, query);
            using var results = searcher.Get();
            foreach (ManagementObject result in results)
            {
                return ClampPercent(ConvertToInt32(result[propertyName]));
            }
        }
        catch
        {
        }

        return null;
    }

    private static int? ClampPercent(int? value)
    {
        if (!value.HasValue)
        {
            return null;
        }

        return Math.Clamp(value.Value, 0, 100);
    }

    private static int? ConvertToInt32(object? value)
    {
        try
        {
            return value is null
                ? null
                : Convert.ToInt32(value, CultureInfo.InvariantCulture);
        }
        catch
        {
            return null;
        }
    }

    private static ulong ConvertToUInt64(object? value)
    {
        try
        {
            return value is null
                ? 0UL
                : Convert.ToUInt64(value, CultureInfo.InvariantCulture);
        }
        catch
        {
            return 0UL;
        }
    }

    private static double? ConvertToDouble(object? value)
    {
        try
        {
            return value is null
                ? null
                : Convert.ToDouble(value, CultureInfo.InvariantCulture);
        }
        catch
        {
            return null;
        }
    }
}
