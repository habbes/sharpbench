
using Sharpbench.Core;

namespace SharpbenchApi;

internal class RealtimeMessagesWorker : BackgroundService
{
    IJobMessageStream messages;
    RealtimeClientsNotifier clientsNotifier;
    ILogger<RealtimeMessagesWorker> logger;

    public RealtimeMessagesWorker(
        IJobMessageStream messages,
        RealtimeClientsNotifier clientsNotifier,
        ILogger<RealtimeMessagesWorker> logger
    )
    {
        this.messages = messages;
        this.logger = logger;
        this.clientsNotifier = clientsNotifier;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Server listening to job notifications...");
        while (!stoppingToken.IsCancellationRequested)
        {
            await foreach (var message in messages.ListenForMessages(stoppingToken))
            {
                logger.LogInformation($"Server received message {message.Type} for job {message.JobId}");
                await clientsNotifier.BroadcastMessage(message);
            }
        }

        await clientsNotifier.CloseAllClients();
    }
}
