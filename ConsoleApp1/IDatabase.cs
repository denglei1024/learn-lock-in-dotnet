namespace ConsoleApp1;

// 数据库访问接口，用于回源查询。
public interface IDatabase
{
    Task<string> QueryAsync(string key);
}
