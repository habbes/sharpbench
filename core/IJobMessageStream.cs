namespace Sharpbench.Core;

public interface IJobMessageStream
{
    Task PublishMessage(JobMessage message);
    IAsyncEnumerable<JobMessage> ListenForMessages(CancellationToken cancellationToken);
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
