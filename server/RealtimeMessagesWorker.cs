
using Sharpbench.Core;

namespace SharpbenchApi;

internal class RealtimeMessagesWorker : BackgroundService
{
    IJobMessageStream messages;
    RealtimeClientsNotifier clientsNotifier;

    public RealtimeMessagesWorker(IJobMessageStream messages, RealtimeClientsNotifier clientsNotifier)
    {
        this.messages = messages;
        this.clientsNotifier = clientsNotifier;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await foreach (var message in this.messages.ListenForMessages(stoppingToken))
            {
                await this.clientsNotifier.BroadcastMessage(message);
            }
        }

        await clientsNotifier.CloseAllClients();
    }
}
