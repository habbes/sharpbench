using Sharpbench.Core;

namespace Sharpbench.Runner;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    IJobRepository db;
    IJobQueue queue;
    IJobMessageStream messagStream;

    public Worker(ILogger<Worker> logger, IJobRepository jobs, IJobQueue queue, IJobMessageStream messageStream)
    {
        _logger = logger;
        this.db = jobs;
        this.queue = queue;
        this.messagStream = messageStream;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (string jobId in queue.ListenForJobs(stoppingToken))
        {
            _logger.LogInformation($"Worker received job '{jobId}'");
            var runner = new JobRunner(this._logger, this.db, jobId);
            await runner.RunJob();
        }
    }
}
