using Sharpbench.Core;
using StackExchange.Redis;

namespace Microsoft.Extensions.DependencyInjection;

public static class SharpbenchServiceCollectionExtensions
{
    //private const string REDIS_CONNECTION_STRING = "localhost";

    /// <summary>
    /// Configures core Sharpbench services.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="setupAction">Action to customize the configuration options.</param>
    public static void AddSharpbench(this IServiceCollection services)
    {
        // See: https://stackexchange.github.io/StackExchange.Redis/Basics
        services.AddSingleton<IDatabase>(sp => sp.GetRequiredService<IConnectionMultiplexer>().GetDatabase());
        services.AddSingleton<ISubscriber>(sp => sp.GetRequiredService<IConnectionMultiplexer>().GetSubscriber());
        services.AddSingleton<IJobQueue, JobQueue>();
        services.AddSingleton<IJobRepository, JobRepository>();
        services.AddSingleton<IJobMessageStream, JobMessageStream>();
    }
}
