namespace RealEstate.Infrastructure.Caching;

public class CacheSettings
{
    public const string SectionName = "Cache";

    public int DefaultTtlMinutes { get; set; } = 10;
}
