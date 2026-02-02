namespace ConsoleApp1;

// 基于文件锁的分布式锁（可用于共享文件系统/网络共享）。
public sealed class FileDistributedLock : IDistributedLock
{
    private readonly string _lockDirectory;
    private readonly TimeSpan _retryDelay;

    public FileDistributedLock(string lockDirectory, TimeSpan? retryDelay = null)
    {
        _lockDirectory = lockDirectory;
        _retryDelay = retryDelay ?? TimeSpan.FromMilliseconds(50);
        Directory.CreateDirectory(_lockDirectory);
    }

    public async Task<IDisposable> AcquireAsync(string key, CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(_lockDirectory, SanitizeFileName(key) + ".lock");

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var stream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                return new FileReleaser(stream, path);
            }
            catch (IOException)
            {
                await Task.Delay(_retryDelay, cancellationToken);
            }
        }
    }

    private static string SanitizeFileName(string key)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var chars = key.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray();
        return new string(chars);
    }

    private sealed class FileReleaser : IDisposable
    {
        private readonly FileStream _stream;
        private readonly string _path;
        private int _disposed;

        public FileReleaser(FileStream stream, string path)
        {
            _stream = stream;
            _path = path;
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            _stream.Dispose();
            
            // 释放后删除锁文件。
            try
            {
                File.Delete(_path);
            }
            catch
            {
                // 忽略删除失败（可能被其他进程占用）。
            }
        }
    }
}
