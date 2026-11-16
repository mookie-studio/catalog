using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;

namespace Catalog.Api.Infrastructure.Storage;

/// <summary>Stores Images in an S3-compatible bucket with path-style addressing and no ACLs (R2, MinIO).</summary>
public sealed class S3ImageStorage : IImageStorage, IDisposable
{
    private readonly StorageOptions _options;
    private readonly AmazonS3Client _client;
    private readonly bool _https;

    public S3ImageStorage(IOptions<StorageOptions> options)
    {
        _options = options.Value;
        _https = _options.ServiceUrl!.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
        var config = new AmazonS3Config
        {
            ServiceURL = _options.ServiceUrl,
            ForcePathStyle = true,
            AuthenticationRegion = _options.Region,
            RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
            ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED,
        };
        _client = new AmazonS3Client(new BasicAWSCredentials(_options.AccessKey, _options.SecretKey), config);
    }

    public async Task SaveAsync(string key, Stream content, string contentType, CancellationToken ct)
    {
        await _client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _options.Bucket,
            Key = key,
            InputStream = content,
            ContentType = contentType,
            AutoCloseStream = false,
            // R2 does not support streaming payload signatures; unsigned payloads require HTTPS.
            DisablePayloadSigning = _https,
        }, ct);
    }

    public async Task DeleteAsync(string key, CancellationToken ct) =>
        await _client.DeleteObjectAsync(new DeleteObjectRequest { BucketName = _options.Bucket, Key = key }, ct);

    public string GetPublicUrl(string key) => $"{_options.PublicBaseUrl?.TrimEnd('/')}/{key}";

    public void Dispose() => _client.Dispose();
}

/// <summary>Used when no Storage settings are present: the API starts, but Image operations fail clearly.</summary>
public sealed class UnconfiguredImageStorage : IImageStorage
{
    private const string Message = "Image storage is not configured. Set the Storage settings.";

    public Task SaveAsync(string key, Stream content, string contentType, CancellationToken ct) =>
        throw new InvalidOperationException(Message);

    public Task DeleteAsync(string key, CancellationToken ct) => throw new InvalidOperationException(Message);

    public string GetPublicUrl(string key) => throw new InvalidOperationException(Message);
}

public static class StorageServiceCollectionExtensions
{
    public static IServiceCollection AddCatalogImageStorage(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<StorageOptions>().BindConfiguration(StorageOptions.SectionName);
        var options = configuration.GetSection(StorageOptions.SectionName).Get<StorageOptions>();
        if (options?.IsConfigured == true)
        {
            services.AddSingleton<IImageStorage, S3ImageStorage>();
        }
        else
        {
            services.AddSingleton<IImageStorage, UnconfiguredImageStorage>();
        }

        return services;
    }
}
