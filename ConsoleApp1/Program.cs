using ConsoleApp1;

Console.WriteLine("=== 分布式缓存锁演示 ===\n");

var key = "order:1001";

// 方案 1：同机多进程锁（命名互斥体）。
Console.WriteLine("【方案 1】同机多进程锁 - 命名互斥体");
Console.WriteLine("使用场景：同一台服务器上的多个进程需要互斥");
var mutexLock = new NamedMutexDistributedLock("ConsoleApp1");
try
{
    using (await mutexLock.AcquireAsync(key))
    {
        Console.WriteLine($"✓ 成功获得锁: {key}");
        await Task.Delay(100);
    }
    Console.WriteLine("✓ 锁已释放\n");
}
catch (Exception ex)
{
    Console.WriteLine($"✗ 锁获取失败: {ex.Message}\n");
}

// 方案 2：多机分布式锁 - 基于文件系统（共享目录）。
Console.WriteLine("【方案 2】多机分布式锁 - 文件系统");
Console.WriteLine("使用场景：多台机器共享一个文件系统（网络共享/NFS）");
Console.WriteLine("配置示例：");
Console.WriteLine("  - 本地测试: new FileDistributedLock(@\"C:\\Locks\")");
Console.WriteLine("  - 网络共享: new FileDistributedLock(@\"\\\\server\\share\\locks\")");
Console.WriteLine("  - NFS 挂载: new FileDistributedLock(\"/mnt/shared/locks\")\n");

var sharedDir = Path.Combine(Path.GetTempPath(), "ConsoleApp1Locks");
var fileLock = new FileDistributedLock(sharedDir);
try
{
    using (await fileLock.AcquireAsync(key))
    {
        Console.WriteLine($"✓ 成功获得文件锁: {key}");
        Console.WriteLine($"  锁目录: {sharedDir}");
        await Task.Delay(100);
    }
    Console.WriteLine("✓ 文件锁已释放\n");
}
catch (Exception ex)
{
    Console.WriteLine($"✗ 文件锁获取失败: {ex.Message}\n");
}

// 方案 3：多机分布式锁 - 基于 Redis（真正的分布式）。
Console.WriteLine("【方案 3】多机分布式锁 - Redis");
Console.WriteLine("使用场景：跨地域、跨数据中心的分布式系统");
Console.WriteLine("需要 Redis 支持，例如:");
Console.WriteLine("  var client = new SimpleRedisClient(\"localhost:6379\")");
Console.WriteLine("  var redisLock = new RedisDistributedLock(client);\n");
Console.WriteLine("备注: Redis 示例需要本地 Redis 服务运行，跳过演示\n");

// 如果有 Redis，可以这样使用：
// try
// {
//     var redisClient = new SimpleRedisClient("localhost:6379");
//     var redisLock = new RedisDistributedLock(redisClient);
//     using (await redisLock.AcquireAsync(key))
//     {
//         Console.WriteLine($"✓ 成功获得 Redis 锁: {key}");
//         await Task.Delay(100);
//     }
//     Console.WriteLine("✓ Redis 锁已释放\n");
// }
// catch (Exception ex)
// {
//     Console.WriteLine($"✗ Redis 锁获取失败: {ex.Message}\n");
// }

Console.WriteLine("=== 演示完成 ===");
