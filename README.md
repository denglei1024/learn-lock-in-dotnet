# ConsoleApp1 - 分布式缓存系统

一个演示从**单体高并发缓存** → **分布式缓存**的完整实现，包含三种分布式锁方案。

## 核心特性

- ✅ **一致性哈希**：稳定地把 key 路由到特定节点
- ✅ **布隆过滤器**：快速判断是否可能存在（防缓存穿透）
- ✅ **分布式锁**：防缓存击穿，支持多进程/多机场景
- ✅ **防护机制**：缓存穿透、缓存击穿、缓存雪崩的完整解决方案

## 三种分布式锁方案对比

| 方案 | 实现 | 适用场景 | 优点 | 缺点 |
|------|------|--------|------|------|
| **NamedMutexDistributedLock** | 命名互斥体 | 同机多进程 | 简单、系统原生、无外部依赖 | 仅限 Windows；不支持跨机 |
| **FileDistributedLock** | 文件系统锁 | 多机（NAS/网络共享） | 支持跨机、无外部依赖、基于共享存储 | 网络延迟影响；需要共享存储 |
| **RedisDistributedLock** | Redis SET NX PX | 多机分布式（常用） | 高性能、支持自动过期、Redlock 可靠 | 需要 Redis 服务；故障转移需特殊处理 |

## 使用示例

### 方案 1：同机多进程（命名互斥体）

```csharp
var mutexLock = new NamedMutexDistributedLock("AppName");
using (await mutexLock.AcquireAsync("order:1001"))
{
    // 同一机器上的其他进程会被阻塞
    Console.WriteLine("获得锁，执行业务逻辑");
}
// 自动释放
```

**启动多个进程演示：**
```powershell
# 终端 1
dotnet run --project ConsoleApp1.csproj

# 终端 2
dotnet run --project ConsoleApp1.csproj
# 会被阻塞，直到终端 1 释放锁
```

### 方案 2：多机分布式（共享目录/NAS）

```csharp
// 本地测试
var fileLock = new FileDistributedLock(@"C:\DistributedLocks");

// 网络共享（Windows）
var fileLock = new FileDistributedLock(@"\\nas-server\shared\locks");

// NFS 挂载（Linux）
var fileLock = new FileDistributedLock("/mnt/nfs/locks");

using (await fileLock.AcquireAsync("order:1001"))
{
    // 多台机器上的代码会互斥执行
    Console.WriteLine("获得分布式锁");
}
```

**工作原理：**
- 锁文件路径：`{lockDirectory}/order:1001.lock`
- 所有机器都能访问这个共享目录
- 创建锁文件时，`FileStream` 以独占模式打开，实现互斥
- 建议配置网络共享的访问权限

### 方案 3：多机分布式（Redis）

```csharp
// 连接 Redis
var client = new SimpleRedisClient("localhost:6379");
var redisLock = new RedisDistributedLock(client, 
    lockExpiry: TimeSpan.FromSeconds(30));

using (await redisLock.AcquireAsync("order:1001"))
{
    // 跨地域、跨数据中心都能互斥
    Console.WriteLine("获得 Redis 分布式锁");
}
```

**启动 Redis 服务：**
```powershell
# Docker
docker run -d -p 6379:6379 redis:latest

# 或本地安装
redis-server
```

## 运行演示

```powershell
# 安装依赖（自动）
dotnet restore

# 运行示例
dotnet run --project ConsoleApp1\ConsoleApp1.csproj
```

## 架构流程

```
┌─────────────────────────────────────────────────┐
│ CacheCluster (一致性哈希路由)                      │
└──────────────────┬──────────────────────────────┘
                   │
       ┌───────────┼───────────┐
       ▼           ▼           ▼
┌─────────────┐┌─────────────┐┌─────────────┐
│ Node 1      ││ Node 2      ││ Node 3      │
│ Cache       ││ Cache       ││ Cache       │
│ BloomFilter ││ BloomFilter ││ BloomFilter │
└─────────────┘└─────────────┘└─────────────┘
       │           │           │
       └───────────┼───────────┘
                   │
    ┌──────────────┴──────────────┐
    ▼                             ▼
┌──────────────┐          ┌──────────────┐
│ DLock        │          │ Database     │
│ (防击穿)      │          │ (回源查询)    │
└──────────────┘          └──────────────┘
```

## 防护机制说明

| 问题 | 原因 | 解决方案 |
|------|------|--------|
| **缓存穿透** | 查询数据库不存在的 key，每次都回源 | 布隆过滤器 + 空值缓存 |
| **缓存击穿** | 热 key 过期，大量请求同时回源 | 分布式锁（只让一个请求回源） |
| **缓存雪崩** | 大量 key 同时过期 | 随机 TTL + 缓存预热 |

## 文件结构

```
ConsoleApp1/
├── ICache.cs                      # 缓存接口
├── IDatabase.cs                   # 数据库接口
├── IBloomFilter.cs                # 布隆过滤器接口
├── IDistributedCache.cs           # 分布式缓存接口
├── IDistributedLock.cs            # 分布式锁接口
├── IRedisClient.cs                # Redis 客户端接口
│
├── CacheNode.cs                   # 单个缓存节点
├── CacheCluster.cs                # 分布式缓存集群
├── ConsistentHashRing.cs          # 一致性哈希环
│
├── InMemoryCache.cs               # 进程内缓存实现
├── SimpleBloomFilter.cs            # 布隆过滤器实现
├── FakeDatabase.cs                # 测试用数据库
│
├── InMemoryDistributedLock.cs     # 进程内锁（仅演示）
├── NamedMutexDistributedLock.cs   # 同机多进程锁 ✓
├── FileDistributedLock.cs         # 多机文件锁 ✓
├── RedisDistributedLock.cs        # 多机 Redis 锁 ✓
├── SimpleRedisClient.cs           # Redis 客户端实现
│
├── Program.cs                     # 示例程序
└── ConsoleApp1.csproj             # 项目配置
```

## 下一步改进

- [ ] 实现 Redis Redlock 算法（更强的一致性保证）
- [ ] 支持 ZooKeeper/etcd 作为锁服务
- [ ] 添加单元测试（多进程/多机竞争测试）
- [ ] 集成监控和日志
- [ ] 支持锁超时告警

