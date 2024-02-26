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

    public void OnNewJob(Action<Job> handler)
    {
        this.newJobHandler = handler;
    }

    private Job UpdateStatus(int jobId, JobStatus status)
    {
        var oldJob = this.jobs[jobId];
        var updatedJob = oldJob with {
            Status = status
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

record Job(int Id, string Code, JobStatus Status);