namespace Redis.Orm;

public class RedisOptions
{
    public bool Enabled { get; set; }
    public string? Connection { get; set; }
    public long MaxMemory { get; set; }
    public TimeSpan ExpirationTime { get; set; }
    public int MaxBatchSize { get; set; }
}