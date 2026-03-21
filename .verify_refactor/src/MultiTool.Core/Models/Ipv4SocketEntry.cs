namespace MultiTool.Core.Models;

public sealed record Ipv4SocketEntry(
    string Protocol,
    string State,
    string LocalEndpoint,
    string RemoteEndpoint,
    string ProgramName,
    int ProcessId)
{
    public string ProgramDisplayName =>
        string.IsNullOrWhiteSpace(ProgramName)
            ? "Unknown app"
            : ProgramName;

    public string ProgramSummary =>
        ProcessId > 0
            ? $"{ProgramDisplayName} (PID {ProcessId})"
            : ProgramDisplayName;

    public string EntryKindLabel =>
        string.Equals(Protocol, "udp4", StringComparison.OrdinalIgnoreCase)
            ? "UDP listener"
            : string.Equals(State, "LISTEN", StringComparison.OrdinalIgnoreCase)
                ? "TCP listener"
                : "TCP connection";
}
