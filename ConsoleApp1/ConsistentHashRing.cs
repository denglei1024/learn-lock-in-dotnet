using System.Collections.Immutable;

namespace ConsoleApp1;

// 一致性哈希环，用于稳定地把 key 映射到节点。
public sealed class ConsistentHashRing<TNode>
{
    private readonly ImmutableSortedDictionary<uint, TNode> _ring;
    private readonly int _replicas;
    private readonly Func<TNode, string> _identity;

    public ConsistentHashRing(IEnumerable<TNode> nodes, Func<TNode, string> identity, int replicas = 64)
    {
        _identity = identity;
        _replicas = replicas;
        _ring = BuildRing(nodes);
    }

    public TNode ResolveNode(string key)
    {
        if (_ring.Count == 0)
        {
            throw new InvalidOperationException("No cache nodes configured.");
        }

        var hash = Hash(key);
        foreach (var kvp in _ring)
        {
            if (kvp.Key >= hash)
            {
                return kvp.Value;
            }
        }

        return _ring.First().Value;
    }

    private ImmutableSortedDictionary<uint, TNode> BuildRing(IEnumerable<TNode> nodes)
    {
        var builder = ImmutableSortedDictionary.CreateBuilder<uint, TNode>();
        foreach (var node in nodes)
        {
            var identity = _identity(node);
            for (var i = 0; i < _replicas; i++)
            {
                var vnodeKey = $"{identity}#{i}";
                var hash = Hash(vnodeKey);
                builder[hash] = node;
            }
        }

        return builder.ToImmutable();
    }

    // FNV-1a 简化哈希，速度快且分布均匀。
    private static uint Hash(string value)
    {
        unchecked
        {
            const uint fnvOffset = 2166136261;
            const uint fnvPrime = 16777619;
            var hash = fnvOffset;
            foreach (var ch in value)
            {
                hash ^= ch;
                hash *= fnvPrime;
            }

            return hash;
        }
    }
}
