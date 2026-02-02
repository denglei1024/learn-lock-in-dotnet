namespace ConsoleApp1;

// 通过一致性哈希把请求路由到具体节点。
public sealed class CacheCluster : IDistributedCache
{
    private readonly ConsistentHashRing<CacheNode> _ring;

    public CacheCluster(IEnumerable<CacheNode> nodes, int replicas = 64)
    {
        _ring = new ConsistentHashRing<CacheNode>(nodes, node => node.Name, replicas);
    }

    public Task<string> GetAsync(string key)
    {
        var node = _ring.ResolveNode(key);
        return node.Cache.GetAsync(key);
    }

    public async Task SetAsync(string key, string value, TimeSpan ttl)
    {
        var node = _ring.ResolveNode(key);
        await node.Cache.SetAsync(key, value, ttl);
        // 写缓存后更新节点级布隆过滤器。
        node.BloomFilter.Add(key);
    }

    public bool MightContain(string key)
    {
        var node = _ring.ResolveNode(key);
        return node.BloomFilter.MightContain(key);
    }
}
