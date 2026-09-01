namespace RealEstate.Infrastructure.Caching;

public class RedisSettings
{
    public const string SectionName = "Redis";

    public string ConnectionString { get; set; } = string.Empty;
    public string InstanceName { get; set; } = "realestate:";
    public int DefaultTtlMinutes { get; set; } = 10;
}
