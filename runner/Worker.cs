using Sharpbench.Core;

namespace Sharpbench.Runner;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    IJobRepository jobs;
    IJobQueue queue;
    IJobMessageStream messages;

    public Worker(ILogger<Worker> logger, IJobRepository jobs, IJobQueue queue, IJobMessageStream messageStream)
    {
        _logger = logger;
        this.jobs = jobs;
        this.queue = queue;
        this.messages = messageStream;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker listening for jobs...");
        await foreach (string jobId in queue.ListenForJobs(stoppingToken))
        {
            _logger.LogInformation($"Worker received job '{jobId}'");
            var runner = new JobRunner(jobId, _logger, this.jobs, this.messages);
            await runner.RunJob();
        }
    }
}
