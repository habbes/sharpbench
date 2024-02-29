﻿using Sharpbench.Core;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Extensions.DependencyInjection;

public static class SharpbenchServiceCollectionExtensions
{
    private const string DEFAULT_REDIS_CONNECTION_STRING = "localhost";
    public static void AddSharpbench(this IServiceCollection services, Action<SharpbenchOptions>? setupAction = null)
    {
        string? redisConnString = Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING");
        var options = new SharpbenchOptions()
        {
            RedisConnectionString = string.IsNullOrEmpty(redisConnString) ? DEFAULT_REDIS_CONNECTION_STRING : redisConnString,
        };

        setupAction?.Invoke(options);

        ConnectionMultiplexer redis = ConnectionMultiplexer.Connect(DEFAULT_REDIS_CONNECTION_STRING);

        services.AddSingleton<IConnectionMultiplexer>(redis);
        services.AddSingleton<IDatabase>(sp => sp.GetRequiredService<IConnectionMultiplexer>().GetDatabase());
        services.AddSingleton<ISubscriber>(sp => sp.GetRequiredService<IConnectionMultiplexer>().GetSubscriber());
        services.AddSingleton<IJobQueue, JobQueue>();
        services.AddSingleton<IJobRepository, JobRepository>();
        services.AddSingleton<IJobMessageStream, JobMessageStream>();
    }
    
}