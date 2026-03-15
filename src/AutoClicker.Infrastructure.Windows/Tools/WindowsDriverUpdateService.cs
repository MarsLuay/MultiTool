using System.Management;
using AutoClicker.Core.Models;
using AutoClicker.Core.Services;

namespace AutoClicker.Infrastructure.Windows.Tools;

public delegate IReadOnlyList<DriverHardwareInfo> DriverHardwareInventoryReader();

public delegate IReadOnlyList<DriverUpdateCandidate> DriverUpdateSearcher();

public delegate IReadOnlyList<DriverUpdateInstallResult> DriverUpdateInstaller(IReadOnlyCollection<string> updateIds);

public sealed class WindowsDriverUpdateService : IDriverUpdateService
{
    private const string RecommendedDriverCriteria = "IsInstalled=0 and Type='Driver' and IsHidden=0 and BrowseOnly=0";
    private const string OptionalDriverCriteria = "IsInstalled=0 and Type='Driver' and IsHidden=0 and BrowseOnly=1";

    private readonly DriverHardwareInventoryReader hardwareInventoryReader;
    private readonly DriverUpdateSearcher driverUpdateSearcher;
    private readonly DriverUpdateInstaller driverUpdateInstaller;

    public WindowsDriverUpdateService()
        : this(ReadHardwareInventory, SearchAvailableDriverUpdates, InstallDriverUpdates)
    {
    }

    public WindowsDriverUpdateService(
        DriverHardwareInventoryReader hardwareInventoryReader,
        DriverUpdateSearcher driverUpdateSearcher,
        DriverUpdateInstaller driverUpdateInstaller)
    {
        this.hardwareInventoryReader = hardwareInventoryReader;
        this.driverUpdateSearcher = driverUpdateSearcher;
        this.driverUpdateInstaller = driverUpdateInstaller;
    }

    public async Task<DriverUpdateScanResult> ScanAsync(CancellationToken cancellationToken = default)
    {
        var warnings = new List<string>();
        IReadOnlyList<DriverHardwareInfo> hardware = [];
        IReadOnlyList<DriverUpdateCandidate> updates = [];

        try
        {
            hardware = await RunStaAsync<IReadOnlyList<DriverHardwareInfo>>(() => hardwareInventoryReader(), cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            warnings.Add($"Hardware inventory skipped: {ex.Message}");
        }

        try
        {
            updates = await RunStaAsync<IReadOnlyList<DriverUpdateCandidate>>(() => driverUpdateSearcher(), cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            warnings.Add($"Windows Update driver scan failed: {ex.Message}");
        }

        return new DriverUpdateScanResult(hardware, updates, warnings);
    }

    public async Task<IReadOnlyList<DriverUpdateInstallResult>> InstallAsync(IEnumerable<string> updateIds, CancellationToken cancellationToken = default)
    {
        var normalizedIds = updateIds
            .Where(static id => !string.IsNullOrWhiteSpace(id))
            .Select(static id => id.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (normalizedIds.Length == 0)
        {
            return [];
        }

        try
        {
            return await RunStaAsync(() => driverUpdateInstaller(normalizedIds), cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return
            [
                .. normalizedIds.Select(
                    updateId => new DriverUpdateInstallResult(
                        updateId,
                        updateId,
                        false,
                        false,
                        false,
                        $"Driver install failed: {ex.Message}")),
            ];
        }
    }

    private static IReadOnlyList<DriverHardwareInfo> ReadHardwareInventory()
    {
        using var searcher = new ManagementObjectSearcher(
            "SELECT DeviceID, DeviceName, Manufacturer, DriverProviderName, DriverVersion, DeviceClass FROM Win32_PnPSignedDriver WHERE DeviceName IS NOT NULL");

        var results = new List<DriverHardwareInfo>();
        foreach (ManagementObject driver in searcher.Get())
        {
            results.Add(
                new DriverHardwareInfo(
                    GetManagementString(driver, "DeviceName", "Unknown device"),
                    GetManagementString(driver, "Manufacturer"),
                    GetManagementString(driver, "DriverProviderName"),
                    GetManagementString(driver, "DriverVersion"),
                    GetManagementString(driver, "DeviceClass"),
                    GetManagementString(driver, "DeviceID")));
        }

        return
        [
            .. results
                .OrderBy(static item => item.DeviceName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static item => item.Manufacturer, StringComparer.OrdinalIgnoreCase),
        ];
    }

    private static IReadOnlyList<DriverUpdateCandidate> SearchAvailableDriverUpdates()
    {
        var discoveredUpdates = new Dictionary<string, DriverUpdateCandidate>(StringComparer.OrdinalIgnoreCase);
        dynamic searcher = CreateUpdateSearcher();

        AddSearchResults(discoveredUpdates, searcher, RecommendedDriverCriteria, isOptional: false);
        AddSearchResults(discoveredUpdates, searcher, OptionalDriverCriteria, isOptional: true);

        return
        [
            .. discoveredUpdates.Values
                .OrderBy(static update => update.IsOptional)
                .ThenBy(static update => update.DriverManufacturer, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static update => update.DriverModel, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static update => update.Title, StringComparer.OrdinalIgnoreCase),
        ];
    }

    private static IReadOnlyList<DriverUpdateInstallResult> InstallDriverUpdates(IReadOnlyCollection<string> updateIds)
    {
        dynamic session = CreateUpdateSession();
        dynamic searcher = session.CreateUpdateSearcher();
        searcher.Online = true;

        Dictionary<string, AvailableDriverUpdateEntry> availableUpdates = GetAvailableUpdatesById(searcher);
        var selectedEntries = updateIds
            .Where(availableUpdates.ContainsKey)
            .Select(updateId => availableUpdates[updateId])
            .ToArray();

        var missingIds = updateIds
            .Where(updateId => !availableUpdates.ContainsKey(updateId))
            .ToArray();

        if (selectedEntries.Length == 0)
        {
            return
            [
                .. missingIds.Select(
                    updateId => new DriverUpdateInstallResult(
                        updateId,
                        updateId,
                        false,
                        false,
                        false,
                        "This driver update is no longer available from Windows Update.")),
            ];
        }

        dynamic updateCollection = CreateUpdateCollection();
        foreach (var entry in selectedEntries)
        {
            TryAcceptEula(entry.Update);
            updateCollection.Add(entry.Update);
        }

        dynamic downloader = session.CreateUpdateDownloader();
        downloader.Updates = updateCollection;
        dynamic downloadResult = downloader.Download();
        var downloadCode = Convert.ToInt32(downloadResult.ResultCode);
        if (downloadCode is 4 or 5)
        {
            return
            [
                .. selectedEntries.Select(
                    static entry => new DriverUpdateInstallResult(
                        entry.UpdateId,
                        entry.Title,
                        false,
                        false,
                        false,
                        "Windows Update could not download this driver.")),
                .. missingIds.Select(
                    updateId => new DriverUpdateInstallResult(
                        updateId,
                        updateId,
                        false,
                        false,
                        false,
                        "This driver update is no longer available from Windows Update.")),
            ];
        }

        dynamic installer = session.CreateUpdateInstaller();
        installer.Updates = updateCollection;
        dynamic installationResult = installer.Install();

        var results = new List<DriverUpdateInstallResult>(selectedEntries.Length + missingIds.Length);
        for (var index = 0; index < selectedEntries.Length; index++)
        {
            var entry = selectedEntries[index];
            dynamic updateResult = installationResult.GetUpdateResult(index);
            var resultCode = Convert.ToInt32(updateResult.ResultCode);
            var hResult = Convert.ToInt32(updateResult.HResult);
            var requiresRestart = SafeGetBool(updateResult, "RebootRequired")
                || SafeGetBool(entry.Update, "RebootRequired")
                || SafeGetBool(installationResult, "RebootRequired");

            var succeeded = resultCode is 2 or 3;
            var message = succeeded
                ? requiresRestart
                    ? "Installed. Restart required."
                    : "Installed."
                : $"Install failed: {DescribeOperationResult(resultCode)} (0x{unchecked((uint)hResult):X8}).";

            results.Add(
                new DriverUpdateInstallResult(
                    entry.UpdateId,
                    entry.Title,
                    succeeded,
                    succeeded,
                    requiresRestart,
                    message));
        }

        results.AddRange(
            missingIds.Select(
                updateId => new DriverUpdateInstallResult(
                    updateId,
                    updateId,
                    false,
                    false,
                    false,
                    "This driver update is no longer available from Windows Update.")));

        return results;
    }

    private static Dictionary<string, AvailableDriverUpdateEntry> GetAvailableUpdatesById(dynamic searcher)
    {
        var updates = new Dictionary<string, AvailableDriverUpdateEntry>(StringComparer.OrdinalIgnoreCase);
        AddAvailableUpdates(updates, searcher, RecommendedDriverCriteria);
        AddAvailableUpdates(updates, searcher, OptionalDriverCriteria);
        return updates;
    }

    private static void AddAvailableUpdates(Dictionary<string, AvailableDriverUpdateEntry> updates, dynamic searcher, string criteria)
    {
        dynamic searchResult = searcher.Search(criteria);
        var count = Convert.ToInt32(searchResult.Updates.Count);
        for (var index = 0; index < count; index++)
        {
            dynamic update = searchResult.Updates.Item(index);
            var updateId = BuildUpdateId(update);
            if (!updates.ContainsKey(updateId))
            {
                updates.Add(updateId, new AvailableDriverUpdateEntry(updateId, SafeGetString(update, "Title", updateId), update));
            }
        }
    }

    private static void AddSearchResults(
        IDictionary<string, DriverUpdateCandidate> updates,
        dynamic searcher,
        string criteria,
        bool isOptional)
    {
        dynamic searchResult = searcher.Search(criteria);
        var count = Convert.ToInt32(searchResult.Updates.Count);
        for (var index = 0; index < count; index++)
        {
            dynamic update = searchResult.Updates.Item(index);
            var updateId = BuildUpdateId(update);
            if (updates.ContainsKey(updateId))
            {
                continue;
            }

            updates.Add(
                updateId,
                new DriverUpdateCandidate(
                    updateId,
                    SafeGetString(update, "Title", "Driver update"),
                    SafeGetString(update, "DriverModel"),
                    SafeGetString(update, "DriverManufacturer"),
                    SafeGetString(update, "DriverClass"),
                    SafeGetDateString(update, "DriverVerDate"),
                    SafeGetString(update, "Description"),
                    isOptional));
        }
    }

    private static dynamic CreateUpdateSession()
    {
        var sessionType = Type.GetTypeFromProgID("Microsoft.Update.Session", throwOnError: true)
            ?? throw new InvalidOperationException("Windows Update Agent is unavailable on this PC.");
        dynamic session = Activator.CreateInstance(sessionType)
            ?? throw new InvalidOperationException("Windows Update Agent could not start a session.");
        session.ClientApplicationID = "MultiTool Driver Updater";
        return session;
    }

    private static dynamic CreateUpdateSearcher()
    {
        dynamic session = CreateUpdateSession();
        dynamic searcher = session.CreateUpdateSearcher();
        searcher.Online = true;
        return searcher;
    }

    private static dynamic CreateUpdateCollection()
    {
        var collectionType = Type.GetTypeFromProgID("Microsoft.Update.UpdateColl", throwOnError: true)
            ?? throw new InvalidOperationException("Windows Update update collection is unavailable on this PC.");
        return Activator.CreateInstance(collectionType)
            ?? throw new InvalidOperationException("Windows Update could not create an update collection.");
    }

    private static void TryAcceptEula(dynamic update)
    {
        try
        {
            if (!SafeGetBool(update, "EulaAccepted"))
            {
                update.AcceptEula();
            }
        }
        catch
        {
        }
    }

    private static string BuildUpdateId(dynamic update)
    {
        var identity = update.Identity;
        var updateId = Convert.ToString(identity.UpdateID)?.Trim();
        var revision = Convert.ToString(identity.RevisionNumber)?.Trim();

        if (string.IsNullOrWhiteSpace(updateId))
        {
            return Guid.NewGuid().ToString("D");
        }

        return string.IsNullOrWhiteSpace(revision)
            ? updateId!
            : $"{updateId}:{revision}";
    }

    private static string GetManagementString(ManagementBaseObject source, string propertyName, string fallback = "") =>
        Convert.ToString(source[propertyName])?.Trim() ?? fallback;

    private static string SafeGetString(dynamic source, string propertyName, string fallback = "")
    {
        try
        {
            return Convert.ToString(source.GetType().InvokeMember(propertyName, System.Reflection.BindingFlags.GetProperty, null, source, null))?.Trim() ?? fallback;
        }
        catch
        {
            return fallback;
        }
    }

    private static bool SafeGetBool(dynamic source, string propertyName)
    {
        try
        {
            return Convert.ToBoolean(source.GetType().InvokeMember(propertyName, System.Reflection.BindingFlags.GetProperty, null, source, null));
        }
        catch
        {
            return false;
        }
    }

    private static string SafeGetDateString(dynamic source, string propertyName)
    {
        try
        {
            var value = source.GetType().InvokeMember(propertyName, System.Reflection.BindingFlags.GetProperty, null, source, null);
            return value switch
            {
                DateTime dateTime => dateTime.ToString("yyyy-MM-dd"),
                _ => Convert.ToString(value)?.Trim() ?? string.Empty,
            };
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string DescribeOperationResult(int resultCode) =>
        resultCode switch
        {
            0 => "not started",
            1 => "in progress",
            2 => "succeeded",
            3 => "succeeded with errors",
            4 => "failed",
            5 => "aborted",
            _ => $"result code {resultCode}",
        };

    private static Task<T> RunStaAsync<T>(Func<T> work, CancellationToken cancellationToken)
    {
        var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        var workerThread = new Thread(
            () =>
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    completion.TrySetResult(work());
                }
                catch (OperationCanceledException)
                {
                    completion.TrySetCanceled(cancellationToken);
                }
                catch (Exception ex)
                {
                    completion.TrySetException(ex);
                }
            });

        workerThread.IsBackground = true;
        workerThread.SetApartmentState(ApartmentState.STA);
        workerThread.Start();

        if (cancellationToken.CanBeCanceled)
        {
            cancellationToken.Register(() => completion.TrySetCanceled(cancellationToken));
        }

        return completion.Task;
    }

    private sealed record AvailableDriverUpdateEntry(string UpdateId, string Title, dynamic Update);
}
