using Cache.Options;
using Cache.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<CacheOptions>(options =>
{
    options.CacheLifetime = "00:00:05";
    options.CacheLifeTimeSpan = TimeSpan.FromMinutes(1);
});

builder.Services.AddMemoryCache();

builder.Services.AddSingleton<ICacheService, MemoryCacheService>();

var host = builder.Build();

var cacheService = host.Services.GetRequiredService<ICacheService>();

Console.WriteLine("=== Cache Lifetime Test (1 minute) ===\n");

const string value1Key = "value1";
const string value2Key = "value2";
const string value3Key = "value3";
const string initialValue1 = "value1";
const string initialValue2 = "value2";
const string initialValue3 = "value3";

Console.WriteLine($"Setting {value1Key} = '{initialValue1}' and {value2Key} = '{initialValue2}' at the same time...");
await cacheService.SetAsync(value1Key, initialValue1, TimeSpan.FromSeconds(5));
await cacheService.SetAsync(value2Key, initialValue2, TimeSpan.FromSeconds(5));
await cacheService.SetAsync(value3Key, initialValue3, TimeSpan.FromSeconds(5));

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

