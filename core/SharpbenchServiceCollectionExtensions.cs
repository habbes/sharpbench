using Sharpbench.Core;
using StackExchange.Redis;

namespace Microsoft.Extensions.DependencyInjection;

public static class SharpbenchServiceCollectionExtensions
{
    private const string REDIS_CONNECTION_STRING = "localhost";

    /// <summary>
    /// Configures core Sharpbench services.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="setupAction">Action to customize the configuration options.</param>
    public static void AddSharpbench(this IServiceCollection services, Action<SharpbenchOptions>? setupAction = null)
    {
        string? redisConnectionString = Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING");
        var options = new SharpbenchOptions()
        {
            RedisConnectionString = string.IsNullOrEmpty(redisConnectionString) ? REDIS_CONNECTION_STRING : redisConnectionString,
        };

        setupAction?.Invoke(options);

        // See: https://stackexchange.github.io/StackExchange.Redis/Configuration
        ConnectionMultiplexer redis = ConnectionMultiplexer.Connect(options.RedisConnectionString);

        // See: https://stackexchange.github.io/StackExchange.Redis/Basics
        services.AddSingleton<IConnectionMultiplexer>(redis);
        services.AddSingleton<IDatabase>(sp => sp.GetRequiredService<IConnectionMultiplexer>().GetDatabase());
        services.AddSingleton<ISubscriber>(sp => sp.GetRequiredService<IConnectionMultiplexer>().GetSubscriber());
        services.AddSingleton<IJobQueue, JobQueue>();
        services.AddSingleton<IJobRepository, JobRepository>();
        services.AddSingleton<IJobMessageStream, JobMessageStream>();
    }
}
