using StackExchange.Redis;
using System.Collections.Concurrent;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
namespace Sharpbench.Core;

internal class JobMessageStream : IJobMessageStream
{
    ISubscriber sub;
    ConcurrentQueue<JobMessage> localBuffer = new();
    AutoResetEvent messagesAvailableSignal = new(false);
    public JobMessageStream(ISubscriber sub)
    {
        this.sub = sub;
    }

    public async IAsyncEnumerable<JobMessage> ListenForMessages([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var channel = new RedisChannel($"message:*", RedisChannel.PatternMode.Pattern);
        var remoteQueue = await this.sub.SubscribeAsync(channel);
        remoteQueue.OnMessage(m =>
        {
            var message = ToJobMessage(m);
            localBuffer.Enqueue(message);
            messagesAvailableSignal.Set();
        });

        while (!cancellationToken.IsCancellationRequested)
        {
            while (localBuffer.TryDequeue(out var message))
            {
                yield return message;
            }

            // wait for more messages to be available before checking the queue again
            messagesAvailableSignal.WaitOne(TimeSpan.FromSeconds(5));
        }
    }

    public async Task PublishMessage(JobMessage message)
    {
        var channel = new RedisChannel($"message:{message.JobId}:{message.Type}", RedisChannel.PatternMode.Literal);
        await this.sub.PublishAsync(channel, message.Data);
    }

    private static JobMessage ToJobMessage(ChannelMessage message)
    {
        string[] parts = message.Channel.ToString().Split(':', 3);
        Contract.Assert(parts.Length == 3);
        string jobId = parts[1];
        JobMessageType messageType = Enum.Parse<JobMessageType>(parts[2]);

        return new JobMessage(jobId, messageType, (byte[])message.Message!);
    }
}
