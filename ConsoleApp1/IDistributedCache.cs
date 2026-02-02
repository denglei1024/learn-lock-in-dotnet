namespace ConsoleApp1;

// 分布式缓存接口，屏蔽节点路由细节。
public interface IDistributedCache
{
    Task<string> GetAsync(string key);
    Task SetAsync(string key, string value, TimeSpan ttl);
    bool MightContain(string key);
}
