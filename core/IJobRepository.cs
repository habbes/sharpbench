namespace Sharpbench.Core;

public interface IJobRepository
{
    Task<Job> SubmitJob(string Code);
    Task<Job> GetJob(int Id);
}