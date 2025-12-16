using Cache.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Cache.Services;

public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly CacheOptions _options;

    public MemoryCacheService(IMemoryCache memoryCache, IOptions<CacheOptions> options)
    {
        _memoryCache = memoryCache;
        _options = options.Value;
    }

    public async Task<TItem?> GetValueAsync<TItem>(string key) =>
        (await TryGetValueAsync<TItem?>(key)).ResultItem;

    public Task SetAsync<TItem>(string key, TItem value, TimeSpan? absoluteExpirationRelativeToNow)
    {
        absoluteExpirationRelativeToNow ??= TryParseTimeSpan(_options.CacheLifetime, out TimeSpan ts)
            ? ts
            : _options.CacheLifeTimeSpan;
        if (absoluteExpirationRelativeToNow.Value < TimeSpan.FromSeconds(1))
            return Task.CompletedTask;
        _memoryCache.Set(key, value, absoluteExpirationRelativeToNow.Value);
        return Task.CompletedTask;
        //bool keyExists = _memoryCache.TryGetValue(key, out _);

        //if (keyExists)
        //{
        //    _memoryCache.Set(key, value);
        //    return Task.CompletedTask;
        //}



        //using var entry = _memoryCache.CreateEntry(key);
        //entry.Value = value;
        //entry.AbsoluteExpirationRelativeToNow = absoluteExpirationRelativeToNow.Value;

        //return Task.CompletedTask;
    }

    public Task SetAsync<TItem>(string key, TItem value) =>
        SetAsync(key, value, null);

    public Task RemoveAsync(string key)
    {
        _memoryCache.Remove(key);
        return Task.CompletedTask;
    }

    public async Task<(bool Success, TItem? ResultItem)> TryGetValueAsync<TItem>(string key)
    {
        TItem? item = default;
        try
        {
            bool success = await Task.FromResult(_memoryCache.TryGetValue(key, out item));
            return (success, item);
        }
        catch (Exception)
        {
            return (false, item);
        }
    }

    private static bool TryParseTimeSpan(string? value, out TimeSpan timeSpan)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            timeSpan = default;
            return false;
        }

        return TimeSpan.TryParse(value, out timeSpan);
    }
}

