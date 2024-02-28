namespace Sharpbench.Core;

public record SubmitJsobResult(int Id, JobStatus Status);

public enum JobStatus
{
    Queued,
    Progress,
    Complete,
    Error
}

public record Job(int Id, string Code, JobStatus Status, string? MarkdownResult = null, int? ExitCode = null);
