using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using RealEstate.Application.Interfaces;
using RealEstate.Core.ValueObjects;

namespace RealEstate.Infrastructure.ExternalServices;

/// <summary>
/// Stores images in a Cloudflare R2 bucket via its S3-compatible API. R2 doesn't use AWS
/// regions, so the client is pointed at the account's R2 endpoint with path-style addressing.
/// </summary>
public class CloudflareR2StorageService : IImageStorageService
{
    private readonly IAmazonS3 _client;
    private readonly CloudflareR2Settings _settings;

    public CloudflareR2StorageService(IOptions<CloudflareR2Settings> options)
    {
        _settings = options.Value;

        var config = new AmazonS3Config
        {
            ServiceURL = _settings.ServiceUrl,
            ForcePathStyle = true,
            // R2 only implements a subset of the S3 API and doesn't support the AWS SDK's
            // newer default of streaming payloads with a trailing checksum ("STREAMING-...-TRAILER
            // not implemented"). Falling back to checksums only when required avoids that path.
            RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
            ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED,
        };

        _client = new AmazonS3Client(_settings.AccessKeyId, _settings.SecretAccessKey, config);
    }

    public async Task<ImageAsset> UploadAsync(Stream content, string fileName, string contentType, string folder, CancellationToken ct = default)
    {
        var extension = Path.GetExtension(fileName);
        var key = $"{folder}/{Guid.NewGuid():N}{extension}";

        await _client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _settings.BucketName,
            Key = key,
            InputStream = content,
            ContentType = contentType,
            AutoCloseStream = false,
            // R2 doesn't implement the SDK's chunked/streaming SigV4 payload signing at all
            // ("STREAMING-AWS4-HMAC-SHA256-PAYLOAD not implemented") — sign the whole payload
            // as a single unchunked request instead, which R2 does support.
            DisablePayloadSigning = true,
            UseChunkEncoding = false,
        }, ct);

        return new ImageAsset
        {
            Url = $"{_settings.PublicBaseUrl}/{key}",
            Alt = Path.GetFileNameWithoutExtension(fileName),
        };
    }

    public async Task DeleteAsync(string url, CancellationToken ct = default)
    {
        if (!url.StartsWith(_settings.PublicBaseUrl, StringComparison.OrdinalIgnoreCase))
            return; // not one of ours (e.g. a seed placeholder) — nothing to delete

        var key = url[(_settings.PublicBaseUrl.Length + 1)..];

        await _client.DeleteObjectAsync(new DeleteObjectRequest
        {
            BucketName = _settings.BucketName,
            Key = key,
        }, ct);
    }
}
