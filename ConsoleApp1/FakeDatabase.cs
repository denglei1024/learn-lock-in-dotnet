using System.Collections.Concurrent;

namespace ConsoleApp1;

// 内存版数据库，便于本地演示和测试。
public sealed class FakeDatabase : IDatabase
{
    private readonly ConcurrentDictionary<string, string> _data = new();

    public FakeDatabase(Dictionary<string, string> seedData)
    {
        foreach (var kvp in seedData)
        {
            _data[kvp.Key] = kvp.Value;
        }
    }

    public Task<string> QueryAsync(string key)
    {
        _data.TryGetValue(key, out var value);
        return Task.FromResult(value);
    }
}
