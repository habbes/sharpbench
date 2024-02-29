using System.Collections.Concurrent;
using System.Net.WebSockets;
using Sharpbench.Core;

namespace SharpbenchApi;

public class RealtimeClientsNotifier
{
    ConcurrentDictionary<WebSocket, ClientEntry> realTimeClients = new();

    public Task RealTimeSyncWithClient(WebSocket client)
    {
        var tcs = new TaskCompletionSource();
        realTimeClients.TryAdd(client, new ClientEntry(client, tcs));
        // TODO how to detect when client is closed
        realTimeClients.Remove(client, out _);
        return tcs.Task;
    }

    public async Task BroadcastMessage(JobMessage message)
    {
        // TODO: broadcast to all for simplicity, but should send messages to the right clients
        var data = new ArraySegment<byte>(message.Data, 0, message.Data.Length);
        await BroadcastRawMessage(data);
    }

    public async Task CloseAllClients()
    {
        List<Task> tasks = new List<Task>();
        foreach (var entry in realTimeClients)
        {
            tasks.Add(entry.Key.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed", default));
            entry.Value.TaskHandler.SetResult();
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
            await client.Socket.SendAsync(message, WebSocketMessageType.Text, true, CancellationToken.None);
            tasks.Add(TrySendToClient(kvp.Key, message));
        }

        await Task.WhenAll(tasks);
    }

    async Task<bool> TrySendToClient(WebSocket key, ArraySegment<byte> message)
    {
        if (!realTimeClients.TryGetValue(key, out var clientEntry))
        {
            return false;
        }

        if (clientEntry.Socket.State == WebSocketState.Closed)
        {
            realTimeClients.Remove(key, out _);
            // complete the task being awaited
            clientEntry.TaskHandler.SetResult();
            return false;
        }

        await clientEntry.Socket.SendAsync(message, WebSocketMessageType.Text, true, CancellationToken.None);
        return true;
    }
}

record ClientEntry(WebSocket Socket, TaskCompletionSource TaskHandler);