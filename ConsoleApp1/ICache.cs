namespace ConsoleApp1;

// 本地缓存接口，封装读写与过期时间。
public interface ICache
{
    Task<string> GetAsync(string key);
    Task SetAsync(string key, string dbData, TimeSpan fromMicroseconds);
}