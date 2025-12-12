namespace Cache.Options;

public class CacheOptions
{
    public string CacheLifetime { get; set; } = "00:30:00";
    public TimeSpan CacheLifeTimeSpan { get; set; } = TimeSpan.FromMinutes(30);
}

