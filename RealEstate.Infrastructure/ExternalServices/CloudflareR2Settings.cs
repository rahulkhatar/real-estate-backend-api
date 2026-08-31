namespace RealEstate.Infrastructure.ExternalServices;

public class CloudflareR2Settings
{
    public const string SectionName = "CloudflareR2";

    public string AccountId { get; set; } = string.Empty;
    public string AccessKeyId { get; set; } = string.Empty;
    public string SecretAccessKey { get; set; } = string.Empty;
    public string BucketName { get; set; } = string.Empty;

    /// <summary>Public base URL the bucket is served from (e.g. the r2.dev subdomain or a custom domain), no trailing slash.</summary>
    public string PublicBaseUrl { get; set; } = string.Empty;

    public string ServiceUrl => $"https://{AccountId}.r2.cloudflarestorage.com";
}
