using System.Collections.Concurrent;

namespace ConsoleApp1;

// 使用命名互斥体实现的同机多进程锁。
public sealed class NamedMutexDistributedLock : IDistributedLock
{
    private readonly string _mutexPrefix;
    private readonly ConcurrentDictionary<string, Mutex> _mutexes = new();

    public NamedMutexDistributedLock(string mutexPrefix = "ConsoleApp1")
    {
        _mutexPrefix = mutexPrefix;
    }

    public Task<IDisposable> AcquireAsync(string key, CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            var mutexName = $"Global\\{_mutexPrefix}:{key}";
            var mutex = _mutexes.GetOrAdd(mutexName, name => new Mutex(false, name));

            try
            {
                WaitHandle.WaitAny(new[] { mutex, cancellationToken.WaitHandle });
                cancellationToken.ThrowIfCancellationRequested();
                return (IDisposable)new MutexReleaser(mutex);
            }
            catch (AbandonedMutexException)
            {
                // 发生异常也视为已获得锁，继续执行。
                return (IDisposable)new MutexReleaser(mutex);
            }
        }, cancellationToken);
    }

    private sealed class MutexReleaser : IDisposable
    {
        private readonly Mutex _mutex;
        private int _disposed;

        public MutexReleaser(Mutex mutex)
        {
            _mutex = mutex;
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            _mutex.ReleaseMutex();
        }
    }
}
