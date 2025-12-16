using Cache.Options;
using Cache.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;

var builder = Host.CreateApplicationBuilder(args);

// Load appsettings.json
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

// Configure cache options from configuration
builder.Services.Configure<CacheOptions>(builder.Configuration.GetSection("Cache"));

// Configure Redis options from configuration
builder.Services.Configure<RedisOptions>(builder.Configuration.GetSection("Redis"));

// Register cache service based on configuration
var redisOptions = builder.Configuration.GetSection("Redis").Get<RedisOptions>() ?? new RedisOptions();

if (redisOptions.UseRedis)
{
    Console.WriteLine("=== Using Redis Cache ===\n");

    // Configure Redis connection
    var redisConnection = ConnectionMultiplexer.Connect(redisOptions.ConnectionString);
    builder.Services.AddSingleton<IConnectionMultiplexer>(redisConnection);
    builder.Services.AddSingleton<ICacheService, RedisCacheService>();
}
else
{
    Console.WriteLine("=== Using Memory Cache ===\n");

    builder.Services.AddMemoryCache();
    builder.Services.AddSingleton<ICacheService, MemoryCacheService>();
}

var host = builder.Build();

var cacheService = host.Services.GetRequiredService<ICacheService>();

Console.WriteLine("=== Cache Lifetime Test (1 minute) ===\n");

const string value1Key = "value1";
const string value2Key = "value2";
const string value3Key = "value3";
const string initialValue1 = "value1";
const string initialValue2 = "value2";
const string initialValue3 = "value3";

Console.WriteLine($"Initial {value1Key} = {await cacheService.TryGetValueAsync<string>(value1Key)}");
Console.WriteLine($"Initial {value2Key} = {await cacheService.TryGetValueAsync<string>(value2Key)}");
Console.WriteLine($"Initial {value3Key} = {await cacheService.TryGetValueAsync<string>(value3Key)}");

Console.WriteLine($"\nSetting {value1Key} = '{initialValue1}' and {value2Key} = '{initialValue2}' at the same time...");
await cacheService.SetAsync(value1Key, initialValue1);
await cacheService.SetAsync(value2Key, initialValue2);
await cacheService.SetAsync(value3Key, initialValue3);

int value2UpdateCounter = 0;
int value3UpdateCounter = 0;

int totalSeconds = 10;
int checkIntervalSeconds = 1;
int elapsedSeconds = 0;

while (elapsedSeconds <= totalSeconds)
{
    Console.WriteLine($"=== Check at {elapsedSeconds} seconds ===");

    var (value1Exists, value1Result) = await cacheService.TryGetValueAsync<string>(value1Key);
    if (value1Exists)
    {
        Console.WriteLine($"{value1Key}: '{value1Result}' (unchanged)");
    }
    else
    {
        Console.WriteLine($"{value1Key}: EXPIRED (not in cache)");
    }

    var (value2Exists, value2Result) = await cacheService.TryGetValueAsync<string>(value2Key);
    if (value2Exists)
    {
        Console.WriteLine($"{value2Key}: '{value2Result}'");

        value2UpdateCounter++;
        string updatedValue2 = $"{initialValue2}+{value2UpdateCounter}";
        await cacheService.SetAsync(value2Key, updatedValue2);
        Console.WriteLine($"  -> Updated to '{updatedValue2}'");
    }
    else
    {
        Console.WriteLine($"{value2Key}: EXPIRED (not in cache)");
    }

    var (value3Exists, value3Result) = await cacheService.TryGetValueAsync<string>(value3Key);
    if (value3Exists)
    {
        Console.WriteLine($"{value3Key}: '{value3Result}'");

        if (value3UpdateCounter < 3)
        {
            value3UpdateCounter++;
            string updatedValue3 = $"{initialValue3}+{value3UpdateCounter}";
            await cacheService.SetAsync(value3Key, updatedValue3);
            Console.WriteLine($"  -> Updated to '{updatedValue3}'");
        }
    }
    else
    {
        Console.WriteLine($"{value3Key}: EXPIRED (not in cache)");
    }

    Console.WriteLine();

    if (elapsedSeconds < totalSeconds)
    {
        await Task.Delay(checkIntervalSeconds * 1000);
        elapsedSeconds += checkIntervalSeconds;
    }
    else
    {
        break;
    }
}

Console.WriteLine("=== Test Complete ===");

