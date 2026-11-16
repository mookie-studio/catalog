namespace Catalog.Api.Infrastructure.Storage;

/// <summary>S3-compatible storage settings (Cloudflare R2, MinIO), bound from the "Storage" section.</summary>
public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    public string? ServiceUrl { get; set; }
    public string? Bucket { get; set; }
    public string? AccessKey { get; set; }
    public string? SecretKey { get; set; }
    public string? PublicBaseUrl { get; set; }

    /// <summary>Signing region. R2 accepts "us-east-1" as an alias of "auto"; MinIO defaults to it.</summary>
    public string Region { get; set; } = "us-east-1";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ServiceUrl) && !string.IsNullOrWhiteSpace(Bucket);

    public override string ToString() => $"StorageOptions {{ ServiceUrl = {ServiceUrl}, Bucket = {Bucket} }}";
}
