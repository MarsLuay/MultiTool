using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using MultiTool.Core.Models;
using MultiTool.Core.Services;

namespace MultiTool.Infrastructure.Windows.Tools;

public sealed class WindowsIpv4SocketSnapshotService : IIpv4SocketSnapshotService
{
    private readonly Func<TcpConnectionInformation[]> getTcpConnections;
    private readonly Func<IPEndPoint[]> getTcpListeners;
    private readonly Func<IPEndPoint[]> getUdpListeners;

    public WindowsIpv4SocketSnapshotService()
        : this(null, null, null)
    {
    }

    internal WindowsIpv4SocketSnapshotService(
        Func<TcpConnectionInformation[]>? getTcpConnections = null,
        Func<IPEndPoint[]>? getTcpListeners = null,
        Func<IPEndPoint[]>? getUdpListeners = null)
    {
        this.getTcpConnections = getTcpConnections ?? GetActiveTcpConnections;
        this.getTcpListeners = getTcpListeners ?? GetActiveTcpListeners;
        this.getUdpListeners = getUdpListeners ?? GetActiveUdpListeners;
    }

    public Task<Ipv4SocketSnapshotResult> CaptureAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var tcpConnections = getTcpConnections()
            .Where(static connection =>
                connection.LocalEndPoint.AddressFamily == AddressFamily.InterNetwork
                && connection.RemoteEndPoint.AddressFamily == AddressFamily.InterNetwork)
            .Select(static connection => new Ipv4SocketEntry(
                Protocol: "tcp4",
                State: FormatTcpState(connection.State),
                LocalEndpoint: FormatEndpoint(connection.LocalEndPoint),
                RemoteEndpoint: FormatEndpoint(connection.RemoteEndPoint)))
            .ToArray();

        cancellationToken.ThrowIfCancellationRequested();

        var tcpListeners = getTcpListeners()
            .Where(static endpoint => endpoint.AddressFamily == AddressFamily.InterNetwork)
            .Select(static endpoint => new Ipv4SocketEntry(
                Protocol: "tcp4",
                State: "LISTEN",
                LocalEndpoint: FormatEndpoint(endpoint),
                RemoteEndpoint: "*:*"))
            .ToArray();

        cancellationToken.ThrowIfCancellationRequested();

        var udpListeners = getUdpListeners()
            .Where(static endpoint => endpoint.AddressFamily == AddressFamily.InterNetwork)
            .Select(static endpoint => new Ipv4SocketEntry(
                Protocol: "udp4",
                State: "UNCONN",
                LocalEndpoint: FormatEndpoint(endpoint),
                RemoteEndpoint: "*:*"))
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

    private static TcpConnectionInformation[] GetActiveTcpConnections() =>
        IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpConnections();

    private static IPEndPoint[] GetActiveTcpListeners() =>
        IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners();

    private static IPEndPoint[] GetActiveUdpListeners() =>
        IPGlobalProperties.GetIPGlobalProperties().GetActiveUdpListeners();

    private static string FormatEndpoint(IPEndPoint endpoint) =>
        $"{endpoint.Address}:{endpoint.Port}";

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
}
