using System.Text.Json.Serialization;

namespace Sharpbench.Core;

public record SubmitJobResult(string Id, JobStatus Status);

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum JobStatus
{
    Queued,
    Progress,
    Complete,
    Error
}

public class Job
{
    public Job(string id, string code, string clientId): this(id, code, clientId, JobStatus.Queued)
    {
    }

    public Job(string id, string code, string clientId, JobStatus status)
    {
        this.Id = id;
        this.ClientId = clientId;
        this.Code = code;
        this.Status = status;
    }

    public Job(string id) { this.Id = id; }

    public string Id { get; private set; }

    public string? ClientId { get; set; }
    public string Code { get; set; }
    public JobStatus Status { get; set; }
    public string? MarkdownReport { get; set; }
    public int? ExitCode { get; set; }
}

public record JobMessage(string JobId, JobMessageType Type, byte[] Data);

public enum JobMessageType
{
    /// <summary>
    /// A log message from stdout or stderr
    /// </summary>
    Log,
    /// <summary>
    /// A status update
    /// </summary>
    Status,
}
