using Cache.Options;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Text.Json;

namespace Cache.Services;

public class RedisCacheService : ICacheService
{
    private readonly IDatabase _redisDatabase;
    private readonly CacheOptions _options;

    public RedisCacheService(IConnectionMultiplexer connectionMultiplexer, IOptions<CacheOptions> options)
    {
        _redisDatabase = connectionMultiplexer.GetDatabase();
        _options = options.Value;
    }

    public async Task<TItem?> GetValueAsync<TItem>(string key)
    {
        var (success, resultItem) = await TryGetValueAsync<TItem>(key);
        return success ? resultItem : default;
    }

    public async Task SetAsync<TItem>(string key, TItem value, TimeSpan? absoluteExpirationRelativeToNow) =>
        await SetAsync(key, value, absoluteExpirationRelativeToNow, true);

    public async Task SetAsync<TItem>(string key, TItem value, TimeSpan? absoluteExpirationRelativeToNow, bool keepTtl = true)
    {
        if (value == null)
        {
            await RemoveAsync(key);
            return;
        }

        absoluteExpirationRelativeToNow ??= TryParseTimeSpan(_options.CacheLifetime, out TimeSpan ts)
            ? ts
            : _options.CacheLifeTimeSpan;

        if (absoluteExpirationRelativeToNow.Value < TimeSpan.FromSeconds(1))
            return;

        // Check if key exists
        bool keyExists = await _redisDatabase.KeyExistsAsync(key);
        var serializedValue = SerializeValue(value);

        if (keyExists && keepTtl)
        {
            await _redisDatabase.StringSetAsync(key, serializedValue, keepTtl: keepTtl);
            return;
        }

        // Set value with expiration
        await _redisDatabase.StringSetAsync(key, serializedValue, absoluteExpirationRelativeToNow);
    }

    public Task SetAsync<TItem>(string key, TItem value) =>
        SetAsync(key, value, null);

    public async Task RemoveAsync(string key)
    {
        await _redisDatabase.KeyDeleteAsync(key);
    }

    public async Task<(bool Success, TItem? ResultItem)> TryGetValueAsync<TItem>(string key)
    {
        try
        {
            var value = await _redisDatabase.StringGetAsync(key);

            if (!value.HasValue || value.IsNullOrEmpty)
            {
                return (false, default);
            }

            var deserializedValue = DeserializeValue<TItem>(value!);
            return (true, deserializedValue);
        }
        catch (Exception)
        {
            return (false, default);
        }
    }

    private static string SerializeValue<TItem>(TItem value)
    {
        if (value is string stringValue)
        {
            return stringValue;
        }

        return JsonSerializer.Serialize(value);
    }

    private static TItem? DeserializeValue<TItem>(string value)
    {
        if (typeof(TItem) == typeof(string))
        {
            return (TItem)(object)value;
        }

        return JsonSerializer.Deserialize<TItem>(value);
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
