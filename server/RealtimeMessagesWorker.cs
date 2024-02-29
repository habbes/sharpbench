
using Sharpbench.Core;

namespace SharpbenchApi;

internal class RealtimeMessagesWorker(
    IJobMessageStream messages,
    RealtimeClientsNotifier clientsNotifier,
    ILogger<RealtimeMessagesWorker> logger
    ) : BackgroundService
{

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
