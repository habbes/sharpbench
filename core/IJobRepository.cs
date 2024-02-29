namespace Sharpbench.Core;

public interface IJobRepository
{
    Task<SubmitJobResult> SubmitJob(string code);
    Task<Job> GetJob(string Id);
    Task<Job> ReportJobStarted(string jobId);
    Task<Job> ReportJobSuccess(string jobId, string markdownResult);
    Task<Job> ReportJobError(string jobId, int exitCode);
}