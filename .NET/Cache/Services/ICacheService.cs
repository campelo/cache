namespace Cache.Services;

public interface ICacheService
{
    Task<TItem?> GetValueAsync<TItem>(string key);
    
    Task SetAsync<TItem>(string key, TItem value, TimeSpan? absoluteExpirationRelativeToNow);
    
    Task SetAsync<TItem>(string key, TItem value);
    
    Task RemoveAsync(string key);
    
    Task<(bool Success, TItem? ResultItem)> TryGetValueAsync<TItem>(string key);
}

