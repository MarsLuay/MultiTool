namespace MultiTool.Core.Models;

public sealed record Ipv4SocketEntry(
    string Protocol,
    string State,
    string LocalEndpoint,
    string RemoteEndpoint);
