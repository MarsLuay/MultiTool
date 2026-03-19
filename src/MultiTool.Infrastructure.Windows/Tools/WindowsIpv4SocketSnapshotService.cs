using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using MultiTool.Core.Models;
using MultiTool.Core.Services;

namespace MultiTool.Infrastructure.Windows.Tools;

public sealed class WindowsIpv4SocketSnapshotService : IIpv4SocketSnapshotService
{
    private const uint NoError = 0;
    private const uint ErrorInsufficientBuffer = 122;
    private const int AddressFamilyInterNetwork = 2;

    private readonly Func<IReadOnlyList<OwnedTcpSocketRow>> getTcpRows;
    private readonly Func<IReadOnlyList<OwnedUdpSocketRow>> getUdpRows;
    private readonly Func<int, string> getProcessName;

    public WindowsIpv4SocketSnapshotService()
        : this(null, null, null)
    {
    }

    internal WindowsIpv4SocketSnapshotService(
        Func<IReadOnlyList<OwnedTcpSocketRow>>? getTcpRows = null,
        Func<IReadOnlyList<OwnedUdpSocketRow>>? getUdpRows = null,
        Func<int, string>? getProcessName = null)
    {
        this.getTcpRows = getTcpRows ?? GetOwnedTcpRows;
        this.getUdpRows = getUdpRows ?? GetOwnedUdpRows;
        this.getProcessName = getProcessName ?? GetProcessName;
    }

    public Task<Ipv4SocketSnapshotResult> CaptureAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var tcpRows = getTcpRows();
        var udpRows = getUdpRows();
        var processNames = new Dictionary<int, string>();
        string ResolveProgramName(int processId)
        {
            if (!processNames.TryGetValue(processId, out var programName))
            {
                programName = getProcessName(processId);
                processNames[processId] = programName;
            }

            return programName;
        }

        var tcpConnections = tcpRows
            .Where(static row =>
                row.State != TcpState.Listen
                && row.LocalEndPoint.AddressFamily == AddressFamily.InterNetwork
                && row.RemoteEndPoint.AddressFamily == AddressFamily.InterNetwork)
            .Select(row => new Ipv4SocketEntry(
                Protocol: "tcp4",
                State: FormatTcpState(row.State),
                LocalEndpoint: FormatEndpoint(row.LocalEndPoint),
                RemoteEndpoint: FormatEndpoint(row.RemoteEndPoint),
                ProgramName: ResolveProgramName(row.ProcessId),
                ProcessId: row.ProcessId))
            .ToArray();

        cancellationToken.ThrowIfCancellationRequested();

        var tcpListeners = tcpRows
            .Where(static row =>
                row.State == TcpState.Listen
                && row.LocalEndPoint.AddressFamily == AddressFamily.InterNetwork)
            .Select(row => new Ipv4SocketEntry(
                Protocol: "tcp4",
                State: "LISTEN",
                LocalEndpoint: FormatEndpoint(row.LocalEndPoint),
                RemoteEndpoint: "*:*",
                ProgramName: ResolveProgramName(row.ProcessId),
                ProcessId: row.ProcessId))
            .ToArray();

        cancellationToken.ThrowIfCancellationRequested();

        var udpListeners = udpRows
            .Where(static row => row.LocalEndPoint.AddressFamily == AddressFamily.InterNetwork)
            .Select(row => new Ipv4SocketEntry(
                Protocol: "udp4",
                State: "UNCONN",
                LocalEndpoint: FormatEndpoint(row.LocalEndPoint),
                RemoteEndpoint: "*:*",
                ProgramName: ResolveProgramName(row.ProcessId),
                ProcessId: row.ProcessId))
            .ToArray();

        var entries = tcpConnections
            .Concat(tcpListeners)
            .Concat(udpListeners)
            .OrderBy(static entry => GetProtocolSortOrder(entry.Protocol))
            .ThenBy(static entry => entry.LocalEndpoint, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static entry => entry.RemoteEndpoint, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static entry => entry.State, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var result = new Ipv4SocketSnapshotResult(
            CapturedAt: DateTimeOffset.Now,
            Entries: entries,
            TcpConnectionCount: tcpConnections.Length,
            TcpListenerCount: tcpListeners.Length,
            UdpListenerCount: udpListeners.Length);

        return Task.FromResult(result);
    }

    internal readonly record struct OwnedTcpSocketRow(
        IPEndPoint LocalEndPoint,
        IPEndPoint RemoteEndPoint,
        TcpState State,
        int ProcessId);

    internal readonly record struct OwnedUdpSocketRow(
        IPEndPoint LocalEndPoint,
        int ProcessId);

    private static IReadOnlyList<OwnedTcpSocketRow> GetOwnedTcpRows()
    {
        using var buffer = AllocateNetworkTableBuffer(
            static (IntPtr pointer, ref int length) => GetExtendedTcpTable(
                pointer,
                ref length,
                order: false,
                AddressFamilyInterNetwork,
                TcpTableClass.TcpTableOwnerPidAll,
                reserved: 0),
            "Unable to read the Windows TCP ownership table.");

        var rowCount = Marshal.ReadInt32(buffer.Pointer);
        var rowPointer = IntPtr.Add(buffer.Pointer, sizeof(int));
        var rowSize = Marshal.SizeOf<MibTcpRowOwnerPid>();
        var rows = new OwnedTcpSocketRow[rowCount];

        for (var index = 0; index < rowCount; index++)
        {
            var row = Marshal.PtrToStructure<MibTcpRowOwnerPid>(rowPointer);
            rows[index] = new OwnedTcpSocketRow(
                LocalEndPoint: new IPEndPoint(new IPAddress(row.LocalAddress), GetPort(row.LocalPort)),
                RemoteEndPoint: new IPEndPoint(new IPAddress(row.RemoteAddress), GetPort(row.RemotePort)),
                State: (TcpState)row.State,
                ProcessId: unchecked((int)row.OwningPid));
            rowPointer = IntPtr.Add(rowPointer, rowSize);
        }

        return rows;
    }

    private static IReadOnlyList<OwnedUdpSocketRow> GetOwnedUdpRows()
    {
        using var buffer = AllocateNetworkTableBuffer(
            static (IntPtr pointer, ref int length) => GetExtendedUdpTable(
                pointer,
                ref length,
                order: false,
                AddressFamilyInterNetwork,
                UdpTableClass.UdpTableOwnerPid,
                reserved: 0),
            "Unable to read the Windows UDP ownership table.");

        var rowCount = Marshal.ReadInt32(buffer.Pointer);
        var rowPointer = IntPtr.Add(buffer.Pointer, sizeof(int));
        var rowSize = Marshal.SizeOf<MibUdpRowOwnerPid>();
        var rows = new OwnedUdpSocketRow[rowCount];

        for (var index = 0; index < rowCount; index++)
        {
            var row = Marshal.PtrToStructure<MibUdpRowOwnerPid>(rowPointer);
            rows[index] = new OwnedUdpSocketRow(
                LocalEndPoint: new IPEndPoint(new IPAddress(row.LocalAddress), GetPort(row.LocalPort)),
                ProcessId: unchecked((int)row.OwningPid));
            rowPointer = IntPtr.Add(rowPointer, rowSize);
        }

        return rows;
    }

    private static NativeBuffer AllocateNetworkTableBuffer(
        NetworkTableReader reader,
        string errorMessage)
    {
        var bufferLength = 0;
        var status = reader(IntPtr.Zero, ref bufferLength);
        if (status != ErrorInsufficientBuffer)
        {
            throw CreateNetworkTableException(errorMessage, status);
        }

        var pointer = Marshal.AllocHGlobal(bufferLength);
        try
        {
            status = reader(pointer, ref bufferLength);
            if (status != NoError)
            {
                throw CreateNetworkTableException(errorMessage, status);
            }

            return new NativeBuffer(pointer);
        }
        catch
        {
            Marshal.FreeHGlobal(pointer);
            throw;
        }
    }

    private static Exception CreateNetworkTableException(string message, uint status) =>
        new Win32Exception(unchecked((int)status), $"{message} (error {status}).");

    private static string GetProcessName(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            var processName = process.ProcessName?.Trim();
            if (string.IsNullOrWhiteSpace(processName))
            {
                return string.Empty;
            }

            return processName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                ? processName
                : $"{processName}.exe";
        }
        catch (ArgumentException)
        {
            return string.Empty;
        }
        catch (InvalidOperationException)
        {
            return string.Empty;
        }
        catch (Win32Exception)
        {
            return string.Empty;
        }
    }

    private static string FormatEndpoint(IPEndPoint endpoint) =>
        $"{endpoint.Address}:{endpoint.Port}";

    private static int GetPort(byte[] portBytes) =>
        (portBytes[0] << 8) | portBytes[1];

    private static int GetProtocolSortOrder(string protocol) =>
        string.Equals(protocol, "tcp4", StringComparison.OrdinalIgnoreCase) ? 0 : 1;

    private static string FormatTcpState(TcpState state) =>
        state switch
        {
            TcpState.Established => "ESTAB",
            TcpState.SynSent => "SYN-SENT",
            TcpState.SynReceived => "SYN-RECV",
            TcpState.FinWait1 => "FIN-WAIT-1",
            TcpState.FinWait2 => "FIN-WAIT-2",
            TcpState.TimeWait => "TIME-WAIT",
            TcpState.Closed => "CLOSED",
            TcpState.CloseWait => "CLOSE-WAIT",
            TcpState.LastAck => "LAST-ACK",
            TcpState.Listen => "LISTEN",
            TcpState.Closing => "CLOSING",
            _ => state.ToString().ToUpperInvariant(),
        };

    private delegate uint NetworkTableReader(IntPtr pointer, ref int length);

    private sealed class NativeBuffer : IDisposable
    {
        public NativeBuffer(IntPtr pointer)
        {
            Pointer = pointer;
        }

        public IntPtr Pointer { get; }

        public void Dispose()
        {
            if (Pointer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(Pointer);
            }
        }
    }

    [DllImport("iphlpapi.dll", SetLastError = true)]
    private static extern uint GetExtendedTcpTable(
        IntPtr tcpTable,
        ref int tcpTableLength,
        [MarshalAs(UnmanagedType.Bool)] bool order,
        int ipVersion,
        TcpTableClass tableClass,
        uint reserved);

    [DllImport("iphlpapi.dll", SetLastError = true)]
    private static extern uint GetExtendedUdpTable(
        IntPtr udpTable,
        ref int udpTableLength,
        [MarshalAs(UnmanagedType.Bool)] bool order,
        int ipVersion,
        UdpTableClass tableClass,
        uint reserved);

    private enum TcpTableClass
    {
        TcpTableBasicListener,
        TcpTableBasicConnections,
        TcpTableBasicAll,
        TcpTableOwnerPidListener,
        TcpTableOwnerPidConnections,
        TcpTableOwnerPidAll,
        TcpTableOwnerModuleListener,
        TcpTableOwnerModuleConnections,
        TcpTableOwnerModuleAll,
    }

    private enum UdpTableClass
    {
        UdpTableBasic,
        UdpTableOwnerPid,
        UdpTableOwnerModule,
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MibTcpRowOwnerPid
    {
        public uint State;

        public uint LocalAddress;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] LocalPort;

        public uint RemoteAddress;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] RemotePort;

        public uint OwningPid;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MibUdpRowOwnerPid
    {
        public uint LocalAddress;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] LocalPort;

        public uint OwningPid;
    }
}
