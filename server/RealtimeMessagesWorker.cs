
using Sharpbench.Core;
using SharpbenchCore;

namespace SharpbenchApi;

internal class RealtimeMessagesWorker : BackgroundService
{
    IJobMessageStream messages;
    RealtimeClientsNotifier clientsNotifier;
    IJobRepository jobRepository;
    ILogger<RealtimeMessagesWorker> logger;

    public RealtimeMessagesWorker(
        IJobMessageStream messages,
        RealtimeClientsNotifier clientsNotifier,
        IJobRepository jobRepository,
        ILogger<RealtimeMessagesWorker> logger
    )
    {
        this.messages = messages;
        this.logger = logger;
        this.clientsNotifier = clientsNotifier;
        this.jobRepository = jobRepository;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Server listening to job notifications...");
        while (!stoppingToken.IsCancellationRequested)
        {
            await foreach (var message in messages.ListenForMessages(stoppingToken))
            {
                logger.LogInformation($"Server received message {message.Type} for job {message.JobId}");

                try
                {
                    var job = await jobRepository.GetJob(message.JobId);
                    if (string.IsNullOrEmpty(job.ClientId))
                    {
                        // Jobs from an older version of Sharpbench did not have a client ID.
                        // Those jobs could still be in the DB. We ignore messages from those jobs.
                        // Eventually we'll remove this check once all such jobs have been cleared
                        // from the DB.
                        logger.LogWarning($"Attempted to send message for job {job.Id} without a client id. Message will be dropped.");
                        return;
                    }

                    await clientsNotifier.SendMessageToClient(job.ClientId, message);
                } catch (ResourceNotFoundException)
                {
                    logger.LogWarning($"Attempting to send job message but job {message.JobId} was not found.");
                    // Possible for job to have been evicted from cache by the time message is sent.
                    // TODO: consider adding client ID to message.
                }
            }
        }

        await clientsNotifier.CloseAllClients();
    }
}
