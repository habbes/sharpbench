using System.Collections.Concurrent;

namespace Sharpbench;

class JobRunner
{
    ConcurrentBag<Job> queue = new();
    JobsTracker tracker;
    public JobRunner(JobsTracker tracker)
    {
        tracker.OnNewJob(this.HandleOnNewJob);
        this.tracker = tracker;
    }

    public void HandleOnNewJob(Job job)
    {
        Console.WriteLine($"Queued job {job.Id}");
        queue.Add(job);
    }

    public void RunJobs()
    {
        Console.WriteLine("Running jobs...");
        Task.Run(() => RunBackgroundJobs()); // TODO: proper background job handling
    }

    private async Task RunBackgroundJobs()
    {
        while (true)
        {
            if (queue.IsEmpty)
            {
                continue;
            }

            if (queue.TryTake(out Job? job))
            {
                try {
                    await RunJob(job);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error occurred while processing job {job}: {e.Message}");
                }
            }
        }
    }

    private async Task RunJob(Job job)
    {
        this.tracker.ReportJobStarted(job.Id);
        var cwd = new DirectoryInfo(Directory.GetCurrentDirectory());
        Console.WriteLine($"cwd: {cwd}");
        var projectTemplateDir = Path.Combine(Directory.GetCurrentDirectory(), "project-template");
        foreach (var file in Directory.GetFiles(projectTemplateDir))
        {
            Console.WriteLine($"file {file}");
        }

        Console.WriteLine($"Running job {job.Id}");
    }
}