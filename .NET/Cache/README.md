# Cache Service Implementation

This project demonstrates a flexible caching solution that supports both **Memory Cache** and **Redis Cache** with easy configuration-based switching.

## Features

- ✅ Memory Cache implementation (in-process caching)
- ✅ Redis Cache implementation (distributed caching)
- ✅ Configuration-based cache provider selection
- ✅ Shared interface (`ICacheService`) for both implementations
- ✅ Support for TTL (Time-To-Live) on cache entries
- ✅ Generic type support for cached values
- ✅ JSON serialization for complex types in Redis

## Configuration

### Option 1: Using appsettings.json

```json
{
  "Redis": {
    "UseRedis": false,
    "ConnectionString": "localhost:6379"
  }
}
```

### Option 2: Using Program.cs (Code-based)

```csharp
builder.Services.Configure<RedisOptions>(options =>
{
    options.UseRedis = false; // Set to true to use Redis
    options.ConnectionString = "localhost:6379";
});
```

## Switching Between Cache Providers

### Use Memory Cache (Default)
Set `UseRedis` to `false` in configuration

### Use Redis Cache
1. Set `UseRedis` to `true` in configuration
2. Ensure Redis server is running
3. Update `ConnectionString` if needed

## Running Redis Locally

### Using Docker
```bash
docker run -d --name redis-cache -p 6379:6379 redis:latest
```

### Using Redis on Windows
Download and install from: https://github.com/microsoftarchive/redis/releases

## Cache Service Interface

```csharp
public interface ICacheService
{
    Task<TItem?> GetValueAsync<TItem>(string key);
    Task SetAsync<TItem>(string key, TItem value, TimeSpan? absoluteExpirationRelativeToNow);
    Task SetAsync<TItem>(string key, TItem value);
    Task RemoveAsync(string key);
    Task<(bool Success, TItem? ResultItem)> TryGetValueAsync<TItem>(string key);
}
```

## Usage Examples

```csharp
// Get the cache service
var cacheService = host.Services.GetRequiredService<ICacheService>();

// Set a value with custom expiration
await cacheService.SetAsync("myKey", "myValue", TimeSpan.FromMinutes(5));

// Set a value with default expiration
await cacheService.SetAsync("anotherKey", "anotherValue");

// Get a value
var value = await cacheService.GetValueAsync<string>("myKey");

// Try get value (returns success flag and value)
var (exists, result) = await cacheService.TryGetValueAsync<string>("myKey");

// Remove a value
await cacheService.RemoveAsync("myKey");
```

## Key Differences

| Feature | Memory Cache | Redis Cache |
|---------|-------------|-------------|
| Scope | Single process | Distributed |
| Persistence | Lost on restart | Can be configured |
| Performance | Fastest | Fast (network overhead) |
| Scalability | Limited to process | Highly scalable |
| Use Case | Single server | Multi-server, microservices |

## Implementation Details

### RedisCacheService
- Uses `StackExchange.Redis` library
- Serializes complex types to JSON
- Supports TTL refresh on updates
- Handles string values without serialization overhead

### MemoryCacheService
- Uses `Microsoft.Extensions.Caching.Memory`
- In-process memory storage
- Automatic garbage collection

## NuGet Packages Required

```xml
<PackageReference Include="Microsoft.Extensions.Caching.Memory" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.Options" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.Hosting" Version="8.0.0" />
<PackageReference Include="StackExchange.Redis" Version="2.8.16" />
```

## Testing

The current Program.cs includes a comprehensive test that:
1. Sets three values with 5-second expiration
2. Continuously checks and updates values
3. Demonstrates cache expiration behavior
4. Shows TTL refresh on updates

## Best Practices

1. **Use Memory Cache** for single-server applications with moderate cache needs
2. **Use Redis** for distributed systems, microservices, or when cache needs to survive restarts
3. Always handle cache misses gracefully
4. Set appropriate TTL values to avoid stale data
5. Consider cache invalidation strategies for data updates
