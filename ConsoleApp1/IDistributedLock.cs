namespace ConsoleApp1;

// 分布式锁接口，用于跨节点的互斥控制。
public interface IDistributedLock
{
    Task<IDisposable> AcquireAsync(string key, CancellationToken cancellationToken = default);
}
