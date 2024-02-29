namespace Sharpbench.Core;
using StackExchange.Redis;

public class JobRepository
{
    IDatabase db;
    public JobRepository(IDatabase redisDb)
    {
        this.db = redisDb;
    }
    public async Task<SubmitJsobResult> SubmitJob(string code)
    {
        string id = Guid.NewGuid().ToString();
        var newJob = new Job(id, code, JobStatus.Queued);
        var jobHash = this.JobToRedisHash(newJob);
        await this.db.HashSetAsync(this.GetJobKey(id), jobHash);
        // TODO: add job to queue
        return new SubmitJsobResult(newJob.Id, newJob.Status);
    }

    public async Task<Job> GetJob(string id)
    {
        var hash = await this.db.HashGetAllAsync(this.GetJobKey(id));
        var job = this.RedisHashToJob(id, hash);
        return job;
    }

    public Task<Job> ReportJobStarted(string jobId)
    {
        return this.UpdateStatus(jobId, JobStatus.Progress);
    }

    public Task<Job> ReportJobSuccess(string jobId, string markdownResult)
    {
        return this.UpdateStatus(jobId, JobStatus.Complete, markdownResult, exitCode: 0);
    }

    public Task<Job> ReportJobError(string jobId, int exitCode)
    {
        return this.UpdateStatus(jobId, JobStatus.Error, exitCode: exitCode);
    }

    private async Task<Job> UpdateStatus(string jobId, JobStatus status, string? markdownResult = null, int? exitCode = null)
    {
        HashEntry[] update =
        {
            new("Status", status.ToString()),
            new("ExitCode", exitCode),
            new("MarkdownReport", markdownResult)
        };

        await this.db.HashSetAsync(this.GetJobKey(jobId), update);
        var updatedJob = await this.GetJob(jobId);
        return updatedJob;
    }

    private HashEntry[] JobToRedisHash(Job job)
    {
        HashEntry[] hash =
        {
            new HashEntry("Id", job.Id),
            new HashEntry("Code", job.Code),
            new HashEntry("Status", job.Status.ToString()),
            new HashEntry("MarkdownReport", job.MarkdownReport),
            new HashEntry("ExitCode", job.ExitCode)
        };

        return hash;
    }

    private Job RedisHashToJob(string id, HashEntry[] hash)
    {
        var job = new Job(id);
        foreach (var entry in hash)
        {
            if (entry.Name == "Code")
            {
                job.Code = (string)entry.Value!;
            }

            if (entry.Name == "Status")
            {
                job.Status = Enum.Parse<JobStatus>(entry.Value!);
            }

            if (entry.Name == "MarkdownReport")
            {
                job.MarkdownReport = (string)entry.Value!;
            }

            if (entry.Name == "ExitCode")
            {
                job.ExitCode = (int?)entry.Value;
            }
        }

        return job;
    }

    private string GetJobKey(string id) => $"jobs:{id}";
}
