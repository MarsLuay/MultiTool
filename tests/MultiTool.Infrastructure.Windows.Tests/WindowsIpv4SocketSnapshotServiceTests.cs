using System.Net;
using System.Net.NetworkInformation;
using MultiTool.Infrastructure.Windows.Tools;
using FluentAssertions;

namespace MultiTool.Infrastructure.Windows.Tests;

public sealed class WindowsIpv4SocketSnapshotServiceTests
{
    [Fact]
    public async Task CaptureAsync_ShouldReturnOnlyIpv4SocketsAndCorrectCounts()
    {
        var service = new WindowsIpv4SocketSnapshotService(
            getTcpRows: () =>
            [
                new WindowsIpv4SocketSnapshotService.OwnedTcpSocketRow(
                    new IPEndPoint(IPAddress.Parse("10.0.0.25"), 52344),
                    new IPEndPoint(IPAddress.Parse("93.184.216.34"), 443),
                    TcpState.Established,
                    1200),
                new WindowsIpv4SocketSnapshotService.OwnedTcpSocketRow(
                    new IPEndPoint(IPAddress.Any, 80),
                    new IPEndPoint(IPAddress.Any, 0),
                    TcpState.Listen,
                    4),
                new WindowsIpv4SocketSnapshotService.OwnedTcpSocketRow(
                    new IPEndPoint(IPAddress.IPv6Loopback, 5000),
                    new IPEndPoint(IPAddress.IPv6Loopback, 443),
                    TcpState.Established,
                    5000),
            ],
            getUdpRows: () =>
            [
                new WindowsIpv4SocketSnapshotService.OwnedUdpSocketRow(
                    new IPEndPoint(IPAddress.Loopback, 53),
                    53),
                new WindowsIpv4SocketSnapshotService.OwnedUdpSocketRow(
                    new IPEndPoint(IPAddress.IPv6Any, 8080),
                    8080),
            ],
            getProcessName: processId => processId switch
            {
                1200 => "chrome.exe",
                4 => "system",
                53 => "dns.exe",
                _ => string.Empty,
            });

        var result = await service.CaptureAsync();

        result.TcpConnectionCount.Should().Be(1);
        result.TcpListenerCount.Should().Be(1);
        result.UdpListenerCount.Should().Be(1);
        result.Entries.Should().HaveCount(3);
        result.Entries.Should().ContainEquivalentOf(new
        {
            Protocol = "tcp4",
            State = "ESTAB",
            LocalEndpoint = "10.0.0.25:52344",
            RemoteEndpoint = "93.184.216.34:443",
            ProgramName = "chrome.exe",
            ProcessId = 1200,
        });
        result.Entries.Should().ContainEquivalentOf(new
        {
            Protocol = "tcp4",
            State = "LISTEN",
            LocalEndpoint = "0.0.0.0:80",
            RemoteEndpoint = "*:*",
            ProgramName = "system",
            ProcessId = 4,
        });
        result.Entries.Should().ContainEquivalentOf(new
        {
            Protocol = "udp4",
            State = "UNCONN",
            LocalEndpoint = "127.0.0.1:53",
            RemoteEndpoint = "*:*",
            ProgramName = "dns.exe",
            ProcessId = 53,
        });
    }
}
