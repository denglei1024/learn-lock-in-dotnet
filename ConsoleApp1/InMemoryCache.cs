using System.Collections.Concurrent;

namespace ConsoleApp1;

// 简单的内存缓存实现，带过期时间。
public sealed class InMemoryCache : ICache
{
    private readonly ConcurrentDictionary<string, CacheEntry> _entries = new();

    public Task<string> GetAsync(string key)
    {
        if (_entries.TryGetValue(key, out var entry) && !entry.IsExpired())
        {
            return Task.FromResult(entry.Value);
        }

        _entries.TryRemove(key, out _);
        return Task.FromResult<string>(null);
    }

    public Task SetAsync(string key, string dbData, TimeSpan ttl)
    {
        var entry = new CacheEntry(dbData, DateTimeOffset.UtcNow.Add(ttl));
        _entries[key] = entry;
        return Task.CompletedTask;
    }

    private sealed record CacheEntry(string Value, DateTimeOffset ExpiresAt)
    {
        public bool IsExpired() => DateTimeOffset.UtcNow >= ExpiresAt;
    }
}
