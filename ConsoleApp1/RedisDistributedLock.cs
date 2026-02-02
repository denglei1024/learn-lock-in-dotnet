namespace ConsoleApp1;

// Redis 分布式锁（用于多机场景，需要 Redis 服务支撑）。
// 基于 SET NX PX 命令实现，带自动过期机制。
public sealed class RedisDistributedLock : IDistributedLock
{
    private readonly IRedisClient _client;
    private readonly TimeSpan _lockExpiry;
    private readonly TimeSpan _retryDelay;

    public RedisDistributedLock(IRedisClient client, TimeSpan? lockExpiry = null, TimeSpan? retryDelay = null)
    {
        _client = client;
        _lockExpiry = lockExpiry ?? TimeSpan.FromSeconds(30);
        _retryDelay = retryDelay ?? TimeSpan.FromMilliseconds(50);
    }

    public async Task<IDisposable> AcquireAsync(string key, CancellationToken cancellationToken = default)
    {
        var lockKey = $"lock:{key}";
        var lockValue = Guid.NewGuid().ToString();
        var deadline = DateTimeOffset.UtcNow.Add(_lockExpiry.Multiply(10));

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // 尝试用 SET NX PX 原子性地创建锁。
            var acquired = await _client.SetNxAsync(lockKey, lockValue, _lockExpiry);
            if (acquired)
            {
                return new RedisReleaser(_client, lockKey, lockValue);
            }

            if (DateTimeOffset.UtcNow >= deadline)
            {
                throw new TimeoutException($"Failed to acquire Redis lock for {key}");
            }

            await Task.Delay(_retryDelay, cancellationToken);
        }
    }

    private sealed class RedisReleaser : IDisposable
    {
        private readonly IRedisClient _client;
        private readonly string _lockKey;
        private readonly string _lockValue;
        private int _disposed;

        public RedisReleaser(IRedisClient client, string lockKey, string lockValue)
        {
            _client = client;
            _lockKey = lockKey;
            _lockValue = lockValue;
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            // 只有持有该锁值才删除（防止误删他人的锁）。
            _client.DeleteIfEqualsAsync(_lockKey, _lockValue).ConfigureAwait(false).GetAwaiter().GetResult();
        }
    }
}

// Redis 客户端接口（便于单测和替换）。
public interface IRedisClient
{
    Task<bool> SetNxAsync(string key, string value, TimeSpan expiry);
    Task<bool> DeleteIfEqualsAsync(string key, string expectedValue);
}
