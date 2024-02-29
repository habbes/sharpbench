namespace Sharpbench.Core;

public class SharpbenchOptions
{
    /// <summary>
    /// Connection string used to connect to the Redis server. Examples:
    /// 
    /// - localhost
    /// - host:port,user=username,password=password
    /// - rediserver.example.com:324324,user=dsfdsf,password=dsfdsfsfdsfd
    /// </summary>
    public string? RedisConnectionString { get; set; }
}
