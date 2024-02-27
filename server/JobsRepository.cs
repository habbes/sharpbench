namespace Sharpbench;

class JobsTracker
{
    int nextId = 1;
    Dictionary<int, Job> jobs = new();
    private Action<Job>? newJobHandler;
    public SubmitJsobResult SubmitJob(string code)
    {
        int id = nextId++; // TODO: I know this is not thread safe
        var newJob = new Job(id, code, JobStatus.Queued);
        this.jobs.Add(id, newJob);
        this.newJobHandler?.Invoke(newJob);
        return new SubmitJsobResult(newJob.Id, newJob.Status);
    }

    public Job ReportJobStarted(int jobId)
    {
        return this.UpdateStatus(jobId, JobStatus.Progress);
    }

    public Job ReportJobSuccess(int jobId, string markdownResult)
    {
        return this.UpdateStatus(jobId, JobStatus.Complete, markdownResult, exitCode: 0);
    }

    public Job ReportJobError(int jobId, int exitCode)
    {
        return this.UpdateStatus(jobId, JobStatus.Error, exitCode: exitCode);
    }

    public void OnNewJob(Action<Job> handler)
    {
        this.newJobHandler = handler;
        
    }

    private Job UpdateStatus(int jobId, JobStatus status, string? markdownResult = null, int? exitCode = null)
    {
        var oldJob = this.jobs[jobId];
        var updatedJob = oldJob with {
            Status = status,
            MarkdownResult = markdownResult,
            ExitCode = exitCode
        };

        this.jobs[jobId] = updatedJob;
        return updatedJob;
    }
}

record SubmitJsobResult(int Id, JobStatus Status);

enum JobStatus
{
    Queued,
    Progress,
    Complete,
    Error
}

record Job(int Id, string Code, JobStatus Status, string? MarkdownResult = null, int? ExitCode = null);