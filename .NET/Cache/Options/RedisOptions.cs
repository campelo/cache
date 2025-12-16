namespace Cache.Options;

public class RedisOptions
{
    public string ConnectionString { get; set; } = "localhost:6379";
    public bool UseRedis { get; set; } = false;
}
