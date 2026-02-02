using StackExchange.Redis;

namespace ConsoleApp1;

// Redis 客户端简单实现（基于 StackExchange.Redis）。
public sealed class SimpleRedisClient : IRedisClient
{
    private readonly IDatabase _db;

    public SimpleRedisClient(string connectionString)
    {
        var connection = ConnectionMultiplexer.Connect(connectionString);
        _db = connection.GetDatabase();
    }

    public async Task<bool> SetNxAsync(string key, string value, TimeSpan expiry)
    {
        return await _db.StringSetAsync(key, value, expiry, When.NotExists);
    }

    public async Task<bool> DeleteIfEqualsAsync(string key, string expectedValue)
    {
        var transaction = _db.CreateTransaction();
        transaction.AddCondition(Condition.StringEqual(key, expectedValue));
        transaction.KeyDeleteAsync(key);
        return await transaction.ExecuteAsync();
    }
}
