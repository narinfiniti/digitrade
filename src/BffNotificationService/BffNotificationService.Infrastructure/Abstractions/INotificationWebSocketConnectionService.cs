using System.Net.WebSockets;
using BffNotificationService.Infrastructure.Events;

namespace BffNotificationService.Infrastructure.Abstractions;

public interface INotificationWebSocketConnectionService
{
    Task RunConnectionAsync(
        string recipientId,
        WebSocket webSocket,
        CancellationToken cancellationToken = default);

    Task<int> BroadcastAsync(
        TerminalNotificationRequestedEvent integrationEvent,
        DateTimeOffset deliveredAtUtc,
        CancellationToken cancellationToken = default);
}