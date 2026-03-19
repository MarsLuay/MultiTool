using MultiTool.Core.Models;

namespace MultiTool.Core.Services;

public interface IIpv4SocketSnapshotService
{
    Task<Ipv4SocketSnapshotResult> CaptureAsync(CancellationToken cancellationToken = default);
}
