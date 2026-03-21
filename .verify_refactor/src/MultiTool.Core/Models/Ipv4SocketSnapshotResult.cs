namespace MultiTool.Core.Models;

public sealed record Ipv4SocketSnapshotResult(
    DateTimeOffset CapturedAt,
    IReadOnlyList<Ipv4SocketEntry> Entries,
    int TcpConnectionCount,
    int TcpListenerCount,
    int UdpListenerCount);
