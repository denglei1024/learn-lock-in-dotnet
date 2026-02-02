using System.Collections.Concurrent;

namespace ConsoleApp1;

// 单节点缓存服务，展示击穿/穿透的防护流程。
public class CacheService
{
    private readonly ICache _cache;
    private readonly IDatabase _database;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _keyLocks;
    private readonly IBloomFilter _bloomFilter;

    public CacheService(IBloomFilter bloomFilter, IDatabase database, ICache cache)
    {
        _bloomFilter = bloomFilter;
        _keyLocks = new ConcurrentDictionary<string, SemaphoreSlim>();
        _database = database;
        _cache = cache;
    }

    public async Task<string> GetDataAsync(string key)
    {
        // 先查缓存，命中直接返回。
        var cacheData = await _cache.GetAsync(key);
        if (cacheData != null)
        {
            return cacheData;
        }
        
        // 布隆过滤器判断是否存在。
        if (!_bloomFilter.MightContain(key))
        {
            return null; // 数据库中不存在，直接返回空。
        }
        
        // 获取对应 key 的锁，防止缓存击穿。
        var keyLock = _keyLocks.GetOrAdd(key, k => new SemaphoreSlim(1, 1));

        // 加锁，防止缓存击穿。
        await keyLock.WaitAsync();

        try
        {
            // 再次查缓存，避免并发下重复回源。
            cacheData = await _cache.GetAsync(key);
            if (cacheData != null)
            {
                return cacheData;
            }

            // 查询数据库。
            var dbData = await _database.QueryAsync(key);
            if (dbData != null)
            {
                // 写缓存。
                await _cache.SetAsync(key, dbData, TimeSpan.FromMicroseconds(30));
            }
            else
            {
                // 防止缓存穿透，写入空值缓存。
                await _cache.SetAsync(key,"nil", TimeSpan.FromMicroseconds(5));
            }
            return dbData;
        }
        catch (Exception e)
        {
            keyLock.Dispose();
            if (_keyLocks.TryRemove(key, out SemaphoreSlim semaphore))
            {
                semaphore?.Release();
            }
            throw;
        }
    }
}