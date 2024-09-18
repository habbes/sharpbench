using System.Collections.Concurrent;
using System.Net.WebSockets;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.Extensions.Logging;
using Sharpbench.Core;

namespace SharpbenchApi;

public class RealtimeClientsNotifier
{
    ConcurrentDictionary<string, RealTimeClient> realTimeClients = new();
    ILogger<RealtimeClientsNotifier> logger;

    public RealtimeClientsNotifier(ILogger<RealtimeClientsNotifier> logger)
    {
        this.logger = logger;
    }

    public Task RealTimeSyncWithClient(WebSocket socket, string clientId)
    {
        logger.LogInformation($"Realtime communication established with new client {clientId}");
        var closeSignal = new TaskCompletionSource();
        realTimeClients.AddOrUpdate(clientId, id =>
        {
            var client = new RealTimeClient(id, logger);
            client.AddConnection(socket, closeSignal);
            return client;
        },
        (id, client) =>
        {
            client.AddConnection(socket, closeSignal);
            return client;
        });
        
        // the task will be complete when we detect that the connection is closed
        // or all communication has ended.
        // This prevents the caller from terminating prematurely and abruptly closing the connection
        return closeSignal.Task;
    }

    public async Task BroadcastMessage(JobMessage message)
    {
        var data = new ArraySegment<byte>(message.Data, 0, message.Data.Length);
        await BroadcastRawMessage(data);
    }

    public async Task SendMessageToClient(string clientId, JobMessage message)
    {
        var data = new ArraySegment<byte>(message.Data, 0, message.Data.Length);
        await TrySendToClient(clientId, data);
    }

    public async Task CloseAllClients()
    {
        List<Task> tasks = new List<Task>();
        foreach (var entry in realTimeClients)
        {
            tasks.Add(entry.Value.Close());
        }

        await Task.WhenAll(tasks);
        realTimeClients.Clear();
    }

    private async Task BroadcastRawMessage(ArraySegment<byte> message)
    {
        List<Task<bool>> tasks = new List<Task<bool>>();
        foreach (var kvp in realTimeClients)
        {
            var client = kvp.Value;
            tasks.Add(TrySendToClient(kvp.Key, message));
        }

        await Task.WhenAll(tasks);
    }

    async Task<bool> TrySendToClient(string clientId, ArraySegment<byte> message)
    {
        if (!realTimeClients.TryGetValue(clientId, out var client))
        {
            logger.LogWarning($"Attempted to send message to client {clientId}, but it was not found.");
            return false;
        }

        logger.LogInformation($"Attempt to send message to client {clientId}");
        await client.SendMessage(message);
        logger.LogInformation($"Sent message to client {clientId}");
        return true;
    }
}

class RealTimeClient(string clientId, ILogger<RealtimeClientsNotifier> logger)
{
    ConcurrentDictionary<ClientConnection, bool> connections = new();

    public string ClientId => clientId;

    /// <summary>
    /// Attaches a socket to this client. The socket will receive messages
    /// sent to this client ID.
    /// </summary>
    /// <param name="socket"></param>
    /// <param name="closeSignal">A task completion source that will be resolved when the client is closed. This helps notify the caller when the socket connection is closed.</param>
    /// <returns>A task that will resolve when the connection to the socket is terminated.</returns>
    public void AddConnection(WebSocket socket, TaskCompletionSource closeSignal)
    {
        connections[new ClientConnection(socket, closeSignal)] = true;
    }

    public async Task SendMessage(ArraySegment<byte> message)
    {
        List<Task<SendResult>> tasks = new(connections.Count);
        foreach (var connection in connections.Keys)
        {
            tasks.Add(TrySendToConnection(connection, message));
        }

        var results = await Task.WhenAll(tasks);

        // we remove the connections in a separate loop so that we don't modify the loop while it's being enumerated
        // TODO: verify whether deleting from a concurrent dictionary while it's being enumerated would be a problem

        foreach (var result in results)
        {
            if (result.SocketClosed)
            {
                logger.LogInformation($"Removing closed connection for client {clientId}");
                connections.Remove(result.Connection, out _);
            }
        }
    }

    public async Task Close()
    {
        List<Task> tasks = new List<Task>();
        foreach (var connection in connections.Keys)
        {
            tasks.Add(connection.Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed", default));
            connection.TaskHandler.SetResult();
        }

        await Task.WhenAll(tasks);
        connections.Clear();
    }

    private async Task<SendResult> TrySendToConnection(ClientConnection connection, ArraySegment<byte> message)
    {
        if (connection.Socket.State == WebSocketState.Closed)
        {
            logger.LogInformation($"Attempted to send message to client {clientId}, but client socket was closed.");
            
            connection.TaskHandler.SetResult();
            return new SendResult(connection, SocketClosed: true);
        }

        logger.LogInformation($"Attempt to send message to client {clientId}");
        await connection.Socket.SendAsync(message, WebSocketMessageType.Text, true, CancellationToken.None);
        logger.LogInformation($"Sent message to client {clientId}");
        return new SendResult(connection);
    }

    private record ClientConnection(WebSocket Socket, TaskCompletionSource TaskHandler);
    private record SendResult(ClientConnection Connection, bool SocketClosed = false);
}

/// <summary>
/// A user can connect from different browser tabs with the same client ID since the client ID
/// is persisted in the user's IndexedDB.
/// </summary>
/// <param name="ClientId"></param>
/// <param name="Instances"></param>
record ClientEntry(string ClientId, ConcurrentBag<ClientEntry> Connections);
