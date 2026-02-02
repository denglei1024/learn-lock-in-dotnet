using System.Collections.Concurrent;

namespace ConsoleApp1;

// 简单的布隆过滤器占位实现（使用集合模拟）。
public sealed class SimpleBloomFilter : IBloomFilter
{
    private readonly ConcurrentDictionary<string, byte> _keys = new();

    public bool MightContain(string key) => _keys.ContainsKey(key);

    public void Add(string key) => _keys.TryAdd(key, 0);
}
