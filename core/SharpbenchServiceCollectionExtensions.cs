using Sharpbench.Core;
using StackExchange.Redis;

namespace Microsoft.Extensions.DependencyInjection;

public static class SharpbenchServiceCollectionExtensions
{
    private const string DEFAULT_REDIS_URL = "localhost";
    public static void AddSharpbench(this IServiceCollection services, Action<SharpbenchOptions>? setupAction = null)
    {
        string? redisUrl = Environment.GetEnvironmentVariable("REDIS_URL");
        var options = new SharpbenchOptions()
        {
            RedisUrl = string.IsNullOrEmpty(redisUrl) ? DEFAULT_REDIS_URL : redisUrl,
        };

        setupAction?.Invoke(options);

        Console.WriteLine($"Attempting to connect to redis url {options.RedisUrl}");
        Console.WriteLine($"Env var was {redisUrl}");
        ConnectionMultiplexer redis = ConnectionMultiplexer.Connect(options.RedisUrl);

        services.AddSingleton<IConnectionMultiplexer>(redis);
        services.AddSingleton<IDatabase>(sp => sp.GetRequiredService<IConnectionMultiplexer>().GetDatabase());
        services.AddSingleton<ISubscriber>(sp => sp.GetRequiredService<IConnectionMultiplexer>().GetSubscriber());
        services.AddSingleton<IJobQueue, JobQueue>();
        services.AddSingleton<IJobRepository, JobRepository>();
        services.AddSingleton<IJobMessageStream, JobMessageStream>();
    }
    
}
