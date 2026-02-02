namespace ConsoleApp1;

// 缓存节点描述，包含节点名、缓存和布隆过滤器。
public sealed class CacheNode
{
    public CacheNode(string name, ICache cache, IBloomFilter bloomFilter)
    {
        Name = name;
        Cache = cache;
        BloomFilter = bloomFilter;
    }

    public string Name { get; }
    public ICache Cache { get; }
    public IBloomFilter BloomFilter { get; }
}
