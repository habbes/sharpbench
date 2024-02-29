namespace Sharpbench.Core;

public interface IJobMessageStream
{
    Task PublishMessage(JobMessage message);
    IAsyncEnumerable<JobMessage> ListenForMessages(CancellationToken cancellationToken);
}
