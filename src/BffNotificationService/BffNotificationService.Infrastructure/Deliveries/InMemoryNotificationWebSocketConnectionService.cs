using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text.Json;
using BffNotificationService.Infrastructure.Abstractions;
using BffNotificationService.Infrastructure.Events;

namespace BffNotificationService.Infrastructure.Deliveries;

public sealed class InMemoryNotificationWebSocketConnectionService : INotificationWebSocketConnectionService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, WebSocket>> recipientSockets = new(StringComparer.Ordinal);

    public async Task RunConnectionAsync(
        string recipientId,
        WebSocket webSocket,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(recipientId);
        ArgumentNullException.ThrowIfNull(webSocket);

        var connectionId = Guid.NewGuid();
        var sockets = recipientSockets.GetOrAdd(recipientId, _ => new ConcurrentDictionary<Guid, WebSocket>());
        sockets[connectionId] = webSocket;

        try
        {
            var receiveBuffer = new byte[4 * 1024];

            while (!cancellationToken.IsCancellationRequested && webSocket.State == WebSocketState.Open)
            {
                var receiveResult = await webSocket.ReceiveAsync(receiveBuffer, cancellationToken);
                if (receiveResult.MessageType == WebSocketMessageType.Close)
                {
                    await CloseIfNeededAsync(webSocket, cancellationToken);
                    break;
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        finally
        {
            RemoveSocket(recipientId, connectionId);
            await DisposeSocketAsync(webSocket);
        }
    }

    public async Task<int> BroadcastAsync(
        TerminalNotificationRequestedEvent integrationEvent,
        DateTimeOffset deliveredAtUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        if (!recipientSockets.TryGetValue(integrationEvent.RecipientId, out var sockets)
            || sockets.IsEmpty)
        {
            return 0;
        }

        var payload = JsonSerializer.SerializeToUtf8Bytes(
            new NotificationStreamEnvelope(
                integrationEvent.EventId,
                integrationEvent.AggregateId,
                integrationEvent.RecipientId,
                integrationEvent.Channel,
                integrationEvent.Subject,
                integrationEvent.Message,
                integrationEvent.CorrelationId,
                deliveredAtUtc),
            SerializerOptions);

        var staleConnectionIds = new List<Guid>();
        var deliveredConnectionCount = 0;

        foreach (var socketEntry in sockets)
        {
            if (socketEntry.Value.State != WebSocketState.Open)
            {
                staleConnectionIds.Add(socketEntry.Key);
                continue;
            }

            try
            {
                await socketEntry.Value.SendAsync(payload, WebSocketMessageType.Text, true, cancellationToken);
                deliveredConnectionCount++;
            }
            catch (WebSocketException)
            {
                staleConnectionIds.Add(socketEntry.Key);
            }
        }

        foreach (var staleConnectionId in staleConnectionIds)
        {
            RemoveSocket(integrationEvent.RecipientId, staleConnectionId);
        }

        return deliveredConnectionCount;
    }

    private void RemoveSocket(string recipientId, Guid connectionId)
    {
        if (!recipientSockets.TryGetValue(recipientId, out var sockets))
        {
            return;
        }

        sockets.TryRemove(connectionId, out _);

        if (sockets.IsEmpty)
        {
            recipientSockets.TryRemove(recipientId, out _);
        }
    }

    private static async Task CloseIfNeededAsync(WebSocket webSocket, CancellationToken cancellationToken)
    {
        if (webSocket.State is WebSocketState.CloseReceived or WebSocketState.Open)
        {
            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Connection closed.", cancellationToken);
        }
    }

    private static async Task DisposeSocketAsync(WebSocket webSocket)
    {
        if (webSocket.State is WebSocketState.Open or WebSocketState.CloseReceived)
        {
            try
            {
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Connection disposed.", CancellationToken.None);
            }
            catch (WebSocketException)
            {
            }
        }

        webSocket.Dispose();
    }

    private sealed record NotificationStreamEnvelope(
        Guid EventId,
        string AggregateId,
        string RecipientId,
        string Channel,
        string Subject,
        string Message,
        string CorrelationId,
        DateTimeOffset DeliveredAtUtc);
}