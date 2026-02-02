namespace ConsoleApp1;

// 布隆过滤器接口，用于快速判断是否可能存在。
public interface IBloomFilter
{
    bool MightContain(string key);
    void Add(string key);
}