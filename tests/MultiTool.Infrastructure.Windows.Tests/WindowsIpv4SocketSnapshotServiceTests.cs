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
            getTcpConnections: () =>
            [
                new FakeTcpConnectionInformation(
                    new IPEndPoint(IPAddress.Parse("10.0.0.25"), 52344),
                    new IPEndPoint(IPAddress.Parse("93.184.216.34"), 443),
                    TcpState.Established),
                new FakeTcpConnectionInformation(
                    new IPEndPoint(IPAddress.IPv6Loopback, 5000),
                    new IPEndPoint(IPAddress.IPv6Loopback, 443),
                    TcpState.Established),
            ],
            getTcpListeners: () =>
            [
                new IPEndPoint(IPAddress.Any, 80),
                new IPEndPoint(IPAddress.IPv6Any, 8080),
            ],
            getUdpListeners: () =>
            [
                new IPEndPoint(IPAddress.Loopback, 53),
            ]);

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
        });
        result.Entries.Should().ContainEquivalentOf(new
        {
            Protocol = "tcp4",
            State = "LISTEN",
            LocalEndpoint = "0.0.0.0:80",
            RemoteEndpoint = "*:*",
        });
        result.Entries.Should().ContainEquivalentOf(new
        {
            Protocol = "udp4",
            State = "UNCONN",
            LocalEndpoint = "127.0.0.1:53",
            RemoteEndpoint = "*:*",
        });
    }

    private sealed class FakeTcpConnectionInformation : TcpConnectionInformation
    {
        public FakeTcpConnectionInformation(IPEndPoint localEndPoint, IPEndPoint remoteEndPoint, TcpState state)
        {
            LocalEndPoint = localEndPoint;
            RemoteEndPoint = remoteEndPoint;
            State = state;
        }

        public override IPEndPoint LocalEndPoint { get; }

        public override IPEndPoint RemoteEndPoint { get; }

        public override TcpState State { get; }
    }
}
